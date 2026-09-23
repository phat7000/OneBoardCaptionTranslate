using LiveCaptionsTranslator.models;
using System.Net.Http;

namespace LiveCaptionsTranslator.translation
{
    public sealed class LangblyProvider : GoogleV2TranslationProviderBase
    {
        public LangblyProvider(HttpClient? client = null) : base(client) { }

        public override string Id => "Langbly";
        public override string DisplayName => "Langbly";

        protected override string GetApiKey(TranslateAPIConfig configuration) =>
            (configuration as LangblyConfig)?.ApiKey ?? string.Empty;

        protected override string GetBaseEndpoint(TranslateAPIConfig configuration) =>
            string.IsNullOrWhiteSpace((configuration as LangblyConfig)?.Endpoint)
                ? "https://api.langbly.com"
                : ((LangblyConfig)configuration).Endpoint;

        protected override void ApplyAuthentication(HttpRequestMessage message, string apiKey) =>
            message.Headers.Add("X-API-Key", apiKey);
    }
}
