# Firmware Binaries

This folder contains compiled firmware binaries (.bin files) for distribution.

## How to Export Binaries from Arduino IDE

1. Open the firmware file in Arduino IDE:
   - For FeatherS3: `FeatherS3 scripts/Arduino version/padawan fs3/padawan_fs3.ino`
   - For FeatherS3D: `FeatherS3 scripts/Arduino version/padavan_fs3d/padavan_fs3d.ino`

2. Select the correct board:
   - **Unexpected Maker FeatherS3** (for padawan_fs3)
   - **Unexpected Maker FeatherS3D** (for padavan_fs3d)

3. Verify/Compile the sketch (Ctrl+R)

4. Export the compiled binary:
   - Go to: **Sketch → Export compiled Binary**
   - The `.bin` file will be created in the same folder as the `.ino` file

5. Copy the `.bin` file to this folder:
   - Rename it appropriately (e.g., `padawan_fs3-v1.0.0.bin`, `padavan_fs3d-v1.0.2.bin`)
   - Place it in this `releases/firmware/` directory

## File Naming Convention

- `padawan_fs3-v{VERSION}.bin` - For FeatherS3 boards
- `padavan_fs3d-v{VERSION}.bin` - For FeatherS3D boards

Example:
- `padawan_fs3-v1.0.0.bin`
- `padavan_fs3d-v1.0.2.bin`

## Including in GitHub Releases

When creating a GitHub release:
1. Attach the `.bin` files as release assets
2. Users can download and flash them manually, or
3. The Pad-Avan Force app can automatically detect and download them for firmware updates

