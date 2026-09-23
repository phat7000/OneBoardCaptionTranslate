using LiveCaptionsTranslator.apis;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.services;

namespace LiveCaptionsTranslator.translation
{
    public static class TranslationProviderRegistry
    {
        private static readonly Dictionary<string, Func<ITranslationProvider>> factories =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["MicrosoftTranslator"] = () => new MicrosoftTranslatorProvider(),
                ["GoogleCloudTranslation"] = () => new GoogleCloudTranslationProvider(),
                ["TranslatePlus"] = () => new TranslatePlusProvider(),
                ["Langbly"] = () => new LangblyProvider()
            };

        private static readonly Dictionary<string, string> displayNames =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["MicrosoftTranslator"] = "Microsoft Translator",
                ["GoogleCloudTranslation"] = "Google Cloud Translation",
                ["TranslatePlus"] = "TranslatePlus",
                ["Langbly"] = "Langbly"
            };

        private static readonly object providerLock = new();
        private static readonly Dictionary<string, ITranslationProvider> providers =
            new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<string> ProviderIds => TranslateAPI.TRANSLATE_FUNCTIONS.Keys.ToList();
        public static string GetDisplayName(string providerId) =>
            displayNames.TryGetValue(providerId, out string? name) ? name : providerId;

        public static ITranslationProvider Get(string providerId)
        {
            lock (providerLock)
            {
                if (providers.TryGetValue(providerId, out ITranslationProvider? provider))
                    return provider;

                if (factories.TryGetValue(providerId, out Func<ITranslationProvider>? factory))
                    provider = factory();
                else if (TranslateAPI.TRANSLATE_FUNCTIONS.TryGetValue(
                             providerId,
                             out Func<string, CancellationToken, Task<string>>? legacy))
                    provider = new LegacyTranslationProvider(
                        providerId,
                        legacy,
                        !TranslateAPI.NO_CONFIG_APIS.Contains(providerId));
                else
                    throw new ProviderException(
                        ProviderErrorKind.InvalidConfiguration,
                        $"Unknown translation provider: {providerId}");

                providers[providerId] = provider;
                return provider;
            }
        }

        public static async Task<string> TranslateAsync(string text, CancellationToken cancellationToken)
        {
            Setting settings = Translator.Setting;
            ITranslationProvider provider = Get(settings.ApiName);
            string targetCode = LanguageCatalog.GetTranslationCode(settings.TargetLanguage, provider.Id);
            string sourceCode = LanguageCatalog.GetTranslationCode(settings.SpeechLanguage, provider.Id);
            TranslationResult result = await provider.TranslateAsync(
                new TranslationRequest(text, sourceCode, targetCode, settings[settings.ApiName]),
                cancellationToken);
            return result.Text;
        }
    }
}
