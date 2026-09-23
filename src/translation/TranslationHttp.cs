using System.Net;
using System.Net.Http;
using System.Text.Json;

namespace LiveCaptionsTranslator.translation
{
    internal static class TranslationHttp
    {
        public static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
                return;

            string body = await response.Content.ReadAsStringAsync(cancellationToken);
            string detail = TryReadMessage(body);
            int status = (int)response.StatusCode;
            ProviderErrorKind kind = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => ProviderErrorKind.Unauthorized,
                HttpStatusCode.Forbidden => ProviderErrorKind.Forbidden,
                HttpStatusCode.TooManyRequests =>
                    detail.Contains("quota", StringComparison.OrdinalIgnoreCase)
                        ? ProviderErrorKind.QuotaExceeded
                        : ProviderErrorKind.RateLimited,
                HttpStatusCode.BadRequest when detail.Contains("language", StringComparison.OrdinalIgnoreCase) =>
                    ProviderErrorKind.UnsupportedLanguage,
                _ when status >= 500 => ProviderErrorKind.ServiceError,
                _ => ProviderErrorKind.ServiceError
            };

            string prefix = kind switch
            {
                ProviderErrorKind.Unauthorized => "Unauthorized: check the API key.",
                ProviderErrorKind.Forbidden => "Access forbidden: check the API key and enabled service.",
                ProviderErrorKind.RateLimited => "Rate limited by the provider.",
                ProviderErrorKind.QuotaExceeded => "Provider quota exceeded.",
                ProviderErrorKind.UnsupportedLanguage => "The selected language is unsupported by this provider.",
                _ when status >= 500 => "The provider service is unavailable.",
                _ => $"Provider request failed (HTTP {status})."
            };

            throw new ProviderException(kind, string.IsNullOrWhiteSpace(detail) ? prefix : $"{prefix} {detail}", status);
        }

        public static ProviderException NormalizeTransportException(
            Exception exception,
            CancellationToken callerToken)
        {
            if (exception is OperationCanceledException && !callerToken.IsCancellationRequested)
                return new ProviderException(ProviderErrorKind.Timeout, "The provider request timed out.", inner: exception);
            if (exception is HttpRequestException)
                return new ProviderException(
                    ProviderErrorKind.NetworkUnavailable,
                    "The provider could not be reached. Check the network connection.",
                    inner: exception);
            if (exception is JsonException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
                return new ProviderException(
                    ProviderErrorKind.InvalidResponse,
                    "The provider returned an unexpected response.",
                    inner: exception);
            return exception as ProviderException ??
                   new ProviderException(ProviderErrorKind.ServiceError, exception.Message, inner: exception);
        }

        private static string TryReadMessage(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return string.Empty;

            try
            {
                using JsonDocument document = JsonDocument.Parse(body);
                JsonElement root = document.RootElement;
                if (root.TryGetProperty("error", out JsonElement error))
                {
                    if (error.ValueKind == JsonValueKind.String)
                        return error.GetString() ?? string.Empty;
                    if (error.TryGetProperty("message", out JsonElement nestedMessage))
                        return nestedMessage.GetString() ?? string.Empty;
                }
                if (root.TryGetProperty("message", out JsonElement message))
                    return message.GetString() ?? string.Empty;
                if (root.TryGetProperty("detail", out JsonElement detail))
                    return detail.GetString() ?? string.Empty;
            }
            catch (JsonException)
            {
            }

            const int maxLength = 240;
            return body.Length <= maxLength ? body : body[..maxLength];
        }
    }
}
