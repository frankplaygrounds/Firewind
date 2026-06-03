#!/usr/bin/env python3
"""Sync Firewind sit/walk flags from furnidata XML.

Arcturus item SQL and Habbo furnidata can disagree about whether a furniture
item is sit-able or walkable. Firewind uses items_base.can_sit and
items_base.is_walkable for room physics, so keep those two values aligned with
furnidata's cansiton/canstandon tags.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("furnidata", type=Path, help="Path to furnidata XML")
    parser.add_argument("--mysql", default="/usr/local/mysql-5.7.31-macos10.14-x86_64/bin/mysql")
    parser.add_argument("--host", default="127.0.0.1")
    parser.add_argument("--user", default="root")
    parser.add_argument("--password", default=None)
    parser.add_argument("--database", default="firewind")
    return parser.parse_args()


def xml_flag(furnitype: ET.Element, tag_name: str) -> int:
    tag = furnitype.find(tag_name)
    return 1 if tag is not None and (tag.text or "").strip() == "1" else 0


def load_flags(furnidata: Path) -> list[tuple[int, int, int]]:
    root = ET.parse(furnidata).getroot()
    rows: list[tuple[int, int, int]] = []
    for furnitype in root.findall(".//roomitemtypes/furnitype"):
        raw_sprite_id = furnitype.get("id")
        if not raw_sprite_id or not raw_sprite_id.isdigit():
            continue

        rows.append(
            (
                int(raw_sprite_id),
                xml_flag(furnitype, "cansiton"),
                xml_flag(furnitype, "canstandon"),
            )
        )

    return rows


def build_sql(database: str, rows: list[tuple[int, int, int]]) -> str:
    statements = [
        f"USE `{database}`;",
        (
            "CREATE TEMPORARY TABLE tmp_furnidata_flags ("
            "`sprite_id` INT PRIMARY KEY, "
            "`can_sit` TINYINT(1) NOT NULL, "
            "`is_walkable` TINYINT(1) NOT NULL"
            ");"
        ),
    ]

    for start in range(0, len(rows), 1000):
        chunk = rows[start : start + 1000]
        values = ",".join(f"({sprite_id},{can_sit},{is_walkable})" for sprite_id, can_sit, is_walkable in chunk)
        statements.append(
            "INSERT INTO tmp_furnidata_flags (`sprite_id`, `can_sit`, `is_walkable`) VALUES "
            + values
            + ";"
        )

    statements.extend(
        [
            (
                "SELECT COUNT(*) AS before_mismatch "
                "FROM items_base ib "
                "JOIN tmp_furnidata_flags ff ON ff.sprite_id = ib.sprite_id "
                "WHERE ib.type = 's' "
                "AND (ib.can_sit <> ff.can_sit OR ib.is_walkable <> ff.is_walkable);"
            ),
            (
                "UPDATE items_base ib "
                "JOIN tmp_furnidata_flags ff ON ff.sprite_id = ib.sprite_id "
                "SET ib.can_sit = ff.can_sit, ib.is_walkable = ff.is_walkable "
                "WHERE ib.type = 's' "
                "AND (ib.can_sit <> ff.can_sit OR ib.is_walkable <> ff.is_walkable);"
            ),
            "SELECT ROW_COUNT() AS rows_changed;",
            (
                "SELECT COUNT(*) AS after_mismatch "
                "FROM items_base ib "
                "JOIN tmp_furnidata_flags ff ON ff.sprite_id = ib.sprite_id "
                "WHERE ib.type = 's' "
                "AND (ib.can_sit <> ff.can_sit OR ib.is_walkable <> ff.is_walkable);"
            ),
        ]
    )

    return "\n".join(statements)


def main() -> int:
    args = parse_args()
    rows = load_flags(args.furnidata)
    if not rows:
        print("No room furniture flags found.", file=sys.stderr)
        return 1

    command = [args.mysql, f"-h{args.host}", f"-u{args.user}", "-N", "-B"]
    if args.password:
        command.append(f"-p{args.password}")

    proc = subprocess.run(command, input=build_sql(args.database, rows), text=True)
    return proc.returncode


if __name__ == "__main__":
    raise SystemExit(main())
