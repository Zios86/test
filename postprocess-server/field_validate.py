from __future__ import annotations

import argparse
import json
import sys
import wave
from datetime import datetime
from pathlib import Path


def wav_info(path: Path) -> dict:
    with wave.open(str(path), "rb") as wav:
        rate = wav.getframerate()
        frames = wav.getnframes()
        return {
            "path": str(path),
            "duration_seconds": round(frames / max(1, rate), 3),
            "sample_rate": rate,
            "channels": wav.getnchannels(),
            "sample_width": wav.getsampwidth(),
            "bytes": path.stat().st_size,
        }


def validate_session(session: Path, expected_minutes: int) -> tuple[dict, bool]:
    checks: list[dict] = []

    def add(name: str, passed: bool, details: str) -> None:
        checks.append({"name": name, "passed": passed, "details": details})

    transcript_path = session / "transcript_autosave.json"
    transcript = {}
    try:
        transcript = json.loads(transcript_path.read_text(encoding="utf-8"))
        entries = transcript.get("entries") or []
        add("Черновая стенограмма", bool(entries), f"реплик: {len(entries)}")
    except Exception as exc:
        add("Черновая стенограмма", False, str(exc))

    tracks: list[dict] = []
    for name in ("system_audio.wav", "microphone_audio.wav"):
        path = session / name
        if not path.is_file():
            add(name, name == "microphone_audio.wav", "файл отсутствует")
            continue
        try:
            info = wav_info(path)
            tracks.append(info)
            minimum = expected_minutes * 60 * 0.9
            add(name, info["duration_seconds"] >= minimum, f"{info['duration_seconds'] / 60:.1f} мин; {info['bytes'] / 1024**2:.1f} МБ")
        except Exception as exc:
            add(name, False, f"WAV повреждён: {exc}")

    if len(tracks) == 2:
        delta = abs(tracks[0]["duration_seconds"] - tracks[1]["duration_seconds"])
        add("Синхронность дорожек", delta <= 5.0, f"разница {delta:.3f} с")

    result_dir = session / "postprocess_result"
    expected_results = ["precise_transcript.json", "corrected_transcript.txt", "comparison.json"]
    missing = [name for name in expected_results if not (result_dir / name).is_file()]
    add("Результат post-processing", not missing, "готов" if not missing else "нет: " + ", ".join(missing))
    comparison = {}
    if not missing:
        try:
            comparison = json.loads((result_dir / "comparison.json").read_text(encoding="utf-8"))
            add("Метрики обработки", "processing_seconds" in comparison, f"обработка: {comparison.get('processing_seconds', '—')} с")
        except Exception as exc:
            add("Метрики обработки", False, str(exc))

    passed = all(check["passed"] for check in checks)
    report = {
        "format": "dion-field-validation-1",
        "created_at": datetime.now().astimezone().isoformat(timespec="seconds"),
        "session": str(session),
        "expected_minutes": expected_minutes,
        "passed": passed,
        "checks": checks,
        "tracks": tracks,
        "comparison": comparison,
    }
    return report, passed


def main() -> int:
    parser = argparse.ArgumentParser(description="Проверка полевой записи DION 1.0.1")
    parser.add_argument("session", type=Path)
    parser.add_argument("--expected-minutes", type=int, choices=(15, 60, 120), required=True)
    args = parser.parse_args()
    session = args.session.resolve()
    report, passed = validate_session(session, args.expected_minutes)
    output = session / f"field_validation_{args.expected_minutes}m.json"
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    for check in report["checks"]:
        print(("OK" if check["passed"] else "FAIL") + f" | {check['name']} | {check['details']}")
    print(f"Отчёт: {output}")
    return 0 if passed else 1


if __name__ == "__main__":
    raise SystemExit(main())
