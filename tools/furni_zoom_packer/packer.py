"""High-level Habbo furniture zoom asset injection."""

from __future__ import annotations

import json
import shutil
import xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path

from .abcpatch import AbcAssetClass, patch_abc
from .pngutil import RgbaImage, round_half_up, scale_half_nearest, write_png
from .swf import (
    TAG_DEFINE_BINARY_DATA,
    TAG_DO_ABC,
    TAG_SYMBOL_CLASS,
    BitmapTag,
    BinaryDataTag,
    SwfFile,
    build_define_bits_lossless2,
)


XML_DECL = '<?xml version="1.0" encoding="ISO-8859-1" ?>\n'


@dataclass
class DirectInjection:
    source_asset: str
    generated_asset: str
    source_class: str
    generated_class: str
    source_character_id: int
    generated_character_id: int
    source_size: tuple[int, int]
    generated_size: tuple[int, int]
    x: int
    y: int


@dataclass
class AliasInjection:
    source_asset: str
    generated_asset: str
    alias_source: str
    x: int
    y: int
    flip_h: bool


@dataclass
class PackResult:
    input_path: Path
    output_path: Path
    furniture_class: str
    direct_injections: list[DirectInjection]
    alias_injections: list[AliasInjection]
    warnings: list[str]

    @property
    def injected_count(self) -> int:
        return len(self.direct_injections) + len(self.alias_injections)


def pack_swf(input_path: Path, output_path: Path, debug_dir: Path | None = None) -> PackResult:
    swf = SwfFile.read(input_path)
    symbols = swf.parse_symbols()
    root_classes = [name for character_id, name in symbols if character_id == 0]
    if len(root_classes) != 1:
        raise RuntimeError(f"expected one root SymbolClass entry with character id 0, found {root_classes}")
    furniture_class = root_classes[0]

    bitmap_by_id = swf.bitmaps()
    symbol_names_by_id = _symbol_names_by_id(symbols)
    image_assets = _map_image_assets(symbols, bitmap_by_id, furniture_class)
    binary = _classify_binary_xml(swf, symbols)

    required = {"manifest", "assets", "visualization"}
    missing = sorted(required - set(binary))
    if missing:
        raise RuntimeError(f"missing required furniture XML binary data: {', '.join(missing)}")

    debug_root = debug_dir
    if debug_root:
        if debug_root.exists():
            shutil.rmtree(debug_root)
        (debug_root / "original-pngs").mkdir(parents=True)
        (debug_root / "generated-zoom-pngs").mkdir(parents=True)
        _write_original_pngs(debug_root / "original-pngs", bitmap_by_id, symbol_names_by_id)
        _write_asset_map(debug_root / "asset-map-before.json", swf, image_assets)

    assets_xml = _parse_xml(binary["assets"].data)
    manifest_xml = _parse_xml(binary["manifest"].data)
    visualization_xml = _parse_xml(binary["visualization"].data)

    direct, aliases, warnings = _plan_injections(
        assets_xml=assets_xml,
        manifest_xml=manifest_xml,
        image_assets=image_assets,
        bitmap_by_id=bitmap_by_id,
        start_character_id=swf.max_character_id() + 1,
    )

    if not direct and not aliases:
        output_path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(input_path, output_path)
        result = PackResult(input_path, output_path, furniture_class, direct, aliases, warnings)
        if debug_root:
            _write_report(debug_root / "comparison-report.md", result, warnings, already_complete=True)
        return result

    generated_images: dict[str, RgbaImage] = {}
    for injection in direct:
        image = scale_half_nearest(bitmap_by_id[injection.source_character_id].image)
        generated_images[injection.generated_asset] = image
        if debug_root:
            write_png(debug_root / "generated-zoom-pngs" / f"{injection.generated_asset}.png", image)

    _apply_assets_xml(assets_xml, direct, aliases)
    _apply_manifest_xml(manifest_xml, direct)
    _apply_visualization_xml(visualization_xml, warnings)

    swf.set_binary_data(binary["assets"], _serialize_xml(assets_xml))
    swf.set_binary_data(binary["manifest"], _serialize_xml(manifest_xml))
    swf.set_binary_data(binary["visualization"], _serialize_xml(visualization_xml))

    new_bitmap_tags = [
        build_define_bits_lossless2(injection.generated_character_id, generated_images[injection.generated_asset])
        for injection in direct
    ]
    swf.insert_tags_before_doabc(new_bitmap_tags)

    symbols = _insert_symbol_entries_before_root(
        swf.parse_symbols(),
        [(injection.generated_character_id, injection.generated_class) for injection in direct],
    )
    swf.replace_symbol_class(symbols)

    do_abc = swf.do_abc()
    patched_abc = patch_abc(
        do_abc.abc,
        furniture_class,
        [
            AbcAssetClass(
                asset_name=injection.generated_asset,
                class_name=injection.generated_class,
                template_asset_name=injection.source_asset,
                template_class_name=injection.source_class,
            )
            for injection in direct
        ],
    )
    swf.set_do_abc(do_abc, patched_abc)
    swf.write(output_path)

    after = SwfFile.read(output_path)
    if debug_root:
        _write_asset_map(
            debug_root / "asset-map-after.json",
            after,
            _map_image_assets(after.symbols, after.bitmaps(), furniture_class),
        )
        _write_report(debug_root / "comparison-report.md", PackResult(input_path, output_path, furniture_class, direct, aliases, warnings), warnings)

    return PackResult(input_path, output_path, furniture_class, direct, aliases, warnings)


