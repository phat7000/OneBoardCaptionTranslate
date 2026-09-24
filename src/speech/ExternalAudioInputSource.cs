using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace LiveCaptionsTranslator.speech
{
    /// <summary>Captures a selected Windows recording endpoint and emits normalized speech PCM.</summary>
    public sealed class ExternalAudioInputSource : IAudioCaptureSource
    {
        private readonly object pipelineLock = new();
        private readonly string? deviceId;
        private readonly string savedDisplayName;
        private readonly IExternalAudioDeviceService deviceService;
        private MMDevice? device;
        private WasapiCapture? capture;
        private Pcm16MonoNormalizer? normalizer;
        private bool running;

        public ExternalAudioInputSource(
            string? deviceId,
            string? displayName,
            IExternalAudioDeviceService? deviceService = null)
        {
            this.deviceId = deviceId;
            savedDisplayName = string.IsNullOrWhiteSpace(displayName) ? "External Audio Input" : displayName;
            this.deviceService = deviceService ?? new ExternalAudioDeviceService();
        }

        public string DisplayName => device?.FriendlyName ?? savedDisplayName;
        public int SampleRate => Pcm16MonoNormalizer.TargetSampleRate;
        public short BitsPerSample => Pcm16MonoNormalizer.TargetBitsPerSample;
        public short Channels => Pcm16MonoNormalizer.TargetChannels;
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
                    device = deviceService.OpenActiveCaptureDevice(deviceId ?? string.Empty);
                    capture = new WasapiCapture(device);
                    normalizer = new Pcm16MonoNormalizer(capture.WaveFormat);
                    capture.DataAvailable += OnDataAvailable;
                    capture.RecordingStopped += OnRecordingStopped;
                    running = true;
                    capture.StartRecording();
                }
                catch (Exception ex)
                {
                    running = false;
                    ReleaseCapture();
                    if (ex is InvalidOperationException &&
                        (ex.Message.Contains("Select an external", StringComparison.Ordinal) ||
                         ex.Message.Contains("unavailable", StringComparison.OrdinalIgnoreCase)))
                        throw;
                    throw new InvalidOperationException(
                        $"External audio input could not start on {savedDisplayName}: {ex.Message}", ex);
                }
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            lock (pipelineLock)
            {
                if (!running || normalizer == null)
                    return;
                try
                {
                    foreach (byte[] chunk in normalizer.Convert(e.Buffer, e.BytesRecorded))
                        AudioAvailable?.Invoke(this, new AudioChunkEventArgs(chunk));
                }
                catch (Exception ex)
                {
                    CaptureFailed?.Invoke(this, new AudioCaptureErrorEventArgs(
                        $"External audio conversion failed for {DisplayName}: {ex.Message}", ex));
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
                string detail = e.Exception?.Message ?? "The endpoint was disconnected or powered off.";
                CaptureFailed?.Invoke(this, new AudioCaptureErrorEventArgs(
                    $"External audio device {savedDisplayName} is unavailable: {detail}", e.Exception));
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
            normalizer = null;
        }

        public void Dispose()
        {
            Stop();
            lock (pipelineLock)
                ReleaseCapture();
        }
    }
}
