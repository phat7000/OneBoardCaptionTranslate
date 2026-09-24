using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace LiveCaptionsTranslator.speech
{
    /// <summary>Converts a native PCM/IEEE-float stream to 16 kHz, 16-bit mono PCM.</summary>
    public sealed class Pcm16MonoNormalizer
    {
        public const int TargetSampleRate = 16000;
        public const short TargetBitsPerSample = 16;
        public const short TargetChannels = 1;
        private const int OutputBufferSize = 6400;

        private readonly WaveFormat sourceFormat;
        private readonly BufferedWaveProvider inputBuffer;
        private readonly IWaveProvider normalizedProvider;
        private readonly byte[] outputBuffer = new byte[OutputBufferSize];

        public Pcm16MonoNormalizer(WaveFormat sourceFormat)
        {
            if (sourceFormat.Channels < 1)
                throw new NotSupportedException($"Unsupported audio format: {sourceFormat}.");

            this.sourceFormat = sourceFormat;
            inputBuffer = new BufferedWaveProvider(sourceFormat)
            {
                BufferDuration = TimeSpan.FromSeconds(2),
                DiscardOnBufferOverflow = true,
                ReadFully = false
            };

            ISampleProvider samples = inputBuffer.ToSampleProvider();
            if (samples.WaveFormat.Channels > 1)
                samples = new DownmixToMonoSampleProvider(samples);
            if (samples.WaveFormat.SampleRate != TargetSampleRate)
                samples = new WdlResamplingSampleProvider(samples, TargetSampleRate);
            normalizedProvider = new SampleToWaveProvider16(samples);
        }

        public IReadOnlyList<byte[]> Convert(byte[] source, int bytesRecorded)
        {
            if (bytesRecorded <= 0)
                return Array.Empty<byte[]>();
            if (bytesRecorded > source.Length)
                throw new ArgumentOutOfRangeException(nameof(bytesRecorded));

            inputBuffer.AddSamples(source, 0, bytesRecorded);
            double seconds = (double)bytesRecorded / sourceFormat.AverageBytesPerSecond;
            int requested = Math.Max(2,
                (int)Math.Ceiling(seconds * TargetSampleRate * (TargetBitsPerSample / 8.0)));
            requested -= requested % 2;

            List<byte[]> chunks = [];
            while (requested > 0)
            {
                int count = Math.Min(requested, outputBuffer.Length);
                int read = normalizedProvider.Read(outputBuffer, 0, count);
                if (read <= 0)
                    break;

                byte[] chunk = new byte[read];
                Buffer.BlockCopy(outputBuffer, 0, chunk, 0, read);
                chunks.Add(chunk);
                requested -= read;
            }
            return chunks;
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
                    buffer[offset + frame] = Math.Clamp(sum / channels, -1f, 1f);
                }
                return framesRead;
            }
        }
    }
}
