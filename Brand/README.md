# RoadSense icons

The master is `Unity/Assets/Brand/RoadSenseIcon.png`: 1024×1024 RGBA, with transparent outer corners and a charcoal tile. Its Unity texture GUID is `c9e02b6bdbef197aa8962f9ff9732056`. Keep it outside Resources; application icons do not need runtime loading.

`exports` contains a three-size favicon ICO (16/32/48), matching PNGs, a 180px Apple Touch icon, and `RoadSense.icns` containing the standard macOS icon and Retina sizes through 1024px. Use the PNG as the Unity Player icon; the ICNS is also available for native packaging. Web builds should copy the favicon to the template's favicon location and expose the PNG and Apple Touch variants.

Created for RoadSense with OpenAI's built-in image generation tool on 2026-11-07. One generated master received two targeted transparency corrections. The prompts are recorded in `icon-prompt.txt`. Sizes were exported using macOS `sips`; the ICNS was assembled and read back with `iconutil`. The ICO stores the three RGBA PNG variants. No third-party logo or stock image was used as an input.

Regenerate PNG sizes with `sips -z SIZE SIZE Unity/Assets/Brand/RoadSenseIcon.png --out OUTPUT.png`. For ICNS, generate `icon_16x16`, `icon_32x32`, `icon_128x128`, `icon_256x256`, and `icon_512x512` PNGs plus their `@2x` variants in a `.iconset` directory, then run `iconutil -c icns DIRECTORY.iconset -o RoadSense.icns`.
