Firewind
========

Educational project

## Habbo Furniture Zoom Packer

This repo includes `furni-zoom-packer`, a small CLI for newer Habbo furniture SWFs that are missing zoomed-out `_32_` bitmap assets.

Usage:

```bash
./furni-zoom-packer input.swf output.swf
./furni-zoom-packer input.swf output.swf --debug ./debug-output
```

The tool reads the input SWF, finds the Habbo furniture XML resources, generates missing `_32_` assets from direct `_64_` bitmap assets, and writes a new SWF. It preserves the original SWF compression type and existing tags, then adds only the missing zoom assets and metadata.

What it modifies:

- Adds `DefineBitsLossless2` bitmap tags for generated `_32_` images.
- Adds matching `SymbolClass` entries.
- Updates the embedded `manifest` XML with new image assets.
- Updates the embedded `*_assets` XML with new registrations and source/flip aliases.
- Replaces or creates the size `32` visualization by cloning the size `64` visualization structure.
- Patches the furniture ABC so the root resource class exposes the generated `BitmapAsset` classes.

Assumptions:

- The SWF is an AS3 Habbo furniture asset library with `manifest`, `index`, `*_assets`, `*_visualization`, and `*_logic` binary XML resources.
- Image tags are `DefineBitsLossless2` format 5, which is what the provided samples use.
- Direct `_64_` image assets are generated as direct `_32_` bitmap tags. Source/flip assets stay as XML aliases and are not duplicated as bitmap tags.
- Scaling uses nearest-neighbour half-size sampling to preserve pixel edges. Registration points are rounded from half of the original `x` and `y` values.
- `rabcdasm` and `rabcasm` must be available on `PATH`; they are used to patch the ABC class table cleanly.

Debug mode writes:

- `original-pngs/` with extracted bitmap assets.
- `generated-zoom-pngs/` with generated `_32_` assets.
- `asset-map-before.json` and `asset-map-after.json`.
- `comparison-report.md` describing every injected bitmap, alias, XML change, and warning.

Verification:

```bash
python3 -m unittest tests/test_furni_zoom_packer.py
```

By default, tests look for the sample SWFs in `../Firewind-Web/samples`. Set `FURNI_ZOOM_SAMPLE_DIR=/path/to/samples` if they live elsewhere.

Known limitations:

- The tool does not hand-paint official Habbo zoom art; it generates compatible half-size assets.
- Registration-point inference can be off by a pixel for furniture whose official legacy `_32_` art was manually aligned.
- SWFs using other bitmap tag formats or unusual package/class layouts produce diagnostics rather than silent output.
