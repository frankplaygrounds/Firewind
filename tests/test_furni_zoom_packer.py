from __future__ import annotations

import os
import shutil
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path
from types import SimpleNamespace
from unittest.mock import patch

from tools.furni_zoom_packer.cli import main as zoom_cli_main
from tools.furni_zoom_packer.packer import pack_swf
from tools.furni_zoom_packer.swf import SwfFile


def sample_dir() -> Path:
    env = os.environ.get("FURNI_ZOOM_SAMPLE_DIR")
    if env:
        return Path(env)
    return Path(__file__).resolve().parents[2] / "Firewind-Web" / "samples"


def require_samples() -> bool:
    samples = sample_dir()
    return all(
        (samples / name).exists()
        for name in (
            "legacy_zoom_furni1.swf",
            "new_furni1.swf",
            "new_furni2.swf",
            "new_furni3.swf",
        )
    )


def require_rabcdasm() -> bool:
    return shutil.which("rabcdasm") is not None and shutil.which("rabcasm") is not None


def binary_xml_by_kind(swf: SwfFile) -> dict[str, str]:
    names_by_id = {character_id: name for character_id, name in swf.symbols}
    out: dict[str, str] = {}
    for binary in swf.binary_data():
        name = names_by_id.get(binary.character_id, "")
        text = binary.data.decode("ISO-8859-1")
        if name.endswith("_manifest") or "<manifest" in text:
            out["manifest"] = text
        elif name.endswith("_assets") or "<assets" in text:
            out["assets"] = text
        elif name.endswith("_visualization") or "<visualizationData" in text:
            out["visualization"] = text
    return out


@unittest.skipUnless(require_samples(), "sample SWFs are not available")
class FurniZoomPackerTests(unittest.TestCase):
    def test_legacy_fixture_already_contains_zoom_assets(self) -> None:
        swf = SwfFile.read(sample_dir() / "legacy_zoom_furni1.swf")
        symbols = [name for _, name in swf.symbols]

        self.assertTrue(any("_32_" in name for name in symbols))
        self.assertEqual(5, len(swf.binary_data()))
        self.assertGreaterEqual(len([name for name in symbols if "_32_" in name]), 4)

    def test_new_fixture_is_missing_zoom_bitmap_classes(self) -> None:
        swf = SwfFile.read(sample_dir() / "new_furni2.swf")
        symbols = [name for _, name in swf.symbols]
        xml = binary_xml_by_kind(swf)

        self.assertFalse(any("_32_" in name for name in symbols))
        self.assertIn('visualization size="32"', xml["visualization"])
        self.assertIn("<layers />", xml["visualization"].replace("<layers/>", "<layers />"))

    @unittest.skipUnless(require_rabcdasm(), "RABCDAsm tools are not available")
    def test_packer_injects_expected_swf_structure(self) -> None:
        with tempfile.TemporaryDirectory(prefix="furni-pack-test-") as temp:
            temp_path = Path(temp)
            output = temp_path / "new_furni3_zoom.swf"
            debug = temp_path / "debug"

            result = pack_swf(sample_dir() / "new_furni3.swf", output, debug)
            self.assertEqual(2, len(result.direct_injections))
            self.assertEqual(2, len(result.alias_injections))

            packed = SwfFile.read(output)
            symbols = [name for _, name in packed.symbols]
            self.assertIn(
                "fall_c23_leafyfloor_fall_c23_leafyfloor_32_a_0_0",
                symbols,
            )
            self.assertIn(
                "fall_c23_leafyfloor_fall_c23_leafyfloor_32_a_0_1",
                symbols,
            )
            self.assertEqual(5, len(packed.bitmaps()))

            xml = binary_xml_by_kind(packed)
            self.assertIn('name="fall_c23_leafyfloor_32_a_0_0"', xml["manifest"])
            self.assertIn('source="fall_c23_leafyfloor_32_a_0_0"', xml["assets"])
            self.assertIn('visualization size="32" layerCount="1"', xml["visualization"])
            self.assertNotIn("<layers />", xml["visualization"].replace("<layers/>", "<layers />"))

            self.assertTrue((debug / "asset-map-before.json").exists())
            self.assertTrue((debug / "asset-map-after.json").exists())
            self.assertTrue((debug / "comparison-report.md").exists())
            self.assertTrue(
                (debug / "generated-zoom-pngs" / "fall_c23_leafyfloor_32_a_0_0.png").exists()
            )

    @unittest.skipUnless(require_rabcdasm(), "RABCDAsm tools are not available")
    def test_generated_zoom_bitmaps_have_half_scale_dimensions(self) -> None:
        with tempfile.TemporaryDirectory(prefix="furni-pack-test-") as temp:
            output = Path(temp) / "new_furni2_zoom.swf"
            result = pack_swf(sample_dir() / "new_furni2.swf", output)
            sizes = {item.generated_asset: item.generated_size for item in result.direct_injections}

            self.assertEqual((23, 91), sizes["fall_c23_leafcurtains_32_a_0_0"])
            self.assertEqual((27, 88), sizes["fall_c23_leafcurtains_32_b_0_0"])
            self.assertEqual((44, 24), sizes["fall_c23_leafcurtains_32_sd_0_0"])

    @unittest.skipUnless(require_rabcdasm(), "RABCDAsm tools are not available")
    def test_legacy_fixture_is_left_unmodified_when_complete(self) -> None:
        with tempfile.TemporaryDirectory(prefix="furni-pack-test-") as temp:
            output = Path(temp) / "legacy_copy.swf"
            result = pack_swf(sample_dir() / "legacy_zoom_furni1.swf", output)

            self.assertEqual(0, result.injected_count)
            self.assertEqual(
                (sample_dir() / "legacy_zoom_furni1.swf").read_bytes(),
                output.read_bytes(),
            )


