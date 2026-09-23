using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.translation
{
    public sealed class MicrosoftTranslatorProvider : ITranslationProvider
    {
        private readonly HttpClient client;

        public MicrosoftTranslatorProvider(HttpClient? client = null) =>
            this.client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public string Id => "MicrosoftTranslator";
        public string DisplayName => "Microsoft Translator";
        public bool SupportsAutoDetect => true;
        public bool RequiresApiKey => true;

        public async Task<TranslationResult> TranslateAsync(
            TranslationRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Configuration is not MicrosoftTranslatorConfig config || string.IsNullOrWhiteSpace(config.ApiKey))
                throw new ProviderException(ProviderErrorKind.InvalidConfiguration, "Microsoft Translator API key is required.");

            string endpoint = NormalizeEndpoint(config.Endpoint, "https://api.cognitive.microsofttranslator.com");
            string query = $"/translate?api-version=3.0&to={Uri.EscapeDataString(request.TargetLanguage)}";
            if (!string.IsNullOrWhiteSpace(request.SourceLanguage))
                query += $"&from={Uri.EscapeDataString(request.SourceLanguage)}";

            using HttpRequestMessage message = new(HttpMethod.Post, endpoint + query);
            message.Headers.Add("Ocp-Apim-Subscription-Key", config.ApiKey);
            if (!string.IsNullOrWhiteSpace(config.Region))
                message.Headers.Add("Ocp-Apim-Subscription-Region", config.Region);
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            message.Content = new StringContent(
                JsonSerializer.Serialize(new[] { new { Text = request.Text } }),
                Encoding.UTF8,
                "application/json");

            try
            {
                using HttpResponseMessage response = await client.SendAsync(message, cancellationToken);
                await TranslationHttp.EnsureSuccessAsync(response, cancellationToken);
                using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                JsonElement first = json.RootElement[0];
                string translatedText = first.GetProperty("translations")[0].GetProperty("text").GetString() ?? string.Empty;
                string? detected = first.TryGetProperty("detectedLanguage", out JsonElement detectedLanguage)
                    ? detectedLanguage.GetProperty("language").GetString()
                    : null;
                return new TranslationResult(translatedText, detected);
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
            if (configuration is not MicrosoftTranslatorConfig config || string.IsNullOrWhiteSpace(config.ApiKey))
                return ProviderValidationResult.Invalid("API key is required.");
            if (!Uri.TryCreate(config.Endpoint, UriKind.Absolute, out _))
                return ProviderValidationResult.Invalid("Endpoint must be an absolute HTTPS URL.");

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
            MicrosoftTranslatorConfig config = configuration as MicrosoftTranslatorConfig ?? new MicrosoftTranslatorConfig();
            string endpoint = NormalizeEndpoint(config.Endpoint, "https://api.cognitive.microsofttranslator.com");
            try
            {
                using HttpResponseMessage response = await client.GetAsync(
                    endpoint + "/languages?api-version=3.0&scope=translation",
                    cancellationToken);
                await TranslationHttp.EnsureSuccessAsync(response, cancellationToken);
                using JsonDocument json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                return json.RootElement.GetProperty("translation").EnumerateObject()
                    .Select(item => item.Name)
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

        private static string NormalizeEndpoint(string? configured, string fallback) =>
            (string.IsNullOrWhiteSpace(configured) ? fallback : configured).TrimEnd('/');
    }
}
