namespace LiveCaptionsTranslator.speech
{
    /// <summary>
    /// Serializes provider replacement so only one recognizer and capture source can be active.
    /// </summary>
    public sealed class SpeechProviderSession
    {
        private readonly SemaphoreSlim switchLock = new(1, 1);
        private readonly Func<string, ISpeechRecognitionProvider> providerFactory;
        private readonly Func<string, string> providerDisplayName;
        private ISpeechRecognitionProvider? current;
        private CancellationTokenSource? lifetimeCancellation;

        public SpeechProviderSession(
            Func<string, ISpeechRecognitionProvider> providerFactory,
            Func<string, string> providerDisplayName)
        {
            this.providerFactory = providerFactory;
            this.providerDisplayName = providerDisplayName;
        }

        public string? CurrentProviderId => current?.Id;
        public event EventHandler<SpeechResult>? ResultReceived;
        public event EventHandler<SpeechProviderStatus>? StatusChanged;

        public async Task SwitchAsync(
            string providerId,
            string languageCode,
            bool forceRestart = false,
            CancellationToken cancellationToken = default)
        {
            await switchLock.WaitAsync(cancellationToken);
            try
            {
                if (!forceRestart && current?.Id == providerId && current.IsRunning)
                    return;

                await StopCurrentCoreAsync(cancellationToken);
                StatusChanged?.Invoke(this, new SpeechProviderStatus(
                    false, $"Starting {providerDisplayName(providerId)}..."));

                current = providerFactory(providerId);
                current.ResultReceived += OnResultReceived;
                current.StatusChanged += OnStatusChanged;
                lifetimeCancellation = new CancellationTokenSource();

                SpeechConfigurationValidation validation =
                    await current.ValidateConfigurationAsync(cancellationToken);
                if (!validation.IsValid)
                {
                    StatusChanged?.Invoke(this, new SpeechProviderStatus(true, validation.Message));
                    await StopCurrentCoreAsync(cancellationToken);
                    return;
                }

                await current.StartAsync(languageCode, lifetimeCancellation.Token);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke(this, new SpeechProviderStatus(
                    true, $"{providerDisplayName(providerId)}: {ex.Message}"));
                try
                {
                    await StopCurrentCoreAsync(CancellationToken.None);
                }
                catch
                {
                    // Preserve the original startup/switching error.
                }
            }
            finally
            {
                switchLock.Release();
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await switchLock.WaitAsync(cancellationToken);
            try
            {
                await StopCurrentCoreAsync(cancellationToken);
                StatusChanged?.Invoke(this, new SpeechProviderStatus(false, "Stopped"));
            }
            finally
            {
                switchLock.Release();
            }
        }

        private async Task StopCurrentCoreAsync(CancellationToken cancellationToken)
        {
            lifetimeCancellation?.Cancel();
            if (current != null)
            {
                current.ResultReceived -= OnResultReceived;
                current.StatusChanged -= OnStatusChanged;
                try
                {
                    await current.StopAsync(cancellationToken);
                }
                finally
                {
                    await current.DisposeAsync();
                }
            }
            current = null;
            lifetimeCancellation?.Dispose();
            lifetimeCancellation = null;
        }

        private void OnResultReceived(object? sender, SpeechResult result) =>
            ResultReceived?.Invoke(this, result);

        private void OnStatusChanged(object? sender, SpeechProviderStatus status) =>
            StatusChanged?.Invoke(this, status);
    }
}
