using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.services;

namespace LiveCaptionsTranslator.speech
{
    public static class SpeechRecognitionService
    {
        private static readonly SpeechProviderSession session = new(Create, ProviderDisplayName);

        public static IReadOnlyDictionary<string, string> Providers { get; } =
            new Dictionary<string, string>
            {
                ["WindowsLiveCaptions"] = "Windows Live Captions",
                ["AzureSpeech"] = "Azure Speech",
                ["GoogleSpeech"] = "Google Speech"
            };

        static SpeechRecognitionService()
        {
            session.ResultReceived += OnResultReceived;
            session.StatusChanged += OnStatusChanged;
        }

        public static string? CurrentProviderId => session.CurrentProviderId;

        public static async Task StartSelectedAsync(CancellationToken cancellationToken = default) =>
            await SwitchAsync(Translator.Setting.SpeechProviderId, cancellationToken);

        public static async Task SwitchAsync(string providerId, CancellationToken cancellationToken = default)
        {
            string locale = LanguageCatalog.GetSpeechLocale(Translator.Setting.SpeechLanguage, providerId);
            await session.SwitchAsync(providerId, locale, cancellationToken: cancellationToken);
        }

        public static async Task RestartSelectedAsync(CancellationToken cancellationToken = default)
        {
            string providerId = Translator.Setting.SpeechProviderId;
            string locale = LanguageCatalog.GetSpeechLocale(Translator.Setting.SpeechLanguage, providerId);
            await session.SwitchAsync(providerId, locale, forceRestart: true, cancellationToken: cancellationToken);
        }

        public static async Task StopAsync(CancellationToken cancellationToken = default) =>
            await session.StopAsync(cancellationToken);

        private static ISpeechRecognitionProvider Create(string providerId) => providerId switch
        {
            "WindowsLiveCaptions" => new WindowsLiveCaptionsProvider(),
            "AzureSpeech" => new AzureSpeechProvider(
                Translator.Setting.AzureSpeech,
                Translator.Setting.AudioSource,
                Translator.Setting.ExternalAudioDeviceId,
                Translator.Setting.ExternalAudioDeviceDisplayName),
            "GoogleSpeech" => new GoogleSpeechProvider(
                Translator.Setting.GoogleSpeech,
                Translator.Setting.AudioSource,
                Translator.Setting.ExternalAudioDeviceId,
                Translator.Setting.ExternalAudioDeviceDisplayName),
            _ => throw new InvalidOperationException($"Unknown speech provider: {providerId}")
        };

        private static string ProviderDisplayName(string providerId) =>
            Providers.TryGetValue(providerId, out string? name) ? name : providerId;

        private static void OnResultReceived(object? sender, SpeechResult result) => Translator.AcceptSpeechResult(result);

        private static void OnStatusChanged(object? sender, SpeechProviderStatus status) =>
            Translator.Setting.SpeechStatus = status.IsError ? $"[ERROR] {status.Message}" : status.Message;
    }
}
