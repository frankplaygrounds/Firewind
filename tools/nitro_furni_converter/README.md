# Nitro Furni Converter

Standalone Node.js tool for converting Habbo furniture from `.nitro` to a zoom-complete, graphics-cleaned `.swf`.

The tool was built as a new project. The referenced Nitro converter, zoom packer, and `higoka/habbo-graphics-tag` project were used as technical guidance, not merged together.

## Installation

Requirements:

- Node.js 18 or newer.
- An AIR/Flex SDK that provides `mxmlc`.
  - Pass it with `--air-home /path/to/sdk`, or set `AIR_HOME` / `FLEX_HOME`.

Install dependencies:

```bash
cd tools/nitro_furni_converter
npm install
```

## Usage

```bash
node index.js input-furniture.nitro output-furniture.swf
```

Convert every `.nitro` in a folder into an output folder:

```bash
node index.js --air-home /path/to/AIRSDK /path/to/nitro-folder /path/to/swf-output-folder
```

With an explicit compiler SDK:

```bash
node index.js --air-home /path/to/AIRSDK input-furniture.nitro output-furniture.swf
```

Convert and deploy into this Firewind setup:

```bash
node index.js --deploy --air-home /path/to/AIRSDK input-furniture.nitro output-furniture.swf
```

Deployment mode keeps the normal output SWF, copies the SWF to `/Users/Iaad/Documents/GitHub/Firewind-Web/swf/dcr/hof_furni/<furni>.swf`, updates the active `furnidata` and `productdata` files referenced by `client.php`, and creates or updates the DB rows for a `Recently Added` catalog page.

Deploy a whole folder without keeping normal output SWFs:

```bash
node index.js \
  --deploy-skip-output \
  --air-home /path/to/AIRSDK \
  /path/to/nitro-folder
```

`--deploy-skip-output` implies `--deploy`. It compiles each Nitro through a temporary SWF, deploys the final cleaned SWF to `dcr/hof_furni`, updates gamedata/DB, then removes the temporary compile output.

Patch existing SWFs in place when they are missing bundled zoomed-out furniture assets:

```bash
node index.js --adapt-swf /path/to/swf-folder
```

`--adapt-swf` recursively scans `.swf` files, reads the Habbo furniture XML already bundled in each SWF, detects `_64_` assets without `_32_` partners, generates the missing `_32_` bitmaps/aliases, updates the manifest/assets/visualization XML, clones the matching ActionScript asset class inside `DoABC`, writes through a temporary sibling file, and replaces the original SWF. It does not need `--air-home` because it patches the SWF/ABC structures directly instead of compiling from Nitro.

Furniture with color variations is detected automatically from Nitro visualization `colors` metadata. For example, a library named `example_chair` with color IDs `1`, `2`, and `3` deploys one DCR file, `example_chair.swf`, and creates catalog/gamedata/product entries for `example_chair*1`, `example_chair*2`, and `example_chair*3`.

Dry-run the Nitro parse, zoom planning, atlas export, and AS3 project generation without compiling:

```bash
node index.js --dry-run --work-dir .tmp/example input-furniture.nitro output-furniture.swf
```

Useful options:

```bash
--debug-dir <path>    Write conversion-report.json
--keep-temp           Keep the generated AS3 project after compiling
--verbose             Show debug logs and compiler output
--skip-cleanup        Leave visualization <graphics> wrappers in place
--skip-verify         Skip final SWF validation
--adapt-swf           Patch existing SWFs in place, no Nitro or output path
--deploy              Enable Firewind-Web/gamedata/catalog deployment
--deploy-skip-output  Deploy only; do not keep normal output SWFs
--web-root <path>     Override the Firewind-Web path
--no-db               Skip MySQL catalog/items_base updates
--no-gamedata         Skip SWF copy and gamedata updates
--display-name <text> Override the generated catalog/product title
--description <text>  Override the generated catalog/product description
--price-credits <n>   Override catalog credit price
--item-type <s|i>     Use floor item s or wall item i
--variant-ids <list>  Force variants, e.g. 1,2,3
--no-variants         Disable automatic color-map variant detection
--walkable            Mark item walkable/canstandon
--sittable            Mark item sittable/cansiton
--not-stackable       Mark item non-stackable
```

## Pipeline

