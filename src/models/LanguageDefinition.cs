namespace LiveCaptionsTranslator.models
{
    public sealed record LanguageDefinition(
        string CanonicalCode,
        string DisplayName,
        string NativeName,
        string TranslationCode,
        string SpeechLocale,
        bool IsRtl = false,
        IReadOnlyDictionary<string, string>? ProviderCodes = null)
    {
        public string DisplayLabel => $"{DisplayName} — {CanonicalCode}";

        public string GetProviderCode(string providerId, bool speech)
        {
            if (ProviderCodes != null && ProviderCodes.TryGetValue(providerId, out string? code))
                return code;

            return speech ? SpeechLocale : TranslationCode;
        }

        public override string ToString() => DisplayLabel;
    }
}
