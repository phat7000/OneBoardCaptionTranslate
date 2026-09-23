using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.services;

namespace LiveCaptionsTranslator.translation
{
    internal sealed class LegacyTranslationProvider : ITranslationProvider
    {
        private readonly Func<string, CancellationToken, Task<string>> translate;

        public LegacyTranslationProvider(
            string id,
            Func<string, CancellationToken, Task<string>> translate,
            bool requiresApiKey)
        {
            Id = id;
            DisplayName = id;
            this.translate = translate;
            RequiresApiKey = requiresApiKey;
        }

        public string Id { get; }
        public string DisplayName { get; }
        public bool SupportsAutoDetect => true;
        public bool RequiresApiKey { get; }

        public async Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken) =>
            new(await translate(request.Text, cancellationToken));

        public Task<ProviderValidationResult> ValidateConfigurationAsync(
            TranslateAPIConfig configuration,
            CancellationToken cancellationToken) =>
            Task.FromResult(ProviderValidationResult.Valid("Legacy provider configuration is managed by the existing adapter."));

        public Task<IReadOnlySet<string>> GetSupportedLanguagesAsync(
            TranslateAPIConfig configuration,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(LanguageCatalog.All
                .Select(language => language.GetProviderCode(Id, speech: false))
                .ToHashSet(StringComparer.OrdinalIgnoreCase));
    }
}
