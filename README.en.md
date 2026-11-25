# Crosshair Overlay Studio (Crosshair X Style)

[Versão em Português](./README.md)

> **Language / Stack:** `C# (.NET Framework 4.8 WinForms + Win32 Overlay)` | **Author:** `piercer`

## Overview

Lightweight native Windows crosshair overlay designed for games (`TopMost`, chroma-key transparent, and **click-through** via `WS_EX_TRANSPARENT` so it never steals focus or mouse clicks).

- **Styles:** `Cross`, `Dot`, `Circle`, `CrossCircle`, `TShape`, and `XCross`.
- **Built-in presets:** CS2 Classic, Valorant Dot, T-Shape Pro, Apex/Circle, and X-Cross.
- **Real-time customization:** length, thickness, center gap, center dot, black outline, color picker, opacity, X/Y pixel offset, and target monitor selection.
- **Global hotkeys:**
  - `F8`: Toggle crosshair overlay on/off.
  - `F9`: Show/hide editor window (also available in the System Tray).
- **Auto-save:** stores your setup in `crosshair_config.ini`.

## How to run / build

Run `piercer_crosshair_overlay.exe` directly or rebuild with:

```bat
build.bat
```

---
Developed by **piercer**