def _symbol_names_by_id(symbols: list[tuple[int, str]]) -> dict[int, list[str]]:
    out: dict[int, list[str]] = {}
    for character_id, name in symbols:
        out.setdefault(character_id, []).append(name)
    return out


def _map_image_assets(
    symbols: list[tuple[int, str]],
    bitmaps: dict[int, BitmapTag],
    root_class_name: str,
) -> dict[str, dict[str, object]]:
    out: dict[str, dict[str, object]] = {}
    for character_id, class_name in symbols:
        if character_id not in bitmaps:
            continue
        short = _short_asset_name(class_name, root_class_name)
        if short:
            bitmap = bitmaps[character_id]
            out[short] = {
                "class_name": class_name,
                "character_id": character_id,
                "width": bitmap.width,
                "height": bitmap.height,
            }
    return out


def _short_asset_name(class_name: str, root_class_name: str) -> str | None:
    prefix = root_class_name + "_"
    if class_name.startswith(prefix):
        return class_name[len(prefix) :]
    return None


def _classify_binary_xml(swf: SwfFile, symbols: list[tuple[int, str]]) -> dict[str, BinaryDataTag]:
    names_by_id = {character_id: name for character_id, name in symbols}
    out: dict[str, BinaryDataTag] = {}
    for binary in swf.binary_data():
        text = binary.data[:200].decode("ISO-8859-1", "ignore")
        name = names_by_id.get(binary.character_id, "")
        if "<manifest" in text or name.endswith("_manifest"):
            out["manifest"] = binary
        elif "<assets" in text or name.endswith("_assets"):
            out["assets"] = binary
        elif "<visualizationData" in text or name.endswith("_visualization"):
            out["visualization"] = binary
        elif "<objectData" in text or name.endswith("_logic"):
            out["logic"] = binary
        elif "<object " in text or name.endswith("_index"):
            out["index"] = binary
    return out


def _parse_xml(data: bytes) -> ET.Element:
    parser = ET.XMLParser(encoding="ISO-8859-1")
    return ET.fromstring(data, parser=parser)


def _serialize_xml(root: ET.Element) -> bytes:
    ET.indent(root, space="  ")
    body = ET.tostring(root, encoding="unicode", short_empty_elements=True)
    return (XML_DECL + body + "\n").encode("ISO-8859-1")


