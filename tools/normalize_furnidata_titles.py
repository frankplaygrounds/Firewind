#!/usr/bin/env python3
"""Ensure Firewind furnidata has <title> tags for clients that do not read <name>."""

from __future__ import annotations

import argparse
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("furnidata", type=Path, nargs="+", help="furnidata XML files to update")
    return parser.parse_args()


def normalize_file(path: Path) -> int:
    tree = ET.parse(path)
    root = tree.getroot()
    changed = 0

    for furnitype in root.findall(".//furnitype"):
        title = furnitype.find("title")
        name = furnitype.find("name")
        fallback = (
            (name.text or "").strip()
            if name is not None and name.text is not None
            else (furnitype.get("classname") or "").strip()
        )

        if title is None:
            title = ET.Element("title")
            title.text = fallback
            insert_at = list(furnitype).index(name) if name is not None else 0
            furnitype.insert(insert_at, title)
            changed += 1
        elif not (title.text or "").strip() and fallback:
            title.text = fallback
            changed += 1

    if changed:
        ET.indent(tree, space="")
        tree.write(path, encoding="UTF-8", xml_declaration=True, short_empty_elements=True)

    return changed


def main() -> int:
    args = parse_args()
    for path in args.furnidata:
        print(f"{path}: changed={normalize_file(path)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
