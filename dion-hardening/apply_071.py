from __future__ import annotations

import base64
import io
import lzma
import sys
import tarfile
from pathlib import Path

PARTS = [f"part{i:02d}" for i in range(7)]


def _safe_members(archive: tarfile.TarFile, target: Path):
    root = target.resolve()
    for member in archive.getmembers():
        destination = (root / member.name).resolve()
        try:
            destination.relative_to(root)
        except ValueError as exc:
            raise SystemExit(f"Unsafe archive member: {member.name}") from exc
        yield member


def _apply_windows_test_seam(target: Path) -> None:
    """Avoid mutating process-global os.name in a Windows unit test."""
    bot = target / "app" / "dion_bot.py"
    text = bot.read_text(encoding="utf-8")
    marker = "from .dion_api import DionIntegrationClient, DionInvite\n\n"
    helper = "from .dion_api import DionIntegrationClient, DionInvite\n\n\ndef _is_windows() -> bool:\n    return os.name == \"nt\"\n\n"
    if "def _is_windows()" not in text:
        text = text.replace(marker, helper, 1)
    text = text.replace('if os.name == "nt":', "if _is_windows():", 1)
    bot.write_text(text, encoding="utf-8")

    test = target / "tests" / "test_dion_bot.py"
    text = test.read_text(encoding="utf-8")
    text = text.replace(
        'monkeypatch.setattr("app.dion_bot.os.name", "posix")',
        'monkeypatch.setattr("app.dion_bot._is_windows", lambda: False)',
    )
    test.write_text(text, encoding="utf-8")


def main() -> int:
    target = Path(sys.argv[1] if len(sys.argv) > 1 else "src").resolve()
    if not target.is_dir():
        raise SystemExit(f"Source directory not found: {target}")

    here = Path(__file__).resolve().parent
    payload = "".join((here / name).read_text(encoding="ascii").strip() for name in PARTS)
    raw = lzma.decompress(base64.b64decode(payload))
    with tarfile.open(fileobj=io.BytesIO(raw), mode="r:") as archive:
        members = list(_safe_members(archive, target))
        archive.extractall(target, members=members)
    _apply_windows_test_seam(target)
    for member in members:
        print(f"0.7.1 Hardening wrote {member.name}")
    print("0.7.1 Windows test seam applied")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
