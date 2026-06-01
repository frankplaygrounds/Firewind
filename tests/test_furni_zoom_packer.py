from __future__ import annotations

import os
import shutil
import tempfile
import unittest
import xml.etree.ElementTree as ET
from pathlib import Path

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


if __name__ == "__main__":
    unittest.main()
