using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.translation
{
    public abstract class GoogleV2TranslationProviderBase : ITranslationProvider
    {
        private readonly HttpClient client;

        protected GoogleV2TranslationProviderBase(HttpClient? client = null) =>
            this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(12) };

        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        public bool SupportsAutoDetect => true;
        public bool RequiresApiKey => true;

        protected abstract string GetApiKey(TranslateAPIConfig configuration);
        protected abstract string GetBaseEndpoint(TranslateAPIConfig configuration);
        protected abstract void ApplyAuthentication(HttpRequestMessage message, string apiKey);

        public async Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken)
        {
            string apiKey = GetApiKey(request.Configuration);
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ProviderException(ProviderErrorKind.InvalidConfiguration, $"{DisplayName} API key is required.");

            string endpoint = GetBaseEndpoint(request.Configuration).TrimEnd('/') + "/language/translate/v2";
            using HttpRequestMessage message = new(HttpMethod.Post, endpoint);
            ApplyAuthentication(message, apiKey);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var payload = new Dictionary<string, object>
            {
                ["q"] = request.Text,
                ["target"] = request.TargetLanguage,
                ["format"] = "text"
            };
            if (!string.IsNullOrWhiteSpace(request.SourceLanguage))
                payload["source"] = request.SourceLanguage;
            message.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            try
            {
                using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
                await TranslationHttp.EnsureSuccessAsync(response, cancellationToken);
                using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                JsonElement translation = json.RootElement.GetProperty("data").GetProperty("translations")[0];
                string text = WebUtility.HtmlDecode(translation.GetProperty("translatedText").GetString() ?? string.Empty);
                string? detected = translation.TryGetProperty("detectedSourceLanguage", out JsonElement source)
                    ? source.GetString()
                    : null;
                return new TranslationResult(text, detected);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException or
                                          KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
            {
                throw TranslationHttp.NormalizeTransportException(ex, cancellationToken);
            }
        }

        public async Task<ProviderValidationResult> ValidateConfigurationAsync(
            TranslateAPIConfig configuration,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(GetApiKey(configuration)))
                return ProviderValidationResult.Invalid("API key is required.");
            try
            {
                await GetSupportedLanguagesAsync(configuration, cancellationToken);
                return ProviderValidationResult.Valid();
            }
            catch (ProviderException ex)
            {
                return ProviderValidationResult.Invalid(ex.Message);
            }
        }

        public async Task<IReadOnlySet<string>> GetSupportedLanguagesAsync(
            TranslateAPIConfig configuration,
            CancellationToken cancellationToken)
        {
            string apiKey = GetApiKey(configuration);
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ProviderException(ProviderErrorKind.InvalidConfiguration, $"{DisplayName} API key is required.");

            string endpoint = GetBaseEndpoint(configuration).TrimEnd('/') + "/language/translate/v2/languages";
            using HttpRequestMessage message = new(HttpMethod.Get, endpoint);
            ApplyAuthentication(message, apiKey);
            try
            {
                using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
                await TranslationHttp.EnsureSuccessAsync(response, cancellationToken);
                using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                return json.RootElement.GetProperty("data").GetProperty("languages").EnumerateArray()
                    .Select(item => item.GetProperty("language").GetString())
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Select(code => code!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
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
    }
}
