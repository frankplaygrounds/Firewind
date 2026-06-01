"""Patch Habbo furniture ABC classes using RABCDAsm text assembly."""

from __future__ import annotations

import re
import shutil
import subprocess
import tempfile
from dataclasses import dataclass
from pathlib import Path


@dataclass(frozen=True)
class AbcAssetClass:
    asset_name: str
    class_name: str
    template_asset_name: str
    template_class_name: str


def patch_abc(
    abc: bytes,
    root_class_name: str,
    asset_classes: list[AbcAssetClass],
    *,
    rabcdasm: str | None = None,
    rabcasm: str | None = None,
) -> bytes:
    if not asset_classes:
        return abc

    rabcdasm_path = rabcdasm or shutil.which("rabcdasm")
    rabcasm_path = rabcasm or shutil.which("rabcasm")
    if not rabcdasm_path or not rabcasm_path:
        raise RuntimeError(
            "rabcdasm and rabcasm are required to patch the furniture ABC. "
            "Install RABCDAsm or put both commands on PATH."
        )

    with tempfile.TemporaryDirectory(prefix="furni-abc-") as temp_dir:
        work = Path(temp_dir)
        abc_path = work / "library.abc"
        abc_path.write_bytes(abc)

        subprocess.run([rabcdasm_path, str(abc_path)], cwd=work, check=True, capture_output=True)
        asm_dir = work / "library"
        main_files = list(asm_dir.glob("*.main.asasm"))
        if len(main_files) != 1:
            raise RuntimeError(f"expected one RABCDAsm main file, found {len(main_files)}")
        main_path = main_files[0]

        for asset in asset_classes:
            _create_asset_script(asm_dir, asset)

        _patch_main_includes(main_path, [asset.class_name for asset in asset_classes])
        _patch_root_class(asm_dir / f"{root_class_name}.class.asasm", asset_classes)

        subprocess.run([rabcasm_path, str(main_path.name)], cwd=asm_dir, check=True, capture_output=True)
        assembled = main_path.with_suffix(".abc")
        if not assembled.exists():
            raise RuntimeError(f"rabcasm did not create {assembled.name}")
        return assembled.read_bytes()


def _create_asset_script(asm_dir: Path, asset: AbcAssetClass) -> None:
    source_script = asm_dir / f"{asset.template_class_name}.script.asasm"
    source_class = asm_dir / f"{asset.template_class_name}.class.asasm"
    if not source_script.exists() or not source_class.exists():
        raise RuntimeError(
            f"cannot find template ABC files for {asset.template_class_name}; "
            "the source bitmap class is missing from the SWF ABC"
        )

    script_text = source_script.read_text()
    class_text = source_class.read_text()
    (asm_dir / f"{asset.class_name}.script.asasm").write_text(
        script_text.replace(asset.template_class_name, asset.class_name)
    )
    (asm_dir / f"{asset.class_name}.class.asasm").write_text(
        class_text.replace(asset.template_class_name, asset.class_name)
    )


def _patch_main_includes(main_path: Path, class_names: list[str]) -> None:
    text = main_path.read_text()
    includes = "".join(f' #include "{class_name}.script.asasm"\n' for class_name in class_names)
    missing = [
        line
        for line in includes.splitlines(keepends=True)
        if line.strip() not in {existing.strip() for existing in text.splitlines()}
    ]
    if not missing:
        return
    marker = "\nend ; program"
    if marker not in text:
        raise RuntimeError("RABCDAsm main file does not contain program terminator")
    text = text.replace(marker, "\n" + "".join(missing) + marker, 1)
    main_path.write_text(text)


def _patch_root_class(root_path: Path, asset_classes: list[AbcAssetClass]) -> None:
    if not root_path.exists():
        raise RuntimeError(f"root furniture class not found in ABC: {root_path.name}")

    text = root_path.read_text()
    missing = [asset for asset in asset_classes if f'"{asset.asset_name}"' not in _trait_names(text)]
    if not missing:
        return

    assign_opcode = _infer_assignment_opcode(text, asset_classes[0].template_class_name)
    trait_kind = _infer_trait_kind(text, asset_classes[0].template_asset_name)
    max_slot = max([int(slot) for slot in re.findall(r"\bslotid\s+(\d+)", text)] or [0])

    assignment_lines = []
    trait_lines = []
    for offset, asset in enumerate(missing, start=1):
        assignment_lines.append(
            "\n"
            f'    findproperty        QName(PackageNamespace(""), "{asset.asset_name}")\n'
            f'    getlex              QName(PackageNamespace(""), "{asset.class_name}")\n'
            f'    {assign_opcode:<19} QName(PackageNamespace(""), "{asset.asset_name}")\n'
        )
        trait_lines.append(
            f' trait {trait_kind} QName(PackageNamespace(""), "{asset.asset_name}") '
            f'slotid {max_slot + offset} type QName(PackageNamespace(""), "Class") end\n'
        )

    cinit_pos = text.find("\n cinit")
    if cinit_pos == -1:
        raise RuntimeError("root furniture class has no cinit block")
    return_pos = text.find("\n    returnvoid", cinit_pos)
    if return_pos == -1:
        raise RuntimeError("root furniture class cinit has no returnvoid")
    text = text[:return_pos] + "".join(assignment_lines) + text[return_pos:]

    class_end = text.rfind("end ; class")
    if class_end == -1:
        raise RuntimeError("root furniture class has no end marker")
    text = text[:class_end] + "".join(trait_lines) + text[class_end:]
    root_path.write_text(text)


def _trait_names(text: str) -> str:
    traits = []
    for match in re.finditer(r'trait (?:const|slot) QName\(PackageNamespace\(""\), "([^"]+)"\)', text):
        traits.append(f'"{match.group(1)}"')
    return "\n".join(traits)


def _infer_assignment_opcode(text: str, template_class_name: str) -> str:
    pattern = (
        r'getlex\s+QName\(PackageNamespace\(""\), "'
        + re.escape(template_class_name)
        + r'"\)\s+([a-z]+property)\s+QName'
    )
    match = re.search(pattern, text)
    return match.group(1) if match else "initproperty"


def _infer_trait_kind(text: str, asset_name: str) -> str:
    match = re.search(
        r'trait (const|slot) QName\(PackageNamespace\(""\), "' + re.escape(asset_name) + r'"\)',
        text,
    )
    return match.group(1) if match else "const"
