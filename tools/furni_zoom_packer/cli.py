"""Command line interface for furni-zoom-packer."""

from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

from . import __version__
from .packer import pack_swf


def _swf_files(path: Path) -> list[Path]:
    return sorted(
        candidate
        for candidate in path.rglob("*")
        if candidate.is_file() and candidate.suffix.lower() == ".swf"
    )


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        prog="furni-zoom-packer",
        description="Generate and inject missing Habbo furniture zoom-out (_32_) SWF assets.",
    )
    parser.add_argument("input", type=Path, help="Input furniture SWF or directory of SWFs")
    parser.add_argument("output", type=Path, help="Output furniture SWF or directory")
    parser.add_argument("--debug", type=Path, help="Directory for extracted PNGs, maps, and reports")
    parser.add_argument(
        "--copy-on-error",
        action="store_true",
        help="In directory mode, copy an unmodified SWF when packing that file fails.",
    )
    parser.add_argument(
        "--report",
        type=Path,
        help="In directory mode, write a JSON summary report to this path.",
    )
    parser.add_argument("--version", action="version", version=f"%(prog)s {__version__}")
    return parser


def _pack_one(input_path: Path, output_path: Path, debug_dir: Path | None) -> dict[str, object]:
    result = pack_swf(input_path, output_path, debug_dir)
    return {
        "input": str(input_path),
        "output": str(output_path),
        "furniture_class": result.furniture_class,
        "direct_injections": len(result.direct_injections),
        "alias_injections": len(result.alias_injections),
        "warnings": result.warnings,
        "status": "packed" if result.injected_count else "copied",
    }


def _pack_directory(args: argparse.Namespace) -> int:
    if args.output.exists() and not args.output.is_dir():
        print("furni-zoom-packer: error: directory input requires directory output", file=sys.stderr)
        return 2

    files = _swf_files(args.input)
    if not files:
        print(f"furni-zoom-packer: error: no .swf files found in {args.input}", file=sys.stderr)
        return 1

    entries: list[dict[str, object]] = []
    copied_on_error = 0
    failed = 0
    packed = 0
    already_complete = 0

    for index, input_path in enumerate(files, start=1):
        relative = input_path.relative_to(args.input)
        output_path = args.output / relative
        debug_dir = args.debug / relative.with_suffix("") if args.debug else None

        try:
            entry = _pack_one(input_path, output_path, debug_dir)
            if entry["status"] == "packed":
                packed += 1
            else:
                already_complete += 1
        except Exception as exc:  # noqa: BLE001 - batch mode should finish the catalogue.
            failed += 1
            output_path.parent.mkdir(parents=True, exist_ok=True)
            entry = {
                "input": str(input_path),
                "output": str(output_path),
                "status": "failed",
                "error": str(exc),
                "copied_original": False,
            }
            if args.copy_on_error:
                output_path.write_bytes(input_path.read_bytes())
                entry["copied_original"] = True
                copied_on_error += 1
            print(f"[{index}/{len(files)}] failed {relative}: {exc}", file=sys.stderr)
        else:
            print(f"[{index}/{len(files)}] {entry['status']} {relative}")

        entries.append(entry)

    report = {
        "input": str(args.input),
        "output": str(args.output),
        "total": len(files),
        "packed": packed,
        "already_complete": already_complete,
        "failed": failed,
        "copied_on_error": copied_on_error,
        "entries": entries,
    }
    if args.report:
        args.report.parent.mkdir(parents=True, exist_ok=True)
        args.report.write_text(json.dumps(report, indent=2), encoding="utf-8")

    print(
        "Processed {total} SWFs: {packed} packed, {already_complete} already complete/copied, "
        "{failed} failed ({copied_on_error} copied on error).".format(**report)
    )
    return 1 if failed and not args.copy_on_error else 0


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)

    if args.input.is_dir():
        return _pack_directory(args)
    if args.output.exists() and args.output.is_dir():
        args.output = args.output / args.input.name
    if args.report:
        print("furni-zoom-packer: error: --report is only valid in directory mode", file=sys.stderr)
        return 2
    if args.copy_on_error:
        print("furni-zoom-packer: error: --copy-on-error is only valid in directory mode", file=sys.stderr)
        return 2

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
