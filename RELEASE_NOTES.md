# OneBoard Capture Translate 1.2.0

This minor release adds professional conference and seminar audio-device input while preserving all existing capture and provider workflows.

### New

- Adds **External Audio Input** as a third audio source alongside System Audio and Microphone.
- Adds a Windows recording-device selector for USB mixers, USB audio interfaces, virtual audio endpoints, and other standard capture devices.
- Persists both the stable Windows endpoint ID and friendly display name, so devices with identical names remain distinguishable internally.
- Adds an in-app **Refresh** action so newly connected recording devices appear without restarting OneBoard.
- Supports conference workflows such as mixer AUX/REC/LINE output through a USB interface, or direct USB audio from a mixer.

### Audio behavior and reliability

- Reuses the existing NAudio pipeline and feeds the same normalized PCM stream to Azure Speech and Google Speech.
- Normalizes external input and System Audio to 16 kHz, 16-bit, mono PCM, including safe multi-channel downmixing and resampling from common device formats.
- Shows the selected external device name in listening status messages.
- Fails clearly when a saved device is missing or disconnected; it never silently switches to Microphone or System Audio.
- Serializes speech-provider and audio-source restarts so previous capture and recognition sessions are disposed before replacements start.

### Existing capabilities retained

- System Audio via WASAPI loopback.
- Microphone capture.
- Windows Live Captions, Azure Speech, and Google Speech.
- Existing translation providers, settings, API keys, history, and language selections.

### Release information

- External inputs are standard Windows recording endpoints; no specific mixer or USB-interface model is certified by this release.
- Windows binaries are unsigned and may trigger Windows SmartScreen warnings.
- Cloud recognition still requires user-supplied credentials.

---

## OneBoard Capture Translate 1.1.1

This patch release adds desktop/system-audio capture for cloud speech recognition and improves the responsive provider and language controls.

### Audio and speech recognition

- Adds System Audio capture through Windows WASAPI loopback using the current Windows output device.
- Enables Azure Speech to recognize desktop/system audio.
- Enables Google Speech to recognize desktop/system audio.
- Keeps microphone capture supported.
- Adds manual Audio Source selection independently of the selected speech provider.
- Normalizes captured audio to a 16 kHz, 16-bit, mono PCM pipeline for Azure Speech and Google Speech.
- Improves speech status messages to show the active Speech Provider and Audio Source.

### Provider and language settings

- Improves the responsive provider/language settings layout with a five-column wide layout and a 3+2 compact layout.
- Reduces unnecessary clipping of provider and language values.
- Adds full-value tooltips for provider, audio-source, and language selections.

### Compatibility and release information

- Preserves Windows Live Captions and all existing translation providers.
- Keeps settings and history under `%LOCALAPPDATA%\OneBoard\OneBoard Capture Translate\`.
- Automatic updates remain disabled.
- Windows binaries are unsigned and may trigger Windows SmartScreen warnings.
- Cloud-provider calls require user-supplied credentials and were not exercised as part of the credential-free automated release build.

---

## OneBoard Capture Translate 1.1.0

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
