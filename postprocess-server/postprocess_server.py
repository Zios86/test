from __future__ import annotations

import argparse
import queue
import json
import os
import re
import secrets
import shutil
import threading
import time
import urllib.request
import uuid
import zipfile
from dataclasses import dataclass, field
from datetime import datetime
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
    submitted_at: str = field(default_factory=lambda: datetime.now().astimezone().isoformat(timespec="seconds"))
    started_at: str = ""
    finished_at: str = ""
    processing_seconds: float = 0.0


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
    job_queue: queue.Queue[Job] = field(default_factory=lambda: queue.Queue(maxsize=8))


ALLOWED_REQUEST_FILES = {
    "manifest.json",
    "transcript_autosave.json",
    "system_audio.wav",
    "microphone_audio.wav",
}
MAX_UNPACKED_BYTES = 6 * 1024 * 1024 * 1024
SERVER_VERSION = "1.0.1"


def safe_extract(payload: bytes | Path, destination: Path) -> None:
    source = payload if isinstance(payload, Path) else __import__("io").BytesIO(payload)
    with zipfile.ZipFile(source) as archive:
        root = destination.resolve()
        members = archive.infolist()
        names = [member.filename for member in members]
        if len(names) != len(set(names)):
            raise ValueError("Архив содержит повторяющиеся имена файлов")
        if not names or any(name not in ALLOWED_REQUEST_FILES for name in names):
            raise ValueError("Архив содержит неожиданные файлы")
        if sum(member.file_size for member in members) > MAX_UNPACKED_BYTES:
            raise ValueError("Распакованный пакет превышает допустимый размер")
        for member in members:
            target = (root / member.filename).resolve()
            try:
                target.relative_to(root)
            except ValueError as exc:
                raise ValueError("Небезопасный путь внутри архива") from exc
            if member.file_size > 4 * 1024 * 1024 * 1024:
                raise ValueError("Один из файлов превышает допустимый размер")
        archive.extractall(destination)


def safe_request_file(request_dir: Path, name: object, allowed: set[str]) -> Path:
    value = str(name or "")
    if value not in allowed:
        raise ValueError(f"Недопустимое имя файла в manifest: {value!r}")
    target = (request_dir / value).resolve()
    try:
        target.relative_to(request_dir.resolve())
    except ValueError as exc:
        raise ValueError("Путь из manifest выходит за пределы задания") from exc
    if not target.is_file():
        raise ValueError(f"Файл из manifest не найден: {value}")
    return target


def validate_manifest(request_dir: Path, manifest: object) -> tuple[Path, list[Path]]:
    if not isinstance(manifest, dict) or manifest.get("format") != "dion-postprocess-1":
        raise ValueError("Неподдерживаемый формат manifest")
    transcript = safe_request_file(request_dir, manifest.get("transcript"), {"transcript_autosave.json"})
    audio_names = manifest.get("audio")
    if not isinstance(audio_names, list) or not 1 <= len(audio_names) <= 2:
        raise ValueError("Manifest должен содержать одну или две WAV-дорожки")
    if len(audio_names) != len(set(map(str, audio_names))):
        raise ValueError("Manifest содержит повторяющиеся WAV-дорожки")
    audio = [
        safe_request_file(request_dir, name, {"system_audio.wav", "microphone_audio.wav"})
        for name in audio_names
    ]
    return transcript, audio


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


def _source_key(value: object) -> str:
    text = str(value or "").casefold()
    return "microphone" if "microphone" in text else "system"


def align_draft(rows: list[dict], draft: dict) -> None:
    try:
        started_at = datetime.fromisoformat(str(draft.get("started_at") or ""))
    except ValueError:
        started_at = None
    candidates: list[dict] = []
    for index, entry in enumerate(draft.get("entries") or []):
        if not isinstance(entry, dict) or not str(entry.get("text") or "").strip():
            continue
        offset = None
        if started_at is not None:
            try:
                offset = max(0.0, (datetime.fromisoformat(str(entry.get("timestamp"))) - started_at).total_seconds())
            except ValueError:
                pass
        candidates.append({
            "index": index,
            "source": _source_key(entry.get("source")),
            "offset": offset,
            "text": " ".join(str(entry.get("text") or "").split()),
            "speaker": str(entry.get("speaker_display") or entry.get("speaker") or "").strip(),
        })
    for row in rows:
        same_source = [item for item in candidates if item["source"] == _source_key(row.get("source"))]
        best = None
        best_score = -1.0
        for item in same_source:
            similarity = SequenceMatcher(None, row["text_whisper"].casefold(), item["text"].casefold()).ratio()
            time_score = 0.0
            if item["offset"] is not None:
                time_score = max(0.0, 1.0 - abs(float(row["start"]) - float(item["offset"])) / 30.0)
            score = similarity * 0.65 + time_score * 0.35
            if score > best_score:
                best, best_score = item, score
        row["text_draft"] = best["text"] if best and best_score >= 0.18 else ""
        row["speaker"] = best["speaker"] if best and best_score >= 0.18 else ""
        row["draft_alignment_score"] = round(max(0.0, best_score), 4)


