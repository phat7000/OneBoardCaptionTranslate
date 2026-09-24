using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LiveCaptionsTranslator.speech
{
    /// <summary>
    /// Captures the current default Windows render endpoint through WASAPI loopback and
    /// normalizes its native mix format to 16 kHz, 16-bit, mono PCM.
    /// </summary>
    public sealed class WasapiLoopbackAudioSource : IAudioCaptureSource
    {
        private const int OutputBufferSize = 6400;
        private readonly object pipelineLock = new();
        private MMDevice? device;
        private WasapiLoopbackCapture? capture;
        private BufferedWaveProvider? inputBuffer;
        private IWaveProvider? normalizedProvider;
        private readonly byte[] outputBuffer = new byte[OutputBufferSize];
        private bool running;

        public string DisplayName => "System Audio";
        public int SampleRate => 16000;
        public short BitsPerSample => 16;
        public short Channels => 1;
        public event EventHandler<AudioChunkEventArgs>? AudioAvailable;
        public event EventHandler<AudioCaptureErrorEventArgs>? CaptureFailed;

        public void Start()
        {
            lock (pipelineLock)
            {
                if (running)
                    return;

                try
                {
                    using MMDeviceEnumerator enumerator = new();
                    device = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia)
                        ?? throw new InvalidOperationException("Windows has no default audio output device.");
                    capture = new WasapiLoopbackCapture(device);
                    BuildNormalizationPipeline(capture.WaveFormat);
                    capture.DataAvailable += OnDataAvailable;
                    capture.RecordingStopped += OnRecordingStopped;
                    running = true;
                    capture.StartRecording();
                }
                catch (Exception ex)
                {
                    running = false;
                    ReleaseCapture();
                    throw new InvalidOperationException(
                        $"System Audio could not start on the default Windows output device: {ex.Message}", ex);
                }
            }
        }

        private void BuildNormalizationPipeline(WaveFormat sourceFormat)
        {
            if (sourceFormat.Channels < 1)
                throw new NotSupportedException($"Unsupported output audio format: {sourceFormat}.");

            inputBuffer = new BufferedWaveProvider(sourceFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
                DiscardOnBufferOverflow = true,
                ReadFully = false
            };

            ISampleProvider samples = inputBuffer.ToSampleProvider();
            if (samples.WaveFormat.Channels > 1)
                samples = new DownmixToMonoSampleProvider(samples);
            if (samples.WaveFormat.SampleRate != SampleRate)
                samples = new WdlResamplingSampleProvider(samples, SampleRate);
            normalizedProvider = new SampleToWaveProvider16(samples);
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            lock (pipelineLock)
            {
                if (!running || inputBuffer == null || normalizedProvider == null || capture == null)
                    return;

                try
                {
                    inputBuffer.AddSamples(e.Buffer, 0, e.BytesRecorded);
                    double seconds = (double)e.BytesRecorded / capture.WaveFormat.AverageBytesPerSecond;
                    int requested = Math.Max(2, (int)Math.Ceiling(seconds * SampleRate * sizeof(short)));
                    requested -= requested % 2;

                    while (requested > 0)
                    {
                        int count = Math.Min(requested, outputBuffer.Length);
                        int read = normalizedProvider.Read(outputBuffer, 0, count);
                        if (read <= 0)
                            break;

                        byte[] chunk = new byte[read];
                        Buffer.BlockCopy(outputBuffer, 0, chunk, 0, read);
                        AudioAvailable?.Invoke(this, new AudioChunkEventArgs(chunk));
                        requested -= read;
                    }
                }
                catch (Exception ex)
                {
                    CaptureFailed?.Invoke(this, new AudioCaptureErrorEventArgs(
                        $"System Audio conversion failed: {ex.Message}", ex));
                }
            }
        }

        private void OnRecordingStopped(object? sender, StoppedEventArgs e)
        {
            bool stoppedUnexpectedly;
            lock (pipelineLock)
            {
                stoppedUnexpectedly = running;
                running = false;
            }

            if (stoppedUnexpectedly)
            {
                string detail = e.Exception?.Message ?? "The default output device was disconnected.";
                CaptureFailed?.Invoke(this, new AudioCaptureErrorEventArgs(
                    $"System Audio capture stopped: {detail}", e.Exception));
            }
        }

        public void Stop()
        {
            lock (pipelineLock)
            {
                if (!running)
                    return;
                running = false;
                capture?.StopRecording();
            }
        }

        private void ReleaseCapture()
        {
            if (capture != null)
            {
                capture.DataAvailable -= OnDataAvailable;
                capture.RecordingStopped -= OnRecordingStopped;
                capture.Dispose();
                capture = null;
            }
            device?.Dispose();
            device = null;
            inputBuffer = null;
            normalizedProvider = null;
        }

        public void Dispose()
        {
            Stop();
            lock (pipelineLock)
                ReleaseCapture();
        }

        private sealed class DownmixToMonoSampleProvider : ISampleProvider
        {
            private readonly ISampleProvider source;
            private float[] sourceBuffer = Array.Empty<float>();

            public DownmixToMonoSampleProvider(ISampleProvider source)
            {
                this.source = source;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 1);
            }

            public WaveFormat WaveFormat { get; }

            public int Read(float[] buffer, int offset, int count)
            {
                int channels = source.WaveFormat.Channels;
                int required = count * channels;
                if (sourceBuffer.Length < required)
                    sourceBuffer = new float[required];

                int samplesRead = source.Read(sourceBuffer, 0, required);
                int framesRead = samplesRead / channels;
                for (int frame = 0; frame < framesRead; frame++)
                {
                    float sum = 0;
                    int sourceOffset = frame * channels;
                    for (int channel = 0; channel < channels; channel++)
                        sum += sourceBuffer[sourceOffset + channel];
                    buffer[offset + frame] = sum / channels;
                }
                return framesRead;
            }
        }
    }
}
