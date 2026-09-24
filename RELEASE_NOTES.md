# OneBoard Capture Translate 1.1.0

This release expands cloud translation and speech recognition, broadens language support, and makes live transcript rendering adapt to the available pane height.

### Translation

- Adds Microsoft Translator.
- Adds official Google Cloud Translation Basic v2.
- Adds TranslatePlus.
- Adds Langbly.

### Speech Recognition

- Adds Azure Speech continuous recognition.
- Adds Google Cloud Speech-to-Text streaming recognition.
- Keeps Windows Live Captions available.
- Adds explicit manual speech-provider selection with no automatic switching.
- Routes direct cloud speech-to-text through `ISpeechRecognitionProvider`.

### Languages

- Expands source and target selection through a shared catalog of 87 canonical languages.
- Keeps the speech source language independent from the translation target language.
- Adds provider-aware translation codes and BCP-47 speech locales.
- Keeps the catalog architecture ready for future language additions and provider discovery.

### Display

- Makes the original and translation viewports adapt independently to their available height.
- Shows more recent content when a pane is taller instead of enforcing a fixed sentence cap.
- Keeps finalized transcript rows stable while updating one mutable partial row.
- Coalesces partial updates to reduce visible jumping and flicker.

### Compatibility

- Preserves existing translation providers and Windows Live Captions.
- Migrates compatible settings and history from the former application data directory on first use.
- Protects new cloud API keys at rest with Windows DPAPI; Google Speech stores only the service-account file path.
- Updates the executable and product metadata to `OneBoardCaptureTranslate.exe` version 1.1.0.

## System requirements

- Windows 11 22H2 or newer for Windows Live Captions.
- A microphone, network access, and valid provider credentials for cloud speech recognition.
- Network access and valid credentials for paid cloud translation providers.

## User data

Settings and history are stored under `%LOCALAPPDATA%\OneBoard\OneBoard Capture Translate\`. On first use, compatible files from the former application data directory are copied forward automatically.

## Release information

- Automatic updates remain disabled.
- Official source: https://github.com/phat7000/OneBoardCaptionTranslate
- Upstream: https://github.com/SakiRinn/LiveCaptions-Translator
- Windows binaries are unsigned and may trigger Windows SmartScreen warnings.

Cloud-provider calls require user-supplied credentials and were not exercised as part of the credential-free automated release build.
