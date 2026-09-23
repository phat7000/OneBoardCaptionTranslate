# OneBoard Capture Translate 1.1.0

Development release notes for the provider and adaptive-display upgrade. No public package was created by this implementation run.

## Highlights

- Independent, manual selection of speech-recognition and translation providers.
- Windows Live Captions retained; Azure Speech and Google Cloud Speech-to-Text added for streaming partial and final results.
- Microsoft Translator, official Google Cloud Translation Basic v2, TranslatePlus, and Langbly added without removing existing translation providers.
- Shared catalog of 87 canonical languages with provider-aware translation codes and BCP-47 speech locales.
- Adaptive original/translation viewports that reveal more history as panes grow.
- Stable finalized rows plus one coalesced mutable partial row to reduce visual jitter.
- New cloud API keys protected at rest with Windows DPAPI; Google Speech stores only the service-account file path.
- Product executable and metadata updated to `OneBoardCaptureTranslate.exe` version 1.1.0.

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