def _plan_injections(
    *,
    assets_xml: ET.Element,
    manifest_xml: ET.Element,
    image_assets: dict[str, dict[str, object]],
    bitmap_by_id: dict[int, BitmapTag],
    start_character_id: int,
) -> tuple[list[DirectInjection], list[AliasInjection], list[str]]:
    existing_asset_nodes = {node.attrib.get("name", "") for node in assets_xml.findall("asset")}
    manifest_assets = _manifest_asset_names(manifest_xml)
    direct: list[DirectInjection] = []
    aliases: list[AliasInjection] = []
    warnings: list[str] = []
    next_id = start_character_id

    for node in list(assets_xml.findall("asset")):
        name = node.attrib.get("name", "")
        if "_64_" not in name:
            continue
        generated = name.replace("_64_", "_32_", 1)
        if generated in existing_asset_nodes:
            continue

        x = _scaled_registration(node.attrib.get("x"))
        y = _scaled_registration(node.attrib.get("y"))
        if x is None or y is None:
            warnings.append(f"Skipped {name}: missing or invalid x/y registration point")
            continue

        source = node.attrib.get("source")
        if source:
            aliases.append(
                AliasInjection(
                    source_asset=name,
                    generated_asset=generated,
                    alias_source=source.replace("_64_", "_32_", 1),
                    x=x,
                    y=y,
                    flip_h=node.attrib.get("flipH") == "1",
                )
            )
            existing_asset_nodes.add(generated)
            continue

        if name not in image_assets:
            warnings.append(f"Skipped {name}: no linked bitmap class was found")
            continue
        if name not in manifest_assets:
            warnings.append(f"Skipped {name}: bitmap is not declared in manifest")
            continue

        image_info = image_assets[name]
        source_class = str(image_info["class_name"])
        generated_class = source_class[: -len(name)] + generated
        source_character_id = int(image_info["character_id"])
        source_bitmap = bitmap_by_id[source_character_id]
        generated_image = scale_half_nearest(source_bitmap.image)

        direct.append(
            DirectInjection(
                source_asset=name,
                generated_asset=generated,
                source_class=source_class,
                generated_class=generated_class,
                source_character_id=source_character_id,
                generated_character_id=next_id,
                source_size=(source_bitmap.width, source_bitmap.height),
                generated_size=(generated_image.width, generated_image.height),
                x=x,
                y=y,
            )
        )
        next_id += 1
        existing_asset_nodes.add(generated)
        manifest_assets.add(generated)

    generated_direct_names = {item.generated_asset for item in direct}
    aliases = [
        alias
        for alias in aliases
        if alias.alias_source in generated_direct_names or alias.alias_source in existing_asset_nodes
    ]
    return direct, aliases, warnings


def _scaled_registration(value: str | None) -> int | None:
    if value is None:
        return None
    try:
        return round_half_up(float(value) / 2)
    except ValueError:
        return None


def _manifest_asset_names(manifest_xml: ET.Element) -> set[str]:
    return {
        node.attrib.get("name", "")
        for node in manifest_xml.findall("./library/assets/asset")
        if node.attrib.get("mimeType") == "image/png"
    }


def _apply_assets_xml(
    assets_xml: ET.Element,
    direct: list[DirectInjection],
    aliases: list[AliasInjection],
) -> None:
    existing = {node.attrib.get("name", "") for node in assets_xml.findall("asset")}
    additions: list[ET.Element] = []

    for injection in direct:
        if injection.generated_asset in existing:
            continue
        additions.append(
            ET.Element(
                "asset",
                {"name": injection.generated_asset, "x": str(injection.x), "y": str(injection.y)},
            )
        )
        existing.add(injection.generated_asset)

    for injection in aliases:
        if injection.generated_asset in existing:
            continue
        attrib = {
            "name": injection.generated_asset,
            "source": injection.alias_source,
            "x": str(injection.x),
            "y": str(injection.y),
        }
        if injection.flip_h:
            attrib["flipH"] = "1"
        additions.append(ET.Element("asset", attrib))
        existing.add(injection.generated_asset)

    assets_xml.extend(additions)


def _apply_manifest_xml(manifest_xml: ET.Element, direct: list[DirectInjection]) -> None:
    assets = manifest_xml.find("./library/assets")
    if assets is None:
        raise RuntimeError("manifest XML does not contain library/assets")
    existing = {node.attrib.get("name", "") for node in assets.findall("asset")}
    for injection in direct:
        if injection.generated_asset in existing:
            continue
        assets.append(ET.Element("asset", {"name": injection.generated_asset, "mimeType": "image/png"}))
        existing.add(injection.generated_asset)


def _apply_visualization_xml(visualization_xml: ET.Element, warnings: list[str]) -> None:
    graphics = visualization_xml.find("graphics")
    parent = graphics if graphics is not None else visualization_xml
    visualizations = parent.findall("visualization")
    size64 = next((node for node in visualizations if node.attrib.get("size") == "64"), None)
    size32 = next((node for node in visualizations if node.attrib.get("size") == "32"), None)
    if size64 is None:
        warnings.append("Could not clone visualization size 64: no size=64 visualization found")
        return
    if size32 is None:
        size32 = ET.Element("visualization", dict(size64.attrib))
        size32.set("size", "32")
        parent.insert(list(parent).index(size64), size32)
    else:
        size32.attrib.clear()
        size32.attrib.update(size64.attrib)
        size32.set("size", "32")
        size32[:] = []

    for child in list(size64):
        size32.append(_clone_element(child))


