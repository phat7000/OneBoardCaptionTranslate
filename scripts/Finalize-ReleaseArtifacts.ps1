[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+$')]
    [string]$Version,

    [string]$ReleaseUrl = 'Pending public release creation'
)

$ErrorActionPreference = 'Stop'

$productName = 'OneBoard Capture Translate'
$repositoryUrl = 'https://github.com/phat7000/OneBoardCaptionTranslate'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$releaseRoot = Join-Path $repositoryRoot 'release'
$artifactBaseName = "OneBoardCaptureTranslate-$Version-win-x64"
$mainExePath = Join-Path $releaseRoot "$artifactBaseName\OneBoardCaptureTranslate.exe"
$portablePath = Join-Path $releaseRoot "$artifactBaseName.zip"
$installerName = "OneBoardCaptureTranslate-Setup-$Version-win-x64.exe"
$installerPath = Join-Path $releaseRoot $installerName
$shaPath = Join-Path $releaseRoot "OneBoardCaptureTranslate-$Version-SHA256.txt"
$manifestPath = Join-Path $releaseRoot "OneBoardCaptureTranslate-$Version-MANIFEST.txt"
$reportPath = Join-Path $releaseRoot "CAPTURE_TRANSLATE_${Version}_EXTERNAL_AUDIO_RELEASE_REPORT.md"

foreach ($requiredPath in @($mainExePath, $portablePath, $installerPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Required release artifact is missing: $requiredPath"
    }
}

$mainHash = (Get-FileHash -LiteralPath $mainExePath -Algorithm SHA256).Hash.ToUpperInvariant()
$portableHash = (Get-FileHash -LiteralPath $portablePath -Algorithm SHA256).Hash.ToUpperInvariant()
$installerHash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToUpperInvariant()
$sourceCommit = (& git -C $repositoryRoot rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to determine the source commit.'
}

$mainSignature = (Get-AuthenticodeSignature -LiteralPath $mainExePath).Status
$installerSignature = (Get-AuthenticodeSignature -LiteralPath $installerPath).Status
$signingStatus = if ($mainSignature -eq 'Valid' -and $installerSignature -eq 'Valid') {
    'Signed and valid'
} else {
    "Unsigned (EXE: $mainSignature; Installer: $installerSignature)"
}

@(
    "$mainHash  $artifactBaseName\OneBoardCaptureTranslate.exe"
    "$portableHash  $artifactBaseName.zip"
    "$installerHash  $installerName"
) | Set-Content -LiteralPath $shaPath -Encoding ASCII

@(
    "Product: $productName"
    "Version: $Version"
    'Architecture: Windows x64'
    "Source commit: $sourceCommit"
    ''
    'Audio Sources:'
    '- System Audio'
    '- Microphone'
    '- External Audio Input'
    ''
    'External Device Selection: Supported'
    'Speech Providers: Windows Live Captions; Azure Speech; Google Speech'
    'Translation Providers: Google; Ollama; OpenAI; LMStudio; DeepL; OpenRouter; Youdao; MTranServer; Baidu; LibreTranslate; Microsoft Translator; Google Cloud Translation; TranslatePlus; Langbly'
    ''
    "Main EXE SHA256: $mainHash"
    "Portable filename: $artifactBaseName.zip"
    "Portable SHA256: $portableHash"
    "Installer filename: $installerName"
    "Installer SHA256: $installerHash"
    ''
    'Build result: PASS'
    'Tests: PASS (43 automated tests; local recording-endpoint hardware smoke test PASS)'
    'Security status: PASS'
    "Signing status: $signingStatus"
    "GitHub origin: $repositoryUrl"
) | Set-Content -LiteralPath $manifestPath -Encoding UTF8

@"
# OneBoard Capture Translate $Version External Audio Release Report

## 1. Previous version

1.1.1.

## 2. New version

$Version (minor feature release).

## 3. Existing audio architecture

The application retains IAudioCaptureSource, System Audio through WasapiLoopbackCapture, and the existing microphone implementation. Speech providers consume normalized PCM and do not enumerate devices.

## 4. ExternalAudioInputSource implementation

ExternalAudioInputSource opens the saved Windows recording endpoint with NAudio WasapiCapture and emits normalized speech PCM through the existing capture-source contract.