1. Reads the `.nitro` entry table and decompresses the JSON descriptor and PNG atlas.
2. Parses furniture assets, spritesheet frames, logic metadata, visualization metadata, aliases, directions, layers, colors, animations, and registrations.
3. Detects missing zoomed-out assets by comparing `_64_` assets with existing `_32_` assets.
4. Generates missing direct bitmap assets with nearest-neighbor half scaling.
5. Generates missing alias/source XML assets without rasterizing aliases.
6. Writes Habbo furniture XML binaries: manifest, index, assets, logic, and visualization.
7. Writes AS3 embed classes for all XML binaries and PNG assets.
8. Compiles the generated project with `mxmlc`.
9. Reopens the SWF and removes visualization `<graphics>` wrappers from `DefineBinaryData`.
10. Verifies that the SWF parses, required XML binaries exist, generated zoom assets are present, duplicate asset names were not introduced, and graphical tags were removed.
11. When `--deploy` is used, deploys the final SWF to Firewind-Web, updates gamedata, and adds or updates the catalog DB records.
12. When `--deploy-skip-output` is used, removes the temporary compiled SWF after deployment and leaves only the DCR copy.

## SWF Adapt Mode

`--adapt-swf` is for furniture SWFs that already exist in `dcr/hof_furni` or another folder. It does not rebuild the whole SWF. Instead it:

1. Reads `SymbolClass`, `DoABC`, furniture `DefineBinaryData` XML, and bitmap tags from the SWF.
2. Parses `manifest`, `assets`, and `visualizationData`.
3. Finds direct `_64_` bitmap assets whose `_32_` equivalent is missing or not bundled.
4. Decodes `DefineBitsLossless`, `DefineBitsLossless2`, `DefineBitsJPEG2`, or `DefineBitsJPEG3` source images.
5. Scales the source image to half size, inserts a new bitmap tag, exports it with a matching symbol name, and clones the existing `_64_` ABC asset class plus script initializer under the new `_32_` class name.
6. Adds missing `_32_` direct assets to `assets` XML and `manifest`.
7. Adds missing `_32_` alias assets by pointing them at the generated scaled source.
8. Clones the size `64` visualization into size `32` if no size `32` visualization exists.
9. Removes visualization `<graphics>` wrappers from files that were actually adapted unless `--skip-cleanup` is used.
10. Writes the patched SWF to a temporary file next to the original, verifies it can be read, then atomically replaces the original.

Dry-run an existing folder without changing files:

```bash
node index.js --adapt-swf --dry-run --verbose /path/to/swf-folder
```

## Reference Decisions

- Nitro converter reference:
  - Reused ideas: Nitro archive layout, generated AS3 project flow, manifest/XML/embed structure, and CLI-style logging.
  - Rewritten: archive validation, project generation, image export, error handling, and compiler resolution.
  - Ignored: Windows-only `cmd.exe` assumptions and minimal visualization serialization.

- Zoom packer reference:
  - Reused ideas: `_64_` to `_32_` detection, alias preservation, registration scaling, nearest-neighbor zoom generation, and XML manifest/assets/visualization handling.
  - Rewritten in Node: zoom planning and image generation are integrated before SWF compilation, so generated assets are bundled by `mxmlc` instead of patched into ABC after the fact.

- Graphics-tag reference:
  - Reused behavior: remove visualization `<graphics>` wrappers from final SWFs.
  - Rewritten: cleanup is native SWF binary-data parsing, not PHP plus FFDec XML conversion.

## Test Workflow

Dry-run using the sample Nitro files from the referenced converter:

```bash
cd tools/nitro_furni_converter
npm run test:workflow
```

Use a different sample:

```bash
SAMPLE_NITRO=/path/to/furniture.nitro npm run test:workflow
```

Run the full compile, cleanup, and verify workflow:

```bash
AIR_HOME=/path/to/AIRSDK RUN_FULL=1 npm run test:workflow
```

Run a real conversion and deploy into the local Firewind web/catalog stack:

```bash
node index.js \
  --deploy \
  --air-home /path/to/AIRSDK \
  --display-name "My New Furni" \
  --description "Freshly converted furniture." \
  --price-credits 3 \
  /path/to/furniture.nitro \
  /path/to/output/furniture.swf
```

Batch deploy a folder into DCR/gamedata/catalog without keeping a separate output folder:

