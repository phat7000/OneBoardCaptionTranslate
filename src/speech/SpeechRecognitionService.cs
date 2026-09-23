using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.services;

namespace LiveCaptionsTranslator.speech
{
    public static class SpeechRecognitionService
    {
        private static readonly SemaphoreSlim switchLock = new(1, 1);
        private static ISpeechRecognitionProvider? current;
        private static CancellationTokenSource? lifetimeCancellation;

        public static IReadOnlyDictionary<string, string> Providers { get; } =
            new Dictionary<string, string>
            {
                ["WindowsLiveCaptions"] = "Windows Live Captions",
                ["AzureSpeech"] = "Azure Speech",
                ["GoogleSpeech"] = "Google Speech"
            };

        public static string? CurrentProviderId => current?.Id;

        public static async Task StartSelectedAsync(CancellationToken cancellationToken = default) =>
            await SwitchAsync(Translator.Setting.SpeechProviderId, cancellationToken);

        public static async Task SwitchAsync(string providerId, CancellationToken cancellationToken = default)
        {
            await switchLock.WaitAsync(cancellationToken);
            try
            {
                if (current?.Id == providerId && current.IsRunning)
                    return;

                await StopCurrentCoreAsync(cancellationToken);
                Translator.Setting.SpeechStatus = $"Starting {ProviderDisplayName(providerId)}...";

                current = Create(providerId);
                current.ResultReceived += OnResultReceived;
                current.StatusChanged += OnStatusChanged;
                lifetimeCancellation = new CancellationTokenSource();
                string locale = LanguageCatalog.GetSpeechLocale(Translator.Setting.SpeechLanguage, providerId);

                SpeechConfigurationValidation validation = await current.ValidateConfigurationAsync(cancellationToken);
                if (!validation.IsValid)
                {
                    Translator.Setting.SpeechStatus = $"[ERROR] {validation.Message}";
                    await StopCurrentCoreAsync(cancellationToken);
                    return;
                }

                await current.StartAsync(locale, lifetimeCancellation.Token);
            }
            catch (Exception ex)
            {
                Translator.Setting.SpeechStatus = $"[ERROR] {ProviderDisplayName(providerId)}: {ex.Message}";
                try
                {
                    await StopCurrentCoreAsync(CancellationToken.None);
                }
                catch { }
            }
            finally
            {
                switchLock.Release();
            }
        }

        public static async Task RestartSelectedAsync(CancellationToken cancellationToken = default)
        {
            string providerId = Translator.Setting.SpeechProviderId;
            await StopAsync(cancellationToken);
            await SwitchAsync(providerId, cancellationToken);
        }

        public static async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await switchLock.WaitAsync(cancellationToken);
            try
            {
                await StopCurrentCoreAsync(cancellationToken);
                Translator.Setting.SpeechStatus = "Stopped";
            }
            finally
            {
                switchLock.Release();
            }
        }

        private static async Task StopCurrentCoreAsync(CancellationToken cancellationToken)
        {
            lifetimeCancellation?.Cancel();
            if (current != null)
            {
                current.ResultReceived -= OnResultReceived;
                current.StatusChanged -= OnStatusChanged;
                try { await current.StopAsync(cancellationToken); }
                finally { await current.DisposeAsync(); }
            }
            current = null;
            lifetimeCancellation?.Dispose();
            lifetimeCancellation = null;
        }

        private static ISpeechRecognitionProvider Create(string providerId) => providerId switch
        {
            "WindowsLiveCaptions" => new WindowsLiveCaptionsProvider(),
            "AzureSpeech" => new AzureSpeechProvider(Translator.Setting.AzureSpeech),
            "GoogleSpeech" => new GoogleSpeechProvider(Translator.Setting.GoogleSpeech),
            _ => throw new InvalidOperationException($"Unknown speech provider: {providerId}")
        };

        private static string ProviderDisplayName(string providerId) =>
            Providers.TryGetValue(providerId, out string? name) ? name : providerId;

        private static void OnResultReceived(object? sender, SpeechResult result) => Translator.AcceptSpeechResult(result);

        private static void OnStatusChanged(object? sender, SpeechProviderStatus status) =>
            Translator.Setting.SpeechStatus = status.IsError ? $"[ERROR] {status.Message}" : status.Message;
    }
}
