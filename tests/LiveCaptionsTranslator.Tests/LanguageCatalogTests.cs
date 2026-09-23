using LiveCaptionsTranslator.services;

namespace LiveCaptionsTranslator.Tests;

public class LanguageCatalogTests
{
    [Fact]
    public void Catalog_IsBroadUniqueAndContainsRequiredLocales()
    {
        Assert.True(LanguageCatalog.All.Count >= 75);
        Assert.Equal(
            LanguageCatalog.All.Count,
            LanguageCatalog.All.Select(language => language.CanonicalCode)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count());

        foreach (string code in new[]
                 {
                     "vi-VN", "en-US", "zh-CN", "ja-JP", "ko-KR", "th-TH", "fr-FR", "de-DE", "es-ES"
                 })
            Assert.NotNull(LanguageCatalog.Find(code));
    }

    [Theory]
    [InlineData("vi-VN", "MicrosoftTranslator", "vi")]
    [InlineData("en-US", "GoogleCloudTranslation", "en")]
    [InlineData("zh-CN", "MicrosoftTranslator", "zh-Hans")]
    [InlineData("zh-CN", "Langbly", "zh-CN")]
    [InlineData("ja-JP", "TranslatePlus", "ja")]
    [InlineData("pt-BR", "DeepL", "PT-BR")]
    public void TranslationMappings_AreProviderAware(string canonical, string provider, string expected) =>
        Assert.Equal(expected, LanguageCatalog.GetTranslationCode(canonical, provider));

    [Theory]
    [InlineData("vi-VN")]
    [InlineData("en-US")]
    [InlineData("zh-CN")]
    [InlineData("ja-JP")]
    public void SpeechMappings_PreserveBcp47Locale(string canonical) =>
        Assert.Equal(canonical, LanguageCatalog.GetSpeechLocale(canonical, "AzureSpeech"));
}
