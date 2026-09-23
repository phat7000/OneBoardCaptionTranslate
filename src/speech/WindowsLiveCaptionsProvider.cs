using System.Windows.Automation;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator.speech
{
    public sealed class WindowsLiveCaptionsProvider : ISpeechRecognitionProvider
    {
        private CancellationTokenSource? sessionCancellation;
        private Task? pollingTask;

        public string Id => "WindowsLiveCaptions";
        public string DisplayName => "Windows Live Captions";
        public bool IsRunning => pollingTask is { IsCompleted: false };

        public event EventHandler<SpeechResult>? ResultReceived;
        public event EventHandler<SpeechProviderStatus>? StatusChanged;

        public Task<SpeechConfigurationValidation> ValidateConfigurationAsync(CancellationToken cancellationToken) =>
            Task.FromResult(SpeechConfigurationValidation.Valid("Windows Live Captions is managed by Windows."));

        public Task StartAsync(string languageCode, CancellationToken cancellationToken)
        {
            if (IsRunning)
                return Task.CompletedTask;

            AutomationElement? window = LiveCaptionsHandler.LaunchLiveCaptions();
            if (window == null)
                throw new InvalidOperationException("Windows Live Captions could not be started.");

            Translator.Window = window;
            LiveCaptionsHandler.FixLiveCaptions(window);
            LiveCaptionsHandler.HideLiveCaptions(window);

            sessionCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            pollingTask = Task.Run(() => PollAsync(window, languageCode, sessionCancellation.Token));
            StatusChanged?.Invoke(this, new SpeechProviderStatus(false, "Listening with Windows Live Captions."));
            return Task.CompletedTask;
        }

        private async Task PollAsync(
            AutomationElement initialWindow,
            string languageCode,
            CancellationToken cancellationToken)
        {
            AutomationElement? window = initialWindow;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (window == null)
                {
                    await Task.Delay(750, cancellationToken);
                    window = LiveCaptionsHandler.LaunchLiveCaptions();
                    Translator.Window = window;
                    continue;
                }

                try
                {
                    _ = window.Current.Name;
                    string text = LiveCaptionsHandler.GetCaptions(window);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        ResultReceived?.Invoke(this, new SpeechResult(
                            text,
                            languageCode,
                            false,
                            DateTimeOffset.UtcNow,
                            IsFullSnapshot: true));
                    }
                }
                catch (ElementNotAvailableException)
                {
                    window = null;
                    Translator.Window = null;
                    StatusChanged?.Invoke(this, new SpeechProviderStatus(true, "Windows Live Captions closed; restarting."));
                }

                await Task.Delay(25, cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            sessionCancellation?.Cancel();
            if (pollingTask != null)
            {
                try { await pollingTask.WaitAsync(cancellationToken); }
                catch (OperationCanceledException) { }
            }

            if (Translator.Window != null)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                LiveCaptionsHandler.KillLiveCaptions(Translator.Window);
                Translator.Window = null;
            }

            sessionCancellation?.Dispose();
            sessionCancellation = null;
            pollingTask = null;
        }

        public async ValueTask DisposeAsync()
        {
            using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(2));
            await StopAsync(timeout.Token);
        }
    }
}
