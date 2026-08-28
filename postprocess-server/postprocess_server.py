from __future__ import annotations

import argparse
import json
import os
import secrets
import shutil
import threading
import urllib.request
import uuid
import zipfile
from dataclasses import dataclass, field
from difflib import SequenceMatcher
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any


@dataclass
class Job:
    job_id: str
    directory: Path
    status: str = "queued"
    message: str = "Задание ожидает обработки"
    error: str = ""
    result: Path | None = None


@dataclass
class ServerState:
    root: Path
    token: str
    allowed_clients: set[str]
    whisper_model: str
    device: str
    compute_type: str
    ollama_url: str
    ollama_model: str
    jobs: dict[str, Job] = field(default_factory=dict)
    lock: threading.Lock = field(default_factory=threading.Lock)
    model: Any = None
    model_lock: threading.Lock = field(default_factory=threading.Lock)


def safe_extract(payload: bytes | Path, destination: Path) -> None:
    source = payload if isinstance(payload, Path) else __import__("io").BytesIO(payload)
    with zipfile.ZipFile(source) as archive:
        root = destination.resolve()
        for member in archive.infolist():
            target = (root / member.filename).resolve()
            try:
                target.relative_to(root)
            except ValueError as exc:
                raise ValueError("Небезопасный путь внутри архива") from exc
            if member.file_size > 4 * 1024 * 1024 * 1024:
                raise ValueError("Один из файлов превышает допустимый размер")
        archive.extractall(destination)


def load_model(state: ServerState):
    with state.model_lock:
        if state.model is None:
            from faster_whisper import WhisperModel

            state.model = WhisperModel(
                state.whisper_model,
                device=state.device,
                compute_type=state.compute_type,
            )
        return state.model


def transcribe_audio(state: ServerState, path: Path, source: str, job: Job) -> list[dict]:
    job.message = f"Точное распознавание {path.name} моделью {state.whisper_model}…"
    model = load_model(state)
    segments, info = model.transcribe(
        str(path),
        language="ru",
        beam_size=5,
        vad_filter=True,
        word_timestamps=True,
        condition_on_previous_text=True,
    )
    rows = []
    for segment in segments:
        text = " ".join(str(segment.text or "").split())
        if text:
            rows.append({
                "source": source,
                "start": round(float(segment.start), 3),
                "end": round(float(segment.end), 3),
                "text_whisper": text,
                "text_corrected": text,
                "needs_review": False,
            })
    return rows


def ollama_correct_batch(state: ServerState, rows: list[dict]) -> list[str] | None:
    source = [row["text_whisper"] for row in rows]
    prompt = (
        "Ты корректор русской стенограммы. Исправь только очевидные ошибки распознавания, пунктуацию и написание терминов. "
        "Не добавляй факты, фамилии, числа, даты, сроки или поручения. Сохрани количество и порядок строк. "
        "Если исправление неочевидно, оставь исходный текст. Верни только JSON-массив строк.\n"
        + json.dumps(source, ensure_ascii=False)
    )
    body = json.dumps({
        "model": state.ollama_model,
        "prompt": prompt,
        "stream": False,
        "think": False,
        "format": {"type": "array", "items": {"type": "string"}, "minItems": len(rows), "maxItems": len(rows)},
        "options": {"temperature": 0.0},
    }, ensure_ascii=False).encode("utf-8")
    request = urllib.request.Request(
        state.ollama_url.rstrip("/") + "/api/generate",
        data=body,
        headers={"Content-Type": "application/json"},
        method="POST",
    )
    try:
        with urllib.request.urlopen(request, timeout=300) as response:
            outer = json.loads(response.read().decode("utf-8"))
        result = json.loads(outer.get("response") or "")
        if isinstance(result, list) and len(result) == len(rows) and all(isinstance(x, str) and x.strip() for x in result):
            return [" ".join(x.split()) for x in result]
    except Exception:
        return None
    return None


