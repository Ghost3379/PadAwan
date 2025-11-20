# PadAwan-Force

A powerful, customizable macro pad with 6 buttons and 2 rotary encoders, featuring a desktop configuration application built with Avalonia UI.

🌐 **Website**: [https://padawan-force.base44.app](https://padawan-force.base44.app)

![PadAwan-Force](https://img.shields.io/badge/Version-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Avalonia](https://img.shields.io/badge/Avalonia-11.0-green)
![Arduino](https://img.shields.io/badge/Arduino-ESP32--S3-orange)

## 🎯 Features

### Hardware
- **6 Programmable Buttons** - Assign any action, key combo, or special key
- **2 Rotary Encoders** - Volume control, scrolling, or custom actions
- **OLED Display** - Shows layer, battery status, or time
- **SD Card Support** - Store configurations directly on the device
- **USB HID** - Works as a standard keyboard/consumer control device

### Desktop Application
- **Cross-Platform UI** - Built with Avalonia UI (Windows, macOS, Linux)
- **Layer Management** - Create and manage multiple configuration layers
- **Visual Configuration** - Easy-to-use interface for buttons and knobs
- **Real-time Updates** - Configure your device without reflashing
- **Configuration Import/Export** - Save and load configurations from files
- **Update System** - Check for firmware and software updates (coming soon)

## 📋 Requirements

### Hardware
- Adafruit Feather ESP32-S3
- 6x Tactile buttons
- 2x Rotary encoders with push buttons
- SSD1306 OLED display (128x64)
- MicroSD card module
- USB-C cable

### Software
- **Arduino IDE** (2.0+) with ESP32 board support
- **.NET 8.0 SDK** (for building the desktop app)
- **Visual Studio 2022** or **JetBrains Rider** (recommended)

## 🚀 Getting Started

### 1. Hardware Setup

1. Solder components according to the wiring diagram (TODO: Add diagram)
2. Insert microSD card (formatted as FAT32)
3. Connect via USB-C

### 2. Firmware Installation

1. Open `FeatherS3 scripts/Arduino version/padawan fs3d/padawan_fs3d/padawan_fs3d.ino` in Arduino IDE
2. Install required libraries:
   - **TinyUSB** (for USB HID support)
   - **ArduinoJson** (v6.x recommended)
   - **Adafruit SSD1306** (for OLED display)
   - **SD** (ESP32 built-in)
   - **SPI** (ESP32 built-in)
   - **Wire** (ESP32 built-in)
3. Select board: **Adafruit Feather ESP32-S3**
4. Upload the sketch

### 3. Desktop Application

#### Building from Source

```bash
cd PadAwan-Force
dotnet restore
dotnet build
dotnet run
```

#### Running the Application

1. Connect your PadAwan-Force device via USB
2. Launch the desktop application
3. Select the COM port from the dropdown
4. Click "Connect"
5. Start configuring your buttons and knobs!

## 📖 Usage

### Button Configuration

1. Click on any button (1-6) in the main window
2. Select an action:
   - **None** - No action
   - **Type Text** - Type a custom string
   - **Special Key** - Send a special key (Enter, Tab, Arrow keys, etc.)
   - **Key Combo** - Create multi-key combinations (Ctrl+C, Alt+Tab, etc.)
   - **Layer Switch** - Switch to another layer
   - **Windows Key** - Open Start menu / Windows shortcuts

### Knob Configuration

1. Click on knob A or B
2. Configure rotation actions:
   - **Volume Control** - Increase/decrease system volume
   - **Scroll** - Scroll up/down
   - **Custom Actions** - Assign any button action
3. Configure press action:
   - Same options as buttons
   - Supports key combos and special keys

### Layer Management

- Create multiple layers for different use cases (gaming, productivity, media, etc.)
- Switch layers using button actions
- Each layer has independent button and knob configurations

### Display Modes

Configure the OLED display to show:
- **Layer** - Current active layer
- **Battery** - Battery percentage
- **Time** - Current time (requires time sync from desktop app)
- **Off** - Display disabled

## 🏗️ Project Structure

```
PadAwan-Force/
├── PadAwan-Force/              # Desktop application (Avalonia UI)
│   ├── Models/                 # Data models
│   ├── ViewModels/             # MVVM view models
│   ├── Views/                  # UI views (XAML)
│   └── Assets/                 # Icons and resources
│
└── FeatherS3 scripts/           # Firmware
    └── Arduino version/
        └── padawan fs3d/       # ESP32-S3 firmware
            └── padawan_fs3d.ino
```

## 🔧 Configuration

### Serial Communication Protocol

The device communicates via USB Serial at 115200 baud:

- `PING` → `PONG` (connection test)
- `GET_VERSION` → `VERSION:1.0.0` (firmware version)
- `DOWNLOAD_CONFIG` → `CONFIG:{json}` (download configuration)
- `GET_CURRENT_CONFIG` → `CURRENT_CONFIG:{json}` (get active config)
- `UPLOAD_LAYER_CONFIG` → `READY_FOR_LAYER_CONFIG` (prepare for upload)
- `BEGIN_JSON` ... `END_JSON` → `UPLOAD_OK` (upload configuration)
- `BATTERY_STATUS` → `BATTERY:percentage,voltage,charging` (battery info)

### Configuration File Format

Configurations are stored as JSON:

```json
{
  "version": "1.0",
  "currentLayer": 1,
  "layers": [
    {
      "id": 1,
      "name": "Layer 1",
      "buttons": {
        "1": {
          "action": "Type Text",
          "key": "Hello World"
        }
      },
      "knobs": {
        "A": {
          "ccwAction": "Decrease Volume",
          "cwAction": "Increase Volume",
          "pressAction": "Special Key",
          "pressKey": "ENTER"
        }
      }
    }
  ]
}
```

## 🐛 Troubleshooting

### Device Not Connecting

1. Check USB cable (data cable required, not charge-only)
2. Verify COM port in Device Manager
3. Ensure drivers are installed (CP210x or CH340)
4. Try different USB port

### Buttons Not Responding

1. Check button wiring
2. Verify configuration is uploaded to device
3. Check if device is in correct layer
4. Test with serial monitor for debug output

### Display Not Working

1. Verify I2C connections (SDA/SCL)
2. Check display address (usually 0x3C)
3. Ensure display is enabled in configuration

## 🔮 Roadmap

- [x] Basic button and knob configuration
- [x] Layer management
- [x] Configuration import/export
- [x] Update system UI
- [ ] GitHub API integration for updates
- [ ] esptool integration for firmware flashing
- [ ] Custom key combo builder improvements
- [ ] Macro recording/playback
- [ ] RGB LED support (if hardware added)
- [ ] Wireless mode (Bluetooth HID)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [Avalonia UI](https://avaloniaui.net/)
- Uses [TinyUSB](https://github.com/hathach/tinyusb) for USB HID support
- Inspired by the macro pad community

## 🌐 Website

Visit our website for more information, documentation, and updates:
- **Website**: [https://padawan-force.base44.app](https://padawan-force.base44.app)

## 📧 Contact

For questions, issues, or feature requests, please open an issue on GitHub.

---

**Made with ❤️ for the macro pad community**

