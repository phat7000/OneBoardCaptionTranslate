using NAudio.Wave;

namespace LiveCaptionsTranslator.speech
{
    /// <summary>Captures the current default Windows recording endpoint as normalized PCM.</summary>
    public sealed class MicrophoneAudioSource : IAudioCaptureSource
    {
        private WaveInEvent? waveIn;
        private bool running;

        public string DisplayName => "Microphone";
        public int SampleRate => 16000;
        public short BitsPerSample => 16;
        public short Channels => 1;
        public event EventHandler<AudioChunkEventArgs>? AudioAvailable;
        public event EventHandler<AudioCaptureErrorEventArgs>? CaptureFailed;

        public void Start()
        {
            if (running)
                return;

            try
            {
                waveIn = new WaveInEvent
                {
                    WaveFormat = new WaveFormat(SampleRate, BitsPerSample, Channels),
                    BufferMilliseconds = 100,
                    NumberOfBuffers = 3
                };
                waveIn.DataAvailable += OnDataAvailable;
                waveIn.RecordingStopped += OnRecordingStopped;
                running = true;
                waveIn.StartRecording();
            }
            catch
            {
                running = false;
                ReleaseWaveIn();
                throw;
            }
        }

        public void Stop()
        {
            if (!running)
                return;

            running = false;
            waveIn?.StopRecording();
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            byte[] copy = new byte[e.BytesRecorded];
            Buffer.BlockCopy(e.Buffer, 0, copy, 0, e.BytesRecorded);
            AudioAvailable?.Invoke(this, new AudioChunkEventArgs(copy));
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            bool stoppedUnexpectedly = running;
            running = false;
            if (stoppedUnexpectedly)
            {
                string detail = e.Exception?.Message ?? "The default microphone became unavailable.";
                CaptureFailed?.Invoke(this, new AudioCaptureErrorEventArgs(
                    $"Microphone capture stopped: {detail}", e.Exception));
            }
        }

        private void ReleaseWaveIn()
        {
            if (waveIn == null)
                return;
            waveIn.DataAvailable -= OnDataAvailable;
            waveIn.RecordingStopped -= OnRecordingStopped;
            waveIn.Dispose();
            waveIn = null;
        }

        public void Dispose()
        {
            Stop();
            ReleaseWaveIn();
        }
    }
}