def run_job(state: ServerState, job: Job) -> None:
    try:
        job.status = "processing"
        request_dir = job.directory / "request"
        manifest = json.loads((request_dir / "manifest.json").read_text(encoding="utf-8"))
        transcript_path = request_dir / str(manifest["transcript"])
        draft = json.loads(transcript_path.read_text(encoding="utf-8"))
        rows: list[dict] = []
        for name in manifest.get("audio", []):
            audio_path = request_dir / str(name)
            if audio_path.is_file():
                rows.extend(transcribe_audio(state, audio_path, audio_path.stem, job))
        rows.sort(key=lambda row: (row["start"], row["source"]))
        job.message = "Ollama исправляет формулировки без изменения фактов…"
        ollama_available = True
        for offset in range(0, len(rows), 12):
            batch = rows[offset:offset + 12]
            corrected = ollama_correct_batch(state, batch)
            if corrected is None:
                ollama_available = False
                break
            for row, text in zip(batch, corrected):
                row["text_corrected"] = text
                ratio = SequenceMatcher(None, row["text_whisper"].casefold(), text.casefold()).ratio()
                row["needs_review"] = ratio < 0.55
        draft_text = " ".join(str(item.get("text") or "") for item in draft.get("entries", []))
        precise_text = " ".join(row["text_whisper"] for row in rows)
        comparison = {
            "draft_characters": len(draft_text),
            "precise_characters": len(precise_text),
            "overall_similarity": round(SequenceMatcher(None, draft_text.casefold(), precise_text.casefold()).ratio(), 4),
            "ollama_applied": ollama_available,
            "review_segments": sum(1 for row in rows if row["needs_review"]),
            "note": "Исходная стенограмма не изменялась. Низкоуверенные исправления требуют прослушивания аудио.",
        }
        result_dir = job.directory / "result"
        result_dir.mkdir(parents=True, exist_ok=True)
        (result_dir / "precise_transcript.json").write_text(json.dumps({"segments": rows}, ensure_ascii=False, indent=2), encoding="utf-8")
        lines = [f"[{row['start']:09.3f}] {row['source']}: {row['text_corrected']}" for row in rows]
        (result_dir / "corrected_transcript.txt").write_text("\n".join(lines) + "\n", encoding="utf-8")
        (result_dir / "comparison.json").write_text(json.dumps(comparison, ensure_ascii=False, indent=2), encoding="utf-8")
        result_zip = job.directory / "postprocess_result.zip"
        with zipfile.ZipFile(result_zip, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            for path in result_dir.iterdir():
                archive.write(path, path.name)
        job.result = result_zip
        job.status = "completed"
        job.message = "Точная обработка завершена"
    except Exception as exc:
        job.status = "failed"
        job.error = f"{type(exc).__name__}: {exc}"
        job.message = "Обработка завершилась ошибкой"


class Handler(BaseHTTPRequestHandler):
    server_version = "DIONPostprocess/1.0"

    @property
    def state(self) -> ServerState:
        return self.server.state  # type: ignore[attr-defined]

    def log_message(self, fmt: str, *args) -> None:
        print(f"{self.client_address[0]} - {fmt % args}")

    def _authorized(self) -> bool:
        if self.state.allowed_clients and self.client_address[0] not in self.state.allowed_clients:
            return False
        supplied = self.headers.get("Authorization", "")
        return secrets.compare_digest(supplied, f"Bearer {self.state.token}")

    def _json(self, status: int, payload: dict) -> None:
        raw = json.dumps(payload, ensure_ascii=False).encode("utf-8")
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(raw)))
        self.end_headers()
        self.wfile.write(raw)

    def do_GET(self) -> None:
        if self.path == "/api/v1/health":
            if not self._authorized():
                self._json(HTTPStatus.UNAUTHORIZED, {"error": "unauthorized"})
                return
            self._json(HTTPStatus.OK, {"status": "ok", "model": self.state.whisper_model, "ollama_model": self.state.ollama_model})
            return
        if not self._authorized():
            self._json(HTTPStatus.UNAUTHORIZED, {"error": "unauthorized"})
            return
        parts = self.path.strip("/").split("/")
        if len(parts) in {4, 5} and parts[:3] == ["api", "v1", "jobs"]:
            job = self.state.jobs.get(parts[3])
            if not job:
                self._json(HTTPStatus.NOT_FOUND, {"error": "job_not_found"})
                return
            if len(parts) == 5 and parts[4] == "result":
                if job.status != "completed" or not job.result:
                    self._json(HTTPStatus.CONFLICT, {"error": "result_not_ready"})
                    return
                self.send_response(HTTPStatus.OK)
                self.send_header("Content-Type", "application/zip")
                self.send_header("Content-Length", str(job.result.stat().st_size))
                self.end_headers()
                with job.result.open("rb") as source:
                    shutil.copyfileobj(source, self.wfile, length=1024 * 1024)
                return
            self._json(HTTPStatus.OK, {"job_id": job.job_id, "status": job.status, "message": job.message, "error": job.error})
            return
        self._json(HTTPStatus.NOT_FOUND, {"error": "not_found"})

    def do_POST(self) -> None:
        if self.path != "/api/v1/jobs":
            self._json(HTTPStatus.NOT_FOUND, {"error": "not_found"})
            return
        if not self._authorized():
            self._json(HTTPStatus.UNAUTHORIZED, {"error": "unauthorized"})
            return
        length = int(self.headers.get("Content-Length") or 0)
        if length <= 0 or length > 8 * 1024 * 1024 * 1024:
            self._json(HTTPStatus.REQUEST_ENTITY_TOO_LARGE, {"error": "invalid_size"})
            return
        job_id = uuid.uuid4().hex
        directory = self.state.root / job_id
        request_dir = directory / "request"
        request_dir.mkdir(parents=True, exist_ok=False)
        upload = directory / "postprocess_request.zip"
        try:
            remaining = length
            with upload.open("wb") as target:
                while remaining:
                    chunk = self.rfile.read(min(1024 * 1024, remaining))
                    if not chunk:
                        raise ValueError("Соединение прервано до завершения загрузки")
                    target.write(chunk)
                    remaining -= len(chunk)
            safe_extract(upload, request_dir)
        except Exception as exc:
            shutil.rmtree(directory, ignore_errors=True)
            self._json(HTTPStatus.BAD_REQUEST, {"error": str(exc)})
            return
        job = Job(job_id, directory)
        with self.state.lock:
            self.state.jobs[job_id] = job
        threading.Thread(target=run_job, args=(self.state, job), daemon=True, name=f"postprocess-{job_id[:8]}").start()
        self._json(HTTPStatus.ACCEPTED, {"job_id": job_id, "status": job.status})


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="DION post-meeting Whisper + Ollama server")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--port", type=int, default=8765)
    parser.add_argument("--allow-client", action="append", default=[])
    parser.add_argument("--token", default=os.environ.get("DION_POSTPROCESS_TOKEN", ""))
    parser.add_argument("--data-dir", default="postprocess-data")
    parser.add_argument("--whisper-model", default="large-v3-turbo")
    parser.add_argument("--device", default="cpu")
    parser.add_argument("--compute-type", default="int8")
    parser.add_argument("--ollama-url", default="http://127.0.0.1:11434")
    parser.add_argument("--ollama-model", default="qwen3:4b")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    if not args.token:
        raise SystemExit("Укажите --token или переменную DION_POSTPROCESS_TOKEN.")
    if args.host not in {"127.0.0.1", "localhost"} and not args.allow_client:
        raise SystemExit("Для сетевого режима обязательно укажите --allow-client IP_КЛИЕНТА.")
    root = Path(args.data_dir).resolve()
    root.mkdir(parents=True, exist_ok=True)
    state = ServerState(root, args.token, set(args.allow_client), args.whisper_model, args.device, args.compute_type, args.ollama_url, args.ollama_model)
    server = ThreadingHTTPServer((args.host, args.port), Handler)
    server.state = state  # type: ignore[attr-defined]
    print(f"DION Postprocess Server: http://{args.host}:{args.port}")
    print(f"Whisper: {args.whisper_model} ({args.device}/{args.compute_type}); Ollama: {args.ollama_model}")
    server.serve_forever()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
