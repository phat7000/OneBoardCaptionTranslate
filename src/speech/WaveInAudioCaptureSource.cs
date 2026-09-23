using NAudio.Wave;

namespace LiveCaptionsTranslator.speech
{
    /// <summary>Single shared 16 kHz mono PCM capture implementation used by all cloud STT adapters.</summary>
    public sealed class WaveInAudioCaptureSource : IAudioCaptureSource
    {
        private readonly WaveInEvent waveIn;
        private bool running;

        public WaveInAudioCaptureSource()
        {
            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels),
                BufferMilliseconds = 100,
                NumberOfBuffers = 3
            };
            waveIn.DataAvailable += OnDataAvailable;
        }

        public int SampleRate => 16000;
        public short BitsPerSample => 16;
        public short Channels => 1;
        public event EventHandler<AudioChunkEventArgs>? AudioAvailable;

        public void Start()
        {
            if (running)
                return;
            waveIn.StartRecording();
            running = true;
        }

        public void Stop()
        {
            if (!running)
                return;
            waveIn.StopRecording();
            running = false;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            byte[] copy = new byte[e.BytesRecorded];
            Buffer.BlockCopy(e.Buffer, 0, copy, 0, e.BytesRecorded);
            AudioAvailable?.Invoke(this, new AudioChunkEventArgs(copy));
        }

        public void Dispose()
        {
            Stop();
            waveIn.DataAvailable -= OnDataAvailable;
            waveIn.Dispose();
        }
    }
}
