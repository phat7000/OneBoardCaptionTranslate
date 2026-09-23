using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.translation
{
    public interface ITranslationProvider
    {
        string Id { get; }
        string DisplayName { get; }
        bool SupportsAutoDetect { get; }
        bool RequiresApiKey { get; }

        Task<TranslationResult> TranslateAsync(TranslationRequest request, CancellationToken cancellationToken);
        Task<ProviderValidationResult> ValidateConfigurationAsync(
            TranslateAPIConfig configuration,
            CancellationToken cancellationToken);
        Task<IReadOnlySet<string>> GetSupportedLanguagesAsync(
            TranslateAPIConfig configuration,
            CancellationToken cancellationToken);
    }

    public sealed record TranslationRequest(
        string Text,
        string? SourceLanguage,
        string TargetLanguage,
        TranslateAPIConfig Configuration);

    public sealed record TranslationResult(string Text, string? DetectedSourceLanguage = null);

    public sealed record ProviderValidationResult(bool IsValid, string Message)
    {
        public static ProviderValidationResult Valid(string message = "Configuration is valid.") => new(true, message);
        public static ProviderValidationResult Invalid(string message) => new(false, message);
    }

    public enum ProviderErrorKind
    {
        InvalidConfiguration,
        Unauthorized,
        Forbidden,
        RateLimited,
        QuotaExceeded,
        NetworkUnavailable,
        Timeout,
        UnsupportedLanguage,
        ServiceError,
        InvalidResponse
    }

    public sealed class ProviderException : Exception
    {
        public ProviderErrorKind Kind { get; }
        public int? StatusCode { get; }

        public ProviderException(ProviderErrorKind kind, string message, int? statusCode = null, Exception? inner = null)
            : base(message, inner)
        {
            Kind = kind;
            StatusCode = statusCode;
        }
    }
}
