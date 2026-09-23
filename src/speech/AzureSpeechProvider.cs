using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.speech
{
    public sealed class AzureSpeechProvider : ISpeechRecognitionProvider
    {
        private readonly AzureSpeechConfig configuration;
        private IAudioCaptureSource? audioSource;
        private PushAudioInputStream? pushStream;
        private AudioConfig? audioConfig;
        private SpeechRecognizer? recognizer;
        private string languageCode = string.Empty;

        public AzureSpeechProvider(AzureSpeechConfig configuration) => this.configuration = configuration;

        public string Id => "AzureSpeech";
        public string DisplayName => "Azure Speech";
        public bool IsRunning { get; private set; }

        public event EventHandler<SpeechResult>? ResultReceived;
        public event EventHandler<SpeechProviderStatus>? StatusChanged;

        public Task<SpeechConfigurationValidation> ValidateConfigurationAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(configuration.ApiKey))
                return Task.FromResult(SpeechConfigurationValidation.Invalid("Azure Speech key is required."));
            if (string.IsNullOrWhiteSpace(configuration.Region))
                return Task.FromResult(SpeechConfigurationValidation.Invalid("Azure Speech region is required."));
            return Task.FromResult(SpeechConfigurationValidation.Valid());
        }

        public async Task StartAsync(string languageCode, CancellationToken cancellationToken)
        {
            if (IsRunning)
                return;
            SpeechConfigurationValidation validation = await ValidateConfigurationAsync(cancellationToken);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.Message);

            this.languageCode = languageCode;
            SpeechConfig speechConfig = SpeechConfig.FromSubscription(configuration.ApiKey, configuration.Region);
            speechConfig.SpeechRecognitionLanguage = languageCode;

            audioSource = new WaveInAudioCaptureSource();
            AudioStreamFormat format = AudioStreamFormat.GetWaveFormatPCM(
                (uint)audioSource.SampleRate,
                (byte)audioSource.BitsPerSample,
                (byte)audioSource.Channels);
            pushStream = AudioInputStream.CreatePushStream(format);
            audioConfig = AudioConfig.FromStreamInput(pushStream);
            recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            recognizer.Recognizing += OnRecognizing;
            recognizer.Recognized += OnRecognized;
            recognizer.Canceled += OnCanceled;
            recognizer.SessionStopped += OnSessionStopped;
            audioSource.AudioAvailable += OnAudioAvailable;

            await recognizer.StartContinuousRecognitionAsync().WaitAsync(cancellationToken);
            audioSource.Start();
            IsRunning = true;
            StatusChanged?.Invoke(this, new SpeechProviderStatus(false, "Listening with Azure Speech."));
        }

        private void OnAudioAvailable(object? sender, AudioChunkEventArgs e) => pushStream?.Write(e.Data);

        private void OnRecognizing(object? sender, SpeechRecognitionEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Result.Text))
                ResultReceived?.Invoke(this, new SpeechResult(
                    e.Result.Text,
                    languageCode,
                    false,
                    DateTimeOffset.UtcNow));
        }

        private void OnRecognized(object? sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech && !string.IsNullOrWhiteSpace(e.Result.Text))
                ResultReceived?.Invoke(this, new SpeechResult(
                    e.Result.Text,
                    languageCode,
                    true,
                    DateTimeOffset.UtcNow));
        }

        private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e) =>
            StatusChanged?.Invoke(this, new SpeechProviderStatus(
                true,
                $"Azure Speech stopped: {e.Reason}{(string.IsNullOrWhiteSpace(e.ErrorDetails) ? string.Empty : $". {e.ErrorDetails}")}"));

        private void OnSessionStopped(object? sender, SessionEventArgs e)
        {
            if (IsRunning)
                StatusChanged?.Invoke(this, new SpeechProviderStatus(false, "Azure Speech session stopped."));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (recognizer != null && IsRunning)
                await recognizer.StopContinuousRecognitionAsync().WaitAsync(cancellationToken);

            IsRunning = false;
            if (audioSource != null)
            {
                audioSource.AudioAvailable -= OnAudioAvailable;
                audioSource.Stop();
                audioSource.Dispose();
            }
            if (recognizer != null)
            {
                recognizer.Recognizing -= OnRecognizing;
                recognizer.Recognized -= OnRecognized;
                recognizer.Canceled -= OnCanceled;
                recognizer.SessionStopped -= OnSessionStopped;
                recognizer.Dispose();
            }
            pushStream?.Close();
            pushStream?.Dispose();
            audioConfig?.Dispose();
            recognizer = null;
            audioSource = null;
            pushStream = null;
            audioConfig = null;
        }

        public async ValueTask DisposeAsync()
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(3));
            await StopAsync(timeout.Token);
        }
    }
}