```bash
node index.js \
  --deploy-skip-output \
  --air-home /Users/Iaad/Downloads/apache-flex-sdk-4.16.1-bin \
  /Users/Iaad/Downloads/nitro-converter/nitro
```

Syntax-check the project:

```bash
npm run check
```

Adapt the local DCR folder in place:

```bash
node index.js --adapt-swf /Users/Iaad/Documents/GitHub/Firewind-Web/swf/dcr/hof_furni
```

## Known Limitations

- SWF creation requires `mxmlc`; this project does not include a full SWF/ABC compiler.
- Rotated TexturePacker atlas frames are detected and rejected with a clear error.
- Furniture with nonstandard scale naming may need future convention inference from reference SWFs.
- Final Habbo client loading cannot be fully proven locally without running the target client; the verifier checks SWF structure and furniture library assets.
- The native SWF parser supports the SWF structures needed by furniture libraries (`FWS`/`CWS`, `SymbolClass`, `DefineBinaryData`, and normal tag rewriting). `ZWS`/LZMA SWFs are not supported.
- The native ABC patcher clones existing asset classes in standard Habbo/Flex furniture SWFs. Highly custom ABC layouts may be skipped with a clear error rather than written incorrectly.
- `--adapt-swf` can generate from `DefineBitsLossless`, `DefineBitsLossless2`, `DefineBitsJPEG2`, and `DefineBitsJPEG3` bitmap tags. SWFs whose only usable `_64_` source is in another image/tag format are skipped with a warning.
- `--adapt-swf` patches files in place. Use `--dry-run` first on large folders, and keep external backups if you are adapting irreplaceable SWF packs.
- Nitro metadata does not always say whether an item should be sit/walk/stack or use a special interaction. Deployment defaults to a normal floor item; use CLI flags to override behavior for special furniture.
- Automatic `*n` variation detection is based on Nitro visualization color IDs. If a furniture line uses a different convention, pass `--variant-ids 1,2,3` or disable it with `--no-variants`.

## Troubleshooting

`Could not find AIR/Flex mxmlc compiler`

Install an AIR/Flex SDK and pass `--air-home`, or set `AIR_HOME` / `FLEX_HOME`.

`Failed to start mxmlc ... EACCES`

The Flex SDK scripts are not executable or are still quarantined by macOS:

```bash
chmod +x /path/to/apache-flex-sdk/bin/*
xattr -dr com.apple.quarantine /path/to/apache-flex-sdk
```

`unable to open '{playerglobalHome}/.../playerglobal.swc'`

Apache Flex needs a Flash Player API SWC. Put a valid `playerglobal.swc` under `frameworks/libs/player/<version>/playerglobal.swc`; the converter automatically sets `PLAYERGLOBAL_HOME` and targets the highest valid version it finds.

`Frame ... is rotated in the atlas`

The Nitro atlas uses rotated TexturePacker frames. Re-export the atlas without rotation or extend `AtlasExporter` with rotated-frame reconstruction.

`Skipped ... no spritesheet frame was found`

The JSON declared a direct bitmap asset but did not include a matching frame. The tool will not invent bitmap data when it cannot trace a source image.

`visualization XML still contains a graphics wrapper`

Cleanup was skipped or the visualization XML has an unexpected structure. Run without `--skip-cleanup` and inspect `--debug-dir`.

`Skipped ... no supported bitmap symbol was found`

`--adapt-swf` found a `_64_` XML asset but could not match it to an exported bitmap tag the adapter can decode. The SWF may use a nonstandard export name or an unsupported bitmap tag.

`Could not clone ABC class(es)`

The SWF has a bitmap/XML asset but no matching ActionScript asset class in `DoABC`. Habbo clients usually load furniture assets through those classes, so the adapter refuses to write a half-patched SWF.

`The item disappears only when zoomed out`

Rerun `--adapt-swf` with the current version. Older builds could leave a half-patched SWF where `_32_` XML/bitmap tags existed but the `_32_` ActionScript class initializer was missing from `DoABC`.

`Could not create Recently Added catalog page`

Check that MySQL is running and that the `firewind` database contains `catalog_pages`, `catalog_items`, and `items_base`. The default connection uses `/usr/local/mysql-5.7.31-macos10.14-x86_64/bin/mysql`, root at `127.0.0.1`, no password first, then `4299`.
