using System.IO;
using System.Threading.Channels;

using Google.Cloud.Speech.V1;
using Google.Protobuf;
using Google.Apis.Auth.OAuth2;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.speech
{
    public sealed class GoogleSpeechProvider : ISpeechRecognitionProvider
    {
        private readonly GoogleSpeechConfig configuration;
        private IAudioCaptureSource? audioSource;
        private SpeechClient? client;
        private SpeechClient.StreamingRecognizeStream? stream;
        private Channel<byte[]>? audioChannel;
        private CancellationTokenSource? sessionCancellation;
        private Task? writerTask;
        private Task? readerTask;
        private string languageCode = string.Empty;

        public GoogleSpeechProvider(GoogleSpeechConfig configuration) => this.configuration = configuration;

        public string Id => "GoogleSpeech";
        public string DisplayName => "Google Speech";
        public bool IsRunning { get; private set; }

        public event EventHandler<SpeechResult>? ResultReceived;
        public event EventHandler<SpeechProviderStatus>? StatusChanged;

        public Task<SpeechConfigurationValidation> ValidateConfigurationAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(configuration.CredentialsFilePath))
                return Task.FromResult(SpeechConfigurationValidation.Invalid("Google credentials file path is required."));
            if (!File.Exists(configuration.CredentialsFilePath))
                return Task.FromResult(SpeechConfigurationValidation.Invalid("Google credentials file was not found."));
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
            ServiceAccountCredential specificCredential = await CredentialFactory
                .FromFileAsync<ServiceAccountCredential>(configuration.CredentialsFilePath, cancellationToken);
            GoogleCredential credential = specificCredential.ToGoogleCredential();
            client = new SpeechClientBuilder { GoogleCredential = credential }.Build();
            stream = client.StreamingRecognize();
            await stream.WriteAsync(new StreamingRecognizeRequest
            {
                StreamingConfig = new StreamingRecognitionConfig
                {
                    InterimResults = true,
                    SingleUtterance = false,
                    Config = new RecognitionConfig
                    {
                        Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                        SampleRateHertz = 16000,
                        AudioChannelCount = 1,
                        LanguageCode = languageCode,
                        EnableAutomaticPunctuation = true
                    }
                }
            });

            audioChannel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(32)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
            sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            audioSource = new WaveInAudioCaptureSource();
            audioSource.AudioAvailable += OnAudioAvailable;
            writerTask = WriteAudioAsync(sessionCancellation.Token);
            readerTask = ReadResultsAsync(sessionCancellation.Token);
            audioSource.Start();
            IsRunning = true;
            StatusChanged?.Invoke(this, new SpeechProviderStatus(false, "Listening with Google Speech."));
        }

        private void OnAudioAvailable(object? sender, AudioChunkEventArgs e) => audioChannel?.Writer.TryWrite(e.Data);

        private async Task WriteAudioAsync(CancellationToken cancellationToken)
        {
            if (audioChannel == null || stream == null)
                return;
            await foreach (byte[] bytes in audioChannel.Reader.ReadAllAsync(cancellationToken))
                await stream.WriteAsync(new StreamingRecognizeRequest { AudioContent = ByteString.CopyFrom(bytes) });
        }

        private async Task ReadResultsAsync(CancellationToken cancellationToken)
        {
            if (stream == null)
                return;
            try
            {
                var responses = stream.GetResponseStream();
                while (await responses.MoveNextAsync(cancellationToken))
                {
                    StreamingRecognizeResponse response = responses.Current;
                    foreach (StreamingRecognitionResult result in response.Results)
                    {
                        string text = result.Alternatives.FirstOrDefault()?.Transcript ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(text))
                            ResultReceived?.Invoke(this, new SpeechResult(
                                text,
                                languageCode,
                                result.IsFinal,
                                DateTimeOffset.UtcNow));
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, new SpeechProviderStatus(true, $"Google Speech stopped: {ex.Message}"));
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            IsRunning = false;
            if (audioSource != null)
            {
                audioSource.AudioAvailable -= OnAudioAvailable;
                audioSource.Stop();
                audioSource.Dispose();
                audioSource = null;
            }

            audioChannel?.Writer.TryComplete();
            if (writerTask != null)
            {
                try { await writerTask.WaitAsync(cancellationToken); }
                catch (OperationCanceledException) { }
            }
            if (stream != null)
            {
                try { await stream.WriteCompleteAsync(); }
                catch { }
            }

            sessionCancellation?.Cancel();
            if (readerTask != null)
            {
                try { await readerTask.WaitAsync(cancellationToken); }
                catch (OperationCanceledException) { }
            }

            sessionCancellation?.Dispose();
            sessionCancellation = null;
            audioChannel = null;
            writerTask = null;
            readerTask = null;
            stream?.Dispose();
            stream = null;
            client = null;
        }

        public async ValueTask DisposeAsync()
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(3));
            await StopAsync(timeout.Token);
        }
    }
}
