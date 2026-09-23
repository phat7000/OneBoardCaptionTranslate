namespace LiveCaptionsTranslator.speech
{
    public interface ISpeechRecognitionProvider : IAsyncDisposable
    {
        string Id { get; }
        string DisplayName { get; }
        bool IsRunning { get; }

        event EventHandler<SpeechResult>? ResultReceived;
        event EventHandler<SpeechProviderStatus>? StatusChanged;

        Task<SpeechConfigurationValidation> ValidateConfigurationAsync(CancellationToken cancellationToken);
        Task StartAsync(string languageCode, CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }

    public sealed record SpeechResult(
        string Text,
        string Language,
        bool IsFinal,
        DateTimeOffset Timestamp,
        bool IsFullSnapshot = false);

    public sealed record SpeechProviderStatus(bool IsError, string Message);

    public sealed record SpeechConfigurationValidation(bool IsValid, string Message)
    {
        public static SpeechConfigurationValidation Valid(string message = "Configuration is valid.") =>
            new(true, message);
        public static SpeechConfigurationValidation Invalid(string message) => new(false, message);
    }
}
