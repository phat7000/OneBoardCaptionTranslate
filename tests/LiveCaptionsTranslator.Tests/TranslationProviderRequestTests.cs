using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.translation;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator.Tests;

public class TranslationProviderRequestTests
{
    [Fact]
    public async Task MicrosoftTranslator_UsesV3HeadersAndBody()
    {
        RecordingHandler handler = new("""
            [{"detectedLanguage":{"language":"en","score":1},"translations":[{"text":"xin chào","to":"vi"}]}]
            """);
        MicrosoftTranslatorProvider provider = new(new HttpClient(handler));
        MicrosoftTranslatorConfig config = Deserialize<MicrosoftTranslatorConfig>(
            """{"ApiKey":"test-key","Region":"eastus","Endpoint":"https://translator.example"}""");

        TranslationResult result = await provider.TranslateAsync(
            new TranslationRequest("hello", "en", "vi", config), CancellationToken.None);

        Assert.Equal("xin chào", result.Text);
        Assert.Contains("api-version=3.0", handler.Uri!.Query);
        Assert.Contains("from=en", handler.Uri.Query);
        Assert.Contains("to=vi", handler.Uri.Query);
        Assert.Equal("test-key", handler.Headers["Ocp-Apim-Subscription-Key"]);
        Assert.Equal("eastus", handler.Headers["Ocp-Apim-Subscription-Region"]);
        Assert.Contains("\"Text\":\"hello\"", handler.Body);
    }

    [Fact]
    public async Task GoogleCloudTranslation_UsesOfficialBasicV2Shape()
    {
        RecordingHandler handler = new("""
            {"data":{"translations":[{"translatedText":"bonjour","detectedSourceLanguage":"en"}]}}
            """);
        GoogleCloudTranslationProvider provider = new(new HttpClient(handler));
        GoogleCloudTranslationConfig config = Deserialize<GoogleCloudTranslationConfig>(
            """{"ApiKey":"google-key"}""");

        TranslationResult result = await provider.TranslateAsync(
            new TranslationRequest("hello", "en", "fr", config), CancellationToken.None);

        Assert.Equal("bonjour", result.Text);
        Assert.Equal("/language/translate/v2", handler.Uri!.AbsolutePath);
        Assert.Contains("key=google-key", handler.Uri.Query);
        Assert.Contains("\"q\":\"hello\"", handler.Body);
        Assert.Contains("\"format\":\"text\"", handler.Body);
    }

    [Fact]
    public async Task TranslatePlus_UsesApiKeyHeaderAndDocumentedFields()
    {
        RecordingHandler handler = new("""
            {"translations":{"text":"hello","translation":"hola","source":"en","target":"es"},"details":{}}
            """);
        TranslatePlusProvider provider = new(new HttpClient(handler));
        TranslatePlusConfig config = Deserialize<TranslatePlusConfig>("""{"ApiKey":"plus-key"}""");

        TranslationResult result = await provider.TranslateAsync(
            new TranslationRequest("hello", "en", "es", config), CancellationToken.None);

        Assert.Equal("hola", result.Text);
        Assert.Equal("plus-key", handler.Headers["X-API-KEY"]);
        Assert.Equal("/v2/translate", handler.Uri!.AbsolutePath);
        Assert.Contains("\"text\":\"hello\"", handler.Body);
        Assert.DoesNotContain("model", handler.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Langbly_UsesGoogleV2ResponseAndPreferredHeaderAuth()
    {
        RecordingHandler handler = new("""
            {"data":{"translations":[{"translatedText":"hallo","detectedSourceLanguage":"en"}]}}
            """);
        LangblyProvider provider = new(new HttpClient(handler));
        LangblyConfig config = Deserialize<LangblyConfig>(
            """{"ApiKey":"langbly-key","Endpoint":"https://api.langbly.com"}""");

        TranslationResult result = await provider.TranslateAsync(
            new TranslationRequest("hello", "en", "nl", config), CancellationToken.None);

        Assert.Equal("hallo", result.Text);
        Assert.Equal("langbly-key", handler.Headers["X-API-Key"]);
        Assert.Equal("/language/translate/v2", handler.Uri!.AbsolutePath);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "{}", ProviderErrorKind.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, "{}", ProviderErrorKind.Forbidden)]
    [InlineData(HttpStatusCode.TooManyRequests, "{\"message\":\"rate limit\"}", ProviderErrorKind.RateLimited)]
    [InlineData(HttpStatusCode.TooManyRequests, "{\"message\":\"quota exhausted\"}", ProviderErrorKind.QuotaExceeded)]
    [InlineData(HttpStatusCode.InternalServerError, "{}", ProviderErrorKind.ServiceError)]
    public async Task TranslatePlus_NormalizesHttpErrors(
        HttpStatusCode statusCode,
        string responseBody,
        ProviderErrorKind expectedKind)
    {
        RecordingHandler handler = new(responseBody, statusCode);
        TranslatePlusProvider provider = new(new HttpClient(handler));
        TranslatePlusConfig config = Deserialize<TranslatePlusConfig>("""{"ApiKey":"plus-key"}""");

        ProviderException error = await Assert.ThrowsAsync<ProviderException>(() => provider.TranslateAsync(
            new TranslationRequest("hello", "en", "es", config), CancellationToken.None));

        Assert.Equal(expectedKind, error.Kind);
        Assert.Equal((int)statusCode, error.StatusCode);
    }

    [Fact]
    public async Task TranslatePlus_NormalizesTimeout()
    {
        TranslatePlusProvider provider = new(new HttpClient(new TimeoutHandler()));
        TranslatePlusConfig config = Deserialize<TranslatePlusConfig>("""{"ApiKey":"plus-key"}""");

        ProviderException error = await Assert.ThrowsAsync<ProviderException>(() => provider.TranslateAsync(
            new TranslationRequest("hello", "en", "es", config), CancellationToken.None));

        Assert.Equal(ProviderErrorKind.Timeout, error.Kind);
    }

    [Fact]
    public void SecretProtector_RoundTripsWithoutPlaintextStorage()
    {
        const string secret = "not-a-real-secret";
        string protectedValue = SecretProtector.Protect(secret);

        Assert.StartsWith("dpapi:", protectedValue);
        Assert.DoesNotContain(secret, protectedValue, StringComparison.Ordinal);
        Assert.Equal(secret, SecretProtector.Unprotect(protectedValue));
    }

    private static T Deserialize<T>(string json) where T : class =>
        JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("Test config could not be read.");

    private sealed class RecordingHandler(
        string responseBody,
        HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }
        public string Body { get; private set; } = string.Empty;
        public Dictionary<string, string> Headers { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            Body = request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            foreach (var header in request.Headers)
                Headers[header.Key] = string.Join(",", header.Value);
            return new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("Simulated provider timeout."));
    }
}
