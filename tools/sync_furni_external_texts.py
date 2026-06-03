#!/usr/bin/env python3
"""Generate furniture external text keys from furnidata XML."""

from __future__ import annotations

import argparse
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("furnidata", type=Path, help="Path to furnidata XML")
    parser.add_argument("external_texts", type=Path, nargs="+", help="external_flash_texts files to update")
    return parser.parse_args()


def add_entry_set(entries: dict[str, str], prefix: str, code: str, name: str, description: str) -> None:
    entries[f"{prefix}.name.{code}"] = name
    entries[f"{prefix}.desc.{code}"] = description


def add_legacy_entry_set(entries: dict[str, str], prefix: str, code: str, name: str, description: str) -> None:
    entries[f"{prefix}_{code}_name"] = name
    entries[f"{prefix}_{code}_desc"] = description


def build_entries(furnidata: Path) -> dict[str, str]:
    root = ET.parse(furnidata).getroot()
    entries: dict[str, str] = {}
    furnitypes = [
        ("roomItem", furnitype)
        for furnitype in root.findall(".//roomitemtypes/furnitype")
    ]
    wall_furnitypes = [
        ("wallItem", furnitype)
        for furnitype in root.findall(".//wallitemtypes/furnitype")
    ]

    for item_prefix, furnitype in furnitypes + wall_furnitypes:
        sprite_id = (furnitype.get("id") or "").strip()
        classname = (furnitype.get("classname") or "").strip()
        if not sprite_id and not classname:
            continue

        name = (
            (furnitype.findtext("title") or "").strip()
            or (furnitype.findtext("name") or "").strip()
            or classname
            or sprite_id
        )
        description = (furnitype.findtext("description") or "").strip()
        codes = {value for value in (sprite_id, classname) if value}
        if "*" in classname:
            codes.add(classname.replace("*", "_"))

        for code in codes:
            add_entry_set(entries, "furni", code, name, description)
            add_entry_set(entries, item_prefix, code, name, description)
            add_entry_set(entries, "furnitype", code, name, description)
            add_legacy_entry_set(entries, "furni", code, name, description)

    return entries


def sync_text_file(path: Path, entries: dict[str, str]) -> int:
    original = path.read_text(errors="replace") if path.exists() else ""
    lines = original.splitlines()
    existing_keys = {line.split("=", 1)[0] for line in lines if "=" in line}
    additions = [f"{key}={value}" for key, value in sorted(entries.items()) if key not in existing_keys]

    if additions:
        if original and not original.endswith("\n"):
            original += "\n"
        path.write_text(original + "\n".join(additions) + "\n")

    return len(additions)


def main() -> int:
    args = parse_args()
    entries = build_entries(args.furnidata)
    for path in args.external_texts:
        print(f"{path}: added={sync_text_file(path, entries)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
