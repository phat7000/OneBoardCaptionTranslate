using LiveCaptionsTranslator.models;
using System.Net.Http;

namespace LiveCaptionsTranslator.translation
{
    /// <summary>Official Google Cloud Translation - Basic (v2), using API-key authentication.</summary>
    public sealed class GoogleCloudTranslationProvider : GoogleV2TranslationProviderBase
    {
        public GoogleCloudTranslationProvider(HttpClient? client = null) : base(client) { }

        public override string Id => "GoogleCloudTranslation";
        public override string DisplayName => "Google Cloud Translation";

        protected override string GetApiKey(TranslateAPIConfig configuration) =>
            (configuration as GoogleCloudTranslationConfig)?.ApiKey ?? string.Empty;

        protected override string GetBaseEndpoint(TranslateAPIConfig configuration) =>
            "https://translation.googleapis.com";

        protected override void ApplyAuthentication(HttpRequestMessage message, string apiKey)
        {
            UriBuilder uri = new(message.RequestUri!) { Query = $"key={Uri.EscapeDataString(apiKey)}" };
            message.RequestUri = uri.Uri;
        }
    }
}
