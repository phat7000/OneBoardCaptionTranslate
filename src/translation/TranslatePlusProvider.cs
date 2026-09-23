using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.services;

namespace LiveCaptionsTranslator.translation
{
    public sealed class TranslatePlusProvider : ITranslationProvider
    {
        private readonly HttpClient client;

        public TranslatePlusProvider(HttpClient? client = null) =>
            this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(12) };

        public string Id => "TranslatePlus";
        public string DisplayName => "TranslatePlus";
        public bool SupportsAutoDetect => true;
        public bool RequiresApiKey => true;

        public async Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Configuration is not TranslatePlusConfig config || string.IsNullOrWhiteSpace(config.ApiKey))
                throw new ProviderException(ProviderErrorKind.InvalidConfiguration, "TranslatePlus API key is required.");

            using HttpRequestMessage message = new(HttpMethod.Post, "https://api.translateplus.io/v2/translate");
            message.Headers.Add("X-API-KEY", config.ApiKey);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var payload = new Dictionary<string, object>
            {
                ["text"] = request.Text,
                ["target"] = request.TargetLanguage
            };
            if (!string.IsNullOrWhiteSpace(request.SourceLanguage))
                payload["source"] = request.SourceLanguage;
            message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
                await TranslationHttp.EnsureSuccessAsync(response, cancellationToken);
                using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                JsonElement translation = json.RootElement.GetProperty("translations");
                return new TranslationResult(
                    translation.GetProperty("translation").GetString() ?? string.Empty,
                    translation.TryGetProperty("source", out JsonElement source) ? source.GetString() : null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or
                                          KeyNotFoundException or InvalidOperationException)
            {
                throw TranslationHttp.NormalizeTransportException(ex, cancellationToken);
            }
        }

        public Task<ProviderValidationResult> ValidateConfigurationAsync(
            TranslateAPIConfig configuration,
            CancellationToken cancellationToken)
        {
            ProviderValidationResult result = configuration is TranslatePlusConfig config &&
                                              !string.IsNullOrWhiteSpace(config.ApiKey)
                ? ProviderValidationResult.Valid("API key is configured; validation occurs on the first request.")
                : ProviderValidationResult.Invalid("API key is required.");
            return Task.FromResult(result);
        }

        public Task<IReadOnlySet<string>> GetSupportedLanguagesAsync(
            TranslateAPIConfig configuration,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlySet<string>>(LanguageCatalog.All
                .Select(language => language.GetProviderCode(Id, speech: false))
                .ToHashSet(StringComparer.OrdinalIgnoreCase));
    }
}
