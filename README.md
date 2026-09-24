<div align="center">

<img src="src/LiveCaptions-Translator.ico" width="128" height="128" alt="OneBoard Capture Translate icon"/>

# OneBoard Capture Translate

### *Lightweight real-time translation for Windows.*

[![Windows 11](https://img.shields.io/badge/platform-Windows%2011-1E9BFA?logo=windows11)](https://www.microsoft.com/windows/windows-11)

**English** | [中文](README_zh-CN.md)

</div>

## Overview

OneBoard Capture Translate connects a manually selected speech-recognition provider to an independently selected translation provider and displays the result in the main window or configurable overlay.

```text
Speech provider -> OneBoard Capture Translate -> translation provider -> adaptive display
```

- Product page: https://oneboard.io.vn/
- Official OneBoard source: https://github.com/phat7000/OneBoardCaptionTranslate
- Upstream project: https://github.com/SakiRinn/LiveCaptions-Translator

## Features

- Windows Live Captions, Azure Speech, and Google Cloud Speech-to-Text with explicit provider selection.
- System Audio, Microphone, and selected External Audio Input capture, including USB mixers and standard Windows recording endpoints.
- Existing translation providers plus Microsoft Translator, official Google Cloud Translation Basic v2, TranslatePlus, and Langbly.
- 87-language canonical catalog with provider-aware translation codes and BCP-47 speech locales.
- Adaptive, bottom-anchored original and translation panes with stable partial-result updates.
- Two-pane overlay with Original on top and Translation on the bottom by default.
- Switch Order, Original Only, and Translation Only modes.
- Font size and bold controls, text color, outline/stroke, background color, and opacity.
- Movable and resizable overlay with click-through mode.
- Translation context options.
- Translation history and CSV export.
- Light and dark theme support.
- Per-user settings and history stored outside the application folder.

Screenshots will be added after product-owner QA. Older screenshots that predate the Capture branding and adaptive layout are intentionally not shown here.

## System requirements

- Windows 11 22H2 or later.
- Windows Live Captions support and the required speech pack when that provider is selected.
- A microphone, system output, or Windows recording endpoint and provider credentials when Azure Speech or Google Speech is selected.
- Windows x64 or ARM64 for CI-produced builds.

The portable package is self-contained. No separate .NET installation is required.

## Quick Start

1. Download the Portable ZIP or Installer.
2. If using the ZIP, extract it completely before running the application.
3. Launch `OneBoardCaptureTranslate.exe`.
4. Select the speech provider and source locale. For Windows Live Captions, complete Windows setup if prompted.
5. Select the translation provider and target language.
6. Open **Provider Settings** and enter only the credentials required by the selected cloud provider.
7. Open **Overlay** if desired.

Windows Live Captions can include microphone audio through its own settings. Translation providers may have their own network, account, API-key, or usage requirements.

## User data

Settings and translation history are stored at:

```text
%LOCALAPPDATA%\OneBoard\OneBoard Capture Translate\
```

Removing the portable folder or uninstalling the application does not automatically remove settings or history stored in LocalAppData.

## Updates and release authenticity

Automatic updates are disabled in v1.1. Obtain releases only from the official OneBoard source repository or product page and verify published SHA256 values.

Windows binaries are unsigned and may trigger Windows SmartScreen warnings.

## Source, license, and upstream attribution

The official OneBoard source repository is:

https://github.com/phat7000/OneBoardCaptionTranslate

OneBoard Capture Translate is based on [SakiRinn/LiveCaptions-Translator](https://github.com/SakiRinn/LiveCaptions-Translator), licensed under the Apache License 2.0. It contains modifications and is not an official upstream release. The upstream authors do not endorse this modified product.

See [LICENSE](LICENSE), [UPSTREAM_ATTRIBUTION.md](UPSTREAM_ATTRIBUTION.md), and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
