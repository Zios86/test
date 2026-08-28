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
    for member in members:
        print(f"0.9 Guest Bot wrote {member.name}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
