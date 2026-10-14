#!/usr/bin/env python3
"""Measure shipping bytes by folder/CPU (ZIP compressed sizes), without Android tooling."""
import argparse
import collections
import json
import pathlib
import zipfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("apk", type=pathlib.Path)
parser.add_argument("--out", type=pathlib.Path)
args = parser.parse_args()
groups = collections.Counter()
files = []
with zipfile.ZipFile(args.apk) as archive:
    if archive.testzip() is not None:
        raise ValueError("APK contains a corrupt ZIP entry")
    for item in archive.infolist():
        parts = item.filename.split("/")
        group = "/".join(parts[:2]) if parts[0] in ("assets", "lib") else parts[0]
        groups[group] += item.compress_size
        files.append({"path": item.filename, "packed": item.compress_size, "unpacked": item.file_size})
        if item.filename.startswith("assets/") and item.filename.endswith((".sha256", ".tmp")):
            raise ValueError("Build cache metadata leaked into APK: " + item.filename)
report = {"apk": args.apk.name, "bytes": args.apk.stat().st_size,
          "folders": dict(groups.most_common()), "files": files}
if args.out:
    args.out.parent.mkdir(parents=True, exist_ok=True)
    args.out.write_text(json.dumps(report, indent=2) + "\n")
print(f"{args.apk.name}: {report['bytes'] / 1024**2:.2f} MiB")
for folder, size in groups.most_common():
    if size > 1024 * 1024:
        print(f"  {folder:24} {size / 1024**2:6.2f} MiB")
