namespace LiveCaptionsTranslator.speech
{
    public interface IAudioCaptureSource : IDisposable
    {
        int SampleRate { get; }
        short BitsPerSample { get; }
        short Channels { get; }
        event EventHandler<AudioChunkEventArgs>? AudioAvailable;

        void Start();
        void Stop();
    }

    public sealed class AudioChunkEventArgs(byte[] data) : EventArgs
    {
        public byte[] Data { get; } = data;
    }
}
