namespace LiveCaptionsTranslator.speech
{
    public interface IAudioCaptureSource : IDisposable
    {
        string DisplayName { get; }
        int SampleRate { get; }
        short BitsPerSample { get; }
        short Channels { get; }
        event EventHandler<AudioChunkEventArgs>? AudioAvailable;
        event EventHandler<AudioCaptureErrorEventArgs>? CaptureFailed;

        void Start();
        void Stop();
    }

    public sealed class AudioChunkEventArgs(byte[] data) : EventArgs
    {
        public byte[] Data { get; } = data;
    }

    public sealed class AudioCaptureErrorEventArgs(string message, Exception? exception = null) : EventArgs
    {
        public string Message { get; } = message;
        public Exception? Exception { get; } = exception;
    }
}