def _clone_element(element: ET.Element) -> ET.Element:
    clone = ET.Element(element.tag, dict(element.attrib))
    clone.text = element.text
    clone.tail = element.tail
    for child in list(element):
        clone.append(_clone_element(child))
    return clone


def _insert_symbol_entries_before_root(
    symbols: list[tuple[int, str]], additions: list[tuple[int, str]]
) -> list[tuple[int, str]]:
    existing_names = {name for _, name in symbols}
    additions = [entry for entry in additions if entry[1] not in existing_names]
    if not additions:
        return symbols
    first_root_index = next((i for i, (character_id, _) in enumerate(symbols) if character_id == 0), len(symbols))
    return symbols[:first_root_index] + additions + symbols[first_root_index:]


def _write_original_pngs(
    output_dir: Path,
    bitmaps: dict[int, BitmapTag],
    symbol_names_by_id: dict[int, list[str]],
) -> None:
    for character_id, bitmap in bitmaps.items():
        names = symbol_names_by_id.get(character_id) or [f"id_{character_id}"]
        for name in names:
            write_png(output_dir / f"{_safe_filename(name)}.png", bitmap.image)


def _write_asset_map(path: Path, swf: SwfFile, image_assets: dict[str, dict[str, object]]) -> None:
    data = {
        "tag_counts": _tag_counts(swf),
        "symbols": [{"character_id": cid, "name": name} for cid, name in swf.parse_symbols()],
        "image_assets": image_assets,
        "binary_assets": [
            {
                "character_id": binary.character_id,
                "length": len(binary.data),
                "tag_index": binary.tag_index,
            }
            for binary in swf.binary_data()
        ],
    }
    path.write_text(json.dumps(data, indent=2, sort_keys=True))


def _tag_counts(swf: SwfFile) -> dict[str, int]:
    counts: dict[str, int] = {}
    for tag in swf.tags:
        counts[tag.name] = counts.get(tag.name, 0) + 1
    return counts


def _write_report(
    path: Path,
    result: PackResult,
    warnings: list[str],
    *,
    already_complete: bool = False,
) -> None:
    lines = [
        "# Furniture Zoom Injection Report",
        "",
        f"Input: `{result.input_path}`",
        f"Output: `{result.output_path}`",
        f"Furniture class: `{result.furniture_class}`",
        "",
    ]
    if already_complete:
        lines.append("No missing `_32_` assets were detected; the output is a byte-for-byte copy of the input.")
    else:
        lines.extend(
            [
                f"Direct bitmap assets injected: {len(result.direct_injections)}",
                f"Alias/source XML assets injected: {len(result.alias_injections)}",
                "",
                "## Direct Bitmap Assets",
                "",
            ]
        )
        for injection in result.direct_injections:
            lines.append(
                "- "
                f"`{injection.generated_asset}` from `{injection.source_asset}` "
                f"character {injection.source_character_id}->{injection.generated_character_id}, "
                f"{injection.source_size[0]}x{injection.source_size[1]} -> "
                f"{injection.generated_size[0]}x{injection.generated_size[1]}, "
                f"registration ({injection.x}, {injection.y})"
            )
        lines.extend(["", "## Alias Assets", ""])
        for injection in result.alias_injections:
            flip = ", flipH=1" if injection.flip_h else ""
            lines.append(
                f"- `{injection.generated_asset}` source=`{injection.alias_source}` "
                f"registration ({injection.x}, {injection.y}){flip}"
            )

    lines.extend(
        [
            "",
            "## Structural Changes",
            "",
            "- Added `DefineBitsLossless2` tags for generated `_32_` bitmap assets.",
            "- Added `SymbolClass` entries for generated bitmap classes.",
            "- Added generated image entries to `manifest` XML.",
            "- Added generated direct/alias entries to `assets` XML.",
            "- Replaced/cloned visualization size `32` from size `64`.",
            "- Added ABC `BitmapAsset` classes and root resource-class fields.",
            "",
            "## Assumptions",
            "",
            "- Images are generated with nearest-neighbour half scaling to preserve pixel-art edges.",
            "- Registration points are rounded from half of the source `x`/`y` values.",
            "- Source/flip aliases remain aliases and are not emitted as bitmap tags.",
        ]
    )
    if warnings:
        lines.extend(["", "## Warnings", ""])
        lines.extend(f"- {warning}" for warning in warnings)
    path.write_text("\n".join(lines) + "\n")


def _safe_filename(name: str) -> str:
    return "".join(ch if ch.isalnum() or ch in "._-" else "_" for ch in name)
