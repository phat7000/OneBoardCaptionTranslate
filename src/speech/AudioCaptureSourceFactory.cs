namespace LiveCaptionsTranslator.speech
{
    public static class AudioCaptureSourceFactory
    {
        public static IAudioCaptureSource Create(
            AudioSourceType sourceType,
            string? externalDeviceId = null,
            string? externalDeviceDisplayName = null) => sourceType switch
            {
                AudioSourceType.SystemAudio => new WasapiLoopbackAudioSource(),
                AudioSourceType.Microphone => new MicrophoneAudioSource(),
                AudioSourceType.ExternalAudioInput => new ExternalAudioInputSource(
                    externalDeviceId,
                    externalDeviceDisplayName),
                _ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, "Unsupported audio source.")
            };
    }
}