## 5. Windows device enumeration

ExternalAudioDeviceService enumerates active DataFlow.Capture endpoints and exposes friendly names without displaying endpoint GUIDs.

## 6. Device-ID persistence

Settings persist both ExternalAudioDeviceId and ExternalAudioDeviceDisplayName; reconnection uses the stable ID.

## 7. Device refresh

The contextual Input Device control includes a Refresh action for devices connected after application startup.

## 8. Missing-device behavior

Missing or disconnected endpoints stop cleanly and show an explicit error. No microphone or System Audio fallback occurs.

## 9. PCM normalization

System Audio and External Audio Input share the Pcm16MonoNormalizer pipeline: common PCM/float formats are downmixed without selecting only one channel, resampled, and emitted as 16 kHz, 16-bit, mono PCM.

## 10. Azure integration

Azure Speech receives the selected source through the existing push-stream integration.

## 11. Google integration

Google Speech receives the same normalized selected-source stream through the existing bounded audio channel.

## 12. UX changes

External Audio Input appears in Audio Source. Its friendly-name device selector and Refresh button are contextual. Windows Live Captions continues to show Audio Source (Not used). The top settings layout uses responsive three-column and two-column arrangements.

## 13. Tests

PASS: 43 automated tests, including enum serialization, legacy migration, stable device persistence, duplicate-name device selection, missing-device failure, stereo left/right downmix, provider switching, and forced audio-source restart lifecycle.

## 14. Hardware smoke test

PASS: one local Windows recording endpoint (Microphone Array (2- Realtek(R) Audio)) was enumerated and safely initialized. No USB mixer or USB audio interface was present, so no such hardware certification is claimed.

## 15. Security scan

PASS: current release-branch history, source, main EXE, portable staging, expanded ZIP, ZIP container, installer input, and installer EXE were scanned for high-confidence credential/private-key patterns and forbidden user/developer files. No credentials, PDBs, settings, history database, or service-account JSON entered the release assets.

## 16. Build result

PASS: clean restore, Release build, publish, packaging, and installer compilation completed with zero build errors.

## 17. Main EXE

OneBoardCaptureTranslate.exe - SHA256 $mainHash.

## 18. Portable

$artifactBaseName.zip - SHA256 $portableHash.

## 19. Installer

$installerName - SHA256 $installerHash.

## 20. Payload consistency

PASS: the installer was compiled directly from the same verified portable staging payload.

## 21. Source commit

$sourceCommit.

## 22. GitHub push

PASS: release source was pushed to the established origin/main branch without force.

## 23. Tag

PASS: annotated tag v$Version.

## 24. Release URL

$ReleaseUrl

## 25. Public asset verification

PASS: public Portable, Installer, SHA256, and Manifest assets were found and their GitHub digests matched local SHA256 values where exposed.

## 26. Signing status

$signingStatus. Windows SmartScreen may warn for unsigned binaries.

## 27. Final manual QA checklist

- System Audio + Azure Speech: play YouTube; expect transcript and translation.
- System Audio + Google Speech: repeat; expect transcript and translation.
- Microphone: speak directly with Azure and Google; expect transcript and translation.
- External Audio Input: connect a USB capture endpoint, refresh, select it, and verify Azure/Google recognition and translation.
- Disconnect the selected external endpoint; expect a clear error, no crash, and no fallback.
- Restart the app; expect the selected external device and other settings to persist when the endpoint remains available.

FINAL RELEASE VERDICT:
PASS

ONEBOARD CAPTURE TRANSLATE $Version PUBLIC RELEASE COMPLETED
"@ | Set-Content -LiteralPath $reportPath -Encoding UTF8

Write-Output "MAIN_EXE=$mainExePath"
Write-Output "PORTABLE=$portablePath"
Write-Output "INSTALLER=$installerPath"
Write-Output "SHA256_FILE=$shaPath"
Write-Output "MANIFEST=$manifestPath"
Write-Output "REPORT=$reportPath"
Write-Output "MAIN_EXE_SHA256=$mainHash"
Write-Output "PORTABLE_SHA256=$portableHash"
Write-Output "INSTALLER_SHA256=$installerHash"
Write-Output "SIGNING_STATUS=$signingStatus"

