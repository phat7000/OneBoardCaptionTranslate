using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.speech
{
    public sealed class AzureSpeechProvider : ISpeechRecognitionProvider
    {
        private readonly AzureSpeechConfig configuration;
        private readonly AudioSourceType audioSourceType;
        private readonly Func<AudioSourceType, IAudioCaptureSource> audioSourceFactory;
        private IAudioCaptureSource? audioSource;
        private PushAudioInputStream? pushStream;
        private AudioConfig? audioConfig;
        private SpeechRecognizer? recognizer;
        private string languageCode = string.Empty;
        private bool recognizerStarted;
        private int failureHandling;

        public AzureSpeechProvider(AzureSpeechConfig configuration)
            : this(configuration, AudioSourceType.Microphone)
        {
        }

        public AzureSpeechProvider(
            AzureSpeechConfig configuration,
            AudioSourceType audioSourceType,
            Func<AudioSourceType, IAudioCaptureSource>? audioSourceFactory = null)
        {
            this.configuration = configuration;
            this.audioSourceType = audioSourceType;
            this.audioSourceFactory = audioSourceFactory ?? AudioCaptureSourceFactory.Create;
        }

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

            audioSource = audioSourceFactory(audioSourceType);
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
            recognizer.SessionStarted += OnSessionStarted;
            recognizer.SessionStopped += OnSessionStopped;
            audioSource.AudioAvailable += OnAudioAvailable;
            audioSource.CaptureFailed += OnAudioCaptureFailed;

            IsRunning = true;
            recognizerStarted = true;
            await recognizer.StartContinuousRecognitionAsync().WaitAsync(cancellationToken);
            audioSource.Start();
            StatusChanged?.Invoke(this, new SpeechProviderStatus(
                false, $"Listening to {audioSource.DisplayName} with Azure Speech."));
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

        private void OnCanceled(object? sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (!IsRunning)
                return;
            StatusChanged?.Invoke(this, new SpeechProviderStatus(
                true,
                $"Azure Speech stopped: {e.Reason}{(string.IsNullOrWhiteSpace(e.ErrorDetails) ? string.Empty : $". {e.ErrorDetails}")}"));
            QueueFailureCleanup();
        }

        private void OnSessionStarted(object? sender, SessionEventArgs e)
        {
            // The user-facing listening status is emitted only after capture also starts.
        }

        private void OnSessionStopped(object? sender, SessionEventArgs e)
        {
            if (IsRunning)
            {
                StatusChanged?.Invoke(this, new SpeechProviderStatus(false, "Azure Speech session stopped."));
                QueueFailureCleanup();
            }
        }

        private void OnAudioCaptureFailed(object? sender, AudioCaptureErrorEventArgs e)
        {
            IsRunning = false;
            StatusChanged?.Invoke(this, new SpeechProviderStatus(true, e.Message));
            QueueFailureCleanup();
        }

        private void QueueFailureCleanup()
        {
            IsRunning = false;
            if (Interlocked.Exchange(ref failureHandling, 1) == 0)
                _ = Task.Run(StopAfterCaptureFailureAsync);
        }

        private async Task StopAfterCaptureFailureAsync()
        {
            try
            {
                await StopAsync(CancellationToken.None);
            }
            catch
            {
                // The original capture failure is already visible to the user.
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            IsRunning = false;
            Exception? stopError = null;
            if (recognizer != null && recognizerStarted)
            {
                try
                {
                    await recognizer.StopContinuousRecognitionAsync().WaitAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    stopError = ex;
                }
                finally
                {
                    recognizerStarted = false;
                }
            }

            if (audioSource != null)
            {
                audioSource.AudioAvailable -= OnAudioAvailable;
                audioSource.CaptureFailed -= OnAudioCaptureFailed;
                try { audioSource.Stop(); }
                finally { audioSource.Dispose(); }
            }
            if (recognizer != null)
            {
                recognizer.Recognizing -= OnRecognizing;
                recognizer.Recognized -= OnRecognized;
                recognizer.Canceled -= OnCanceled;
                recognizer.SessionStarted -= OnSessionStarted;
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
            Interlocked.Exchange(ref failureHandling, 0);

            if (stopError != null)
                throw stopError;
        }

        public async ValueTask DisposeAsync()
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(3));
            await StopAsync(timeout.Token);
        }
    }
}