class FurniZoomPackerCliTests(unittest.TestCase):
    def test_directory_mode_preserves_relative_paths(self) -> None:
        with tempfile.TemporaryDirectory(prefix="furni-pack-cli-test-") as temp:
            temp_path = Path(temp)
            input_dir = temp_path / "in"
            output_dir = temp_path / "out"
            input_file = input_dir / "nested" / "chair.swf"
            input_file.parent.mkdir(parents=True)
            input_file.write_bytes(b"swf")

            def fake_pack(input_path: Path, output_path: Path, debug_dir: Path | None):
                output_path.parent.mkdir(parents=True, exist_ok=True)
                output_path.write_bytes(input_path.read_bytes() + b"-packed")
                return SimpleNamespace(
                    furniture_class="chair",
                    direct_injections=[object()],
                    alias_injections=[],
                    warnings=[],
                    injected_count=1,
                )

            with patch("tools.furni_zoom_packer.cli.pack_swf", side_effect=fake_pack):
                exit_code = zoom_cli_main([str(input_dir), str(output_dir)])

            self.assertEqual(0, exit_code)
            self.assertEqual(b"swf-packed", (output_dir / "nested" / "chair.swf").read_bytes())

    def test_directory_mode_can_copy_on_error(self) -> None:
        with tempfile.TemporaryDirectory(prefix="furni-pack-cli-test-") as temp:
            temp_path = Path(temp)
            input_dir = temp_path / "in"
            output_dir = temp_path / "out"
            report = temp_path / "report.json"
            input_file = input_dir / "broken.swf"
            input_file.parent.mkdir(parents=True)
            input_file.write_bytes(b"original")

            with patch("tools.furni_zoom_packer.cli.pack_swf", side_effect=RuntimeError("no xml")):
                exit_code = zoom_cli_main(
                    [str(input_dir), str(output_dir), "--copy-on-error", "--report", str(report)]
                )

            self.assertEqual(0, exit_code)
            self.assertEqual(b"original", (output_dir / "broken.swf").read_bytes())
            self.assertIn('"failed": 1', report.read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
