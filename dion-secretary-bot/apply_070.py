from __future__ import annotations
import base64, io, lzma, sys, tarfile
from pathlib import Path

PARTS = [f"part{i:02d}" for i in range(7)]

def main() -> int:
    target = Path(sys.argv[1] if len(sys.argv) > 1 else "src").resolve()
    if not target.is_dir():
        raise SystemExit(f"Source directory not found: {target}")
    here = Path(__file__).resolve().parent
    payload = "".join((here / name).read_text(encoding="ascii").strip() for name in PARTS)
    raw = lzma.decompress(base64.b64decode(payload))
    with tarfile.open(fileobj=io.BytesIO(raw), mode="r:") as archive:
        names = archive.getnames()
        archive.extractall(target)
    for name in names:
        print(f"0.7 Secretary Bot wrote {name}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
