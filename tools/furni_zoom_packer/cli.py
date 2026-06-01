"""Command line interface for furni-zoom-packer."""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

from . import __version__
from .packer import pack_swf


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="furni-zoom-packer",
        description="Generate and inject missing Habbo furniture zoom-out (_32_) SWF assets.",
    )
    parser.add_argument("input", type=Path, help="Input furniture SWF")
    parser.add_argument("output", type=Path, help="Output furniture SWF")
    parser.add_argument("--debug", type=Path, help="Directory for extracted PNGs, maps, and report")
    parser.add_argument("--version", action="version", version=f"%(prog)s {__version__}")
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    try:
        result = pack_swf(args.input, args.output, args.debug)
    except Exception as exc:  # noqa: BLE001 - CLI should show concise diagnostics.
        print(f"furni-zoom-packer: error: {exc}", file=sys.stderr)
        return 1

    if result.injected_count:
        print(
            f"Injected {len(result.direct_injections)} bitmap assets and "
            f"{len(result.alias_injections)} aliases into {args.output}"
        )
    else:
        print(f"No missing zoom assets detected; copied input to {args.output}")
    if result.warnings:
        print("Warnings:")
        for warning in result.warnings:
            print(f"  - {warning}")
    if args.debug:
        print(f"Debug output written to {args.debug}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
