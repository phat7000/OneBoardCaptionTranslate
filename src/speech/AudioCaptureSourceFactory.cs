namespace LiveCaptionsTranslator.speech
{
    public static class AudioCaptureSourceFactory
    {
        public static IAudioCaptureSource Create(AudioSourceType sourceType) => sourceType switch
        {
            AudioSourceType.SystemAudio => new WasapiLoopbackAudioSource(),
            AudioSourceType.Microphone => new MicrophoneAudioSource(),
            _ => throw new ArgumentOutOfRangeException(nameof(sourceType), sourceType, "Unsupported audio source.")
        };
    }
}