def ollama_correct_batch(state: ServerState, rows: list[dict]) -> list[str] | None:
    source = [
        {
            "whisper": row["text_whisper"],
            "draft": row.get("text_draft", ""),
            "speaker": row.get("speaker", ""),
        }
        for row in rows
    ]
    prompt = (
        "Ты корректор русской стенограммы. Для каждой строки сопоставь точный повторный Whisper-текст с черновой live-стенограммой. "
        "Исправь только очевидные ошибки распознавания, пунктуацию и написание терминов. Черновик используй как подсказку, "
        "но не переноси из него слова, которые противоречат Whisper-тексту. "
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


def _critical_tokens(text: str) -> list[str]:
    return re.findall(r"(?<!\w)\d+(?:[.,:/-]\d+)*(?!\w)", text.casefold())


def run_job(state: ServerState, job: Job) -> None:
    started = time.monotonic()
    job.started_at = datetime.now().astimezone().isoformat(timespec="seconds")
    try:
        job.status = "processing"
        request_dir = job.directory / "request"
        manifest = json.loads((request_dir / "manifest.json").read_text(encoding="utf-8"))
        transcript_path, audio_paths = validate_manifest(request_dir, manifest)
        draft = json.loads(transcript_path.read_text(encoding="utf-8"))
        if not isinstance(draft, dict) or not isinstance(draft.get("entries"), list):
            raise ValueError("Черновая стенограмма имеет неверный формат")
        rows: list[dict] = []
        for audio_path in audio_paths:
            rows.extend(transcribe_audio(state, audio_path, audio_path.stem, job))
        rows.sort(key=lambda row: (row["start"], row["source"]))
        align_draft(rows, draft)
        job.message = "Ollama исправляет формулировки без изменения фактов…"
        ollama_available = True
        for offset in range(0, len(rows), 12):
            batch = rows[offset:offset + 12]
            corrected = ollama_correct_batch(state, batch)
            if corrected is None:
                ollama_available = False
                break
            for row, text in zip(batch, corrected):
                ratio = SequenceMatcher(None, row["text_whisper"].casefold(), text.casefold()).ratio()
                changed_numbers = _critical_tokens(row["text_whisper"]) != _critical_tokens(text)
                if ratio < 0.55 or changed_numbers:
                    row["suggested_text"] = text
                    row["text_corrected"] = row["text_whisper"]
                    row["needs_review"] = True
                    row["correction_status"] = "rejected_numbers" if changed_numbers else "rejected_large_change"
                else:
                    row["text_corrected"] = text
                    row["needs_review"] = False
                    row["correction_status"] = "applied" if text != row["text_whisper"] else "unchanged"
        draft_text = " ".join(str(item.get("text") or "") for item in draft.get("entries", []))
        precise_text = " ".join(row["text_whisper"] for row in rows)
        comparison = {
            "draft_characters": len(draft_text),
            "precise_characters": len(precise_text),
            "overall_similarity": round(SequenceMatcher(None, draft_text.casefold(), precise_text.casefold()).ratio(), 4),
            "ollama_applied": ollama_available,
            "review_segments": sum(1 for row in rows if row["needs_review"]),
            "processing_seconds": round(time.monotonic() - started, 3),
            "note": "Исходная стенограмма не изменялась. Низкоуверенные исправления требуют прослушивания аудио.",
        }
        result_dir = job.directory / "result"
        result_dir.mkdir(parents=True, exist_ok=True)
        (result_dir / "precise_transcript.json").write_text(json.dumps({"segments": rows}, ensure_ascii=False, indent=2), encoding="utf-8")
        lines = [
            f"[{row['start']:09.3f}] {row.get('speaker') or row['source']}: {row['text_corrected']}"
            for row in rows
        ]
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
    finally:
        job.processing_seconds = round(time.monotonic() - started, 3)
        job.finished_at = datetime.now().astimezone().isoformat(timespec="seconds")


def job_worker(state: ServerState) -> None:
    while True:
        job = state.job_queue.get()
        try:
            run_job(state, job)
        finally:
            state.job_queue.task_done()


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
            self._json(HTTPStatus.OK, {
                "status": "ok",
                "server_version": SERVER_VERSION,
                "model": self.state.whisper_model,
                "ollama_model": self.state.ollama_model,
                "queued_jobs": self.state.job_queue.qsize(),
            })
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
            self._json(HTTPStatus.OK, {
                "job_id": job.job_id,
                "status": job.status,
                "message": job.message,
                "error": job.error,
                "submitted_at": job.submitted_at,
                "started_at": job.started_at,
                "finished_at": job.finished_at,
                "processing_seconds": job.processing_seconds,
            })
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
        try:
            self.state.job_queue.put_nowait(job)
        except queue.Full:
            with self.state.lock:
                self.state.jobs.pop(job_id, None)
            shutil.rmtree(directory, ignore_errors=True)
            self._json(HTTPStatus.SERVICE_UNAVAILABLE, {"error": "job_queue_full"})
            return
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
    threading.Thread(target=job_worker, args=(state,), daemon=True, name="postprocess-worker").start()
    server = ThreadingHTTPServer((args.host, args.port), Handler)
    server.state = state  # type: ignore[attr-defined]
    print(f"DION Postprocess Server: http://{args.host}:{args.port}")
    print(f"Whisper: {args.whisper_model} ({args.device}/{args.compute_type}); Ollama: {args.ollama_model}")
    server.serve_forever()
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
