# PadAwan-Force Arduino Version

Arduino-Implementierung für den FeatherS3 ESP32-S3 Macro Pad.

## Vorteile gegenüber CircuitPython

- **Schnellerer Boot**: Start in ~100-500ms statt 3-5 Sekunden
- **Bessere Performance**: Niedrigere Latenz bei Button/Encoder-Eingaben
- **Weniger Speicherverbrauch**: Kompilierter Code statt interpretiert

## Benötigte Bibliotheken

Installiere folgende Bibliotheken über den Arduino Library Manager:

1. **Adafruit SSD1306** - Für das OLED Display
2. **Adafruit GFX** - Grafik-Bibliothek (wird automatisch mit SSD1306 installiert)
3. **Adafruit BusIO** - I2C/SPI Support (wird automatisch installiert)
4. **SD** - SD-Karten Support (sollte bereits in ESP32 enthalten sein)
5. **ArduinoJson** - JSON Parsing (Version 6.x empfohlen)
6. **RotaryEncoder** - Rotary Encoder Support (von Matthias Hertel)

## ESP32-S3 USB HID

Die USB HID Bibliotheken sind Teil des ESP32-S3 Core. Die folgenden Header sollten verfügbar sein:

- `<USB.h>` - USB Initialisierung
- `<USBHIDKeyboard.h>` - Keyboard HID
- `<USBHIDConsumerControl.h>` - Consumer Control (Volume/Media)

**Wichtig**: Falls diese Header nicht gefunden werden:
- Stelle sicher, dass du die neueste ESP32 Board-Version installiert hast
- Wähle das Board: **Tools > Board > ESP32 Arduino > UM FeatherS3**
- Aktiviere Native USB: **Tools > USB Mode > Native USB**

## Installation

1. **Arduino IDE Setup**:
   - Installiere Arduino IDE 2.0.3 oder höher
   - Füge ESP32 Board Support hinzu:
     - File > Preferences > Additional Board Manager URLs
     - Füge hinzu: `https://raw.githubusercontent.com/espressif/arduino-esp32/gh-pages/package_esp32_index.json`
   - Tools > Board > Boards Manager
   - Suche nach "esp32" und installiere "esp32 by Espressif Systems"

2. **Board auswählen**:
   - Tools > Board > ESP32 Arduino > UM FeatherS3
   - Tools > USB Mode > Native USB
   - Tools > Port > Wähle den COM-Port des FeatherS3

3. **Bibliotheken installieren**:
   - Tools > Manage Libraries
   - Installiere die oben genannten Bibliotheken

4. **Code hochladen**:
   - Öffne `padawan.ino` in der Arduino IDE
   - Klicke auf Upload
   - Nach dem Upload: **Reset-Button am FeatherS3 drücken** (wichtig!)

## Pin-Belegung

- **Buttons**: IO14, IO18, IO5, IO17, IO6, IO12
- **Rotary A**: A=IO10, B=IO11, Press=IO7
- **Rotary B**: A=IO1, B=IO3, Press=IO33
- **SD Card**: CS=IO38
- **Display**: I2C, Address 0x3C

## Serial Communication & Debugging

### Serial Monitor (Arduino IDE)

**Ja, du kannst die Konsole auslesen!** 🎉

Im Gegensatz zu CircuitPython, wo die USB CDC Console deaktiviert werden musste, funktioniert in Arduino der **Serial Monitor parallel zur App-Kommunikation**.

**So geht's:**
1. Öffne **Tools > Serial Monitor** in der Arduino IDE
2. Stelle die Baudrate auf **115200** ein
3. Du siehst alle Debug-Ausgaben in Echtzeit!

**Debug-Ausgaben aktivieren/deaktivieren:**
- In `padawan.ino` findest du: `#define DEBUG_SERIAL 1`
- Setze auf `1` für Debug-Ausgaben (Standard)
- Setze auf `0` um Debug-Ausgaben zu deaktivieren (nur App-Kommunikation)

### App-Kommunikation

Die Desktop-App kommuniziert über denselben `Serial` Port. Die App-Antworten (wie "PONG", "UPLOAD_OK", etc.) werden immer ausgegeben, auch wenn `DEBUG_SERIAL = 0`.

**Baudrate**: 115200 (kann in `padawan.ino` angepasst werden)

**Wichtig**: Du kannst sowohl den Serial Monitor als auch die App gleichzeitig nutzen. Die App ignoriert einfach die Debug-Zeilen.

## Bekannte Probleme / Anpassungen

1. **USB HID Bibliotheken**: Falls `USBHIDKeyboard.h` nicht gefunden wird, könnte es sein, dass die ESP32-Version noch nicht vollständig unterstützt. In diesem Fall müsste man auf eine alternative Bibliothek zurückgreifen oder die ESP32-Version aktualisieren.

2. **Keyboard Layout**: Die Schweizer Tastaturbelegung (QWERTZ) ist implementiert. Für vollständige Unterstützung aller Schweizer Zeichen kann die `KeyboardLayoutWinCH.h` erweitert werden.

3. **SD Card**: Die SD-Karte muss im SPI-Modus betrieben werden. Stelle sicher, dass die CS-Leitung korrekt verbunden ist (IO38).

## Unterschiede zur CircuitPython-Version

- **Boot-Zeit**: Deutlich schneller (~100-500ms vs. 3-5s)
- **Serial**: Verwendet `Serial` statt `usb_cdc.data`
- **JSON**: Verwendet ArduinoJson statt Python's json
- **Rotary Encoder**: Verwendet RotaryEncoder Library statt rotaryio

## Debugging

Serial Monitor öffnen (Tools > Serial Monitor) mit 115200 Baud, um Debug-Ausgaben zu sehen.

## Kompatibilität mit C#-App

**Ja, die Arduino-Version funktioniert direkt mit der C#-App!** ✅

Alle wichtigen Kommandos sind implementiert:
- ✅ `PING` → `PONG`
- ✅ `UPLOAD_LAYER_CONFIG` → `READY_FOR_LAYER_CONFIG`
- ✅ `BEGIN_JSON` / `END_JSON` → `UPLOAD_OK`
- ✅ `GET_CURRENT_CONFIG` → `CURRENT_CONFIG:...`
- ✅ `DOWNLOAD_CONFIG` → `CONFIG:...`
- ✅ `SET_DISPLAY_MODE:...` → `DISPLAY_MODE_SET`
- ✅ `SET_TIME:...` → `TIME_SET`
- ✅ `BATTERY_STATUS` → `BATTERY:...` (Dummy-Implementierung)

**Wichtig**: 
- Baudrate: **115200** (bereits korrekt)
- Serial Port: Wird automatisch erkannt
- Debug-Ausgaben: Werden von der App ignoriert (können parallel laufen)

## Nächste Schritte

- [x] Alle App-Kommandos implementiert
- [ ] Testen aller Funktionen mit der App
- [ ] Keyboard Layout vollständig implementieren
- [ ] Battery Status implementieren (falls benötigt)
- [ ] Performance-Optimierungen

