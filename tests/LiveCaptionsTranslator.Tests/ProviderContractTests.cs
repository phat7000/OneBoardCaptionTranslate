using LiveCaptionsTranslator.apis;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.speech;
using LiveCaptionsTranslator.translation;

namespace LiveCaptionsTranslator.Tests;

public class ProviderContractTests
{
    [Fact]
    public void NewTranslationProviders_AreRegisteredAlongsideExistingProviders()
    {
        string[] required =
        {
            "Google", "DeepL", "MicrosoftTranslator", "GoogleCloudTranslation", "TranslatePlus", "Langbly"
        };
        foreach (string provider in required)
            Assert.Contains(provider, TranslateAPI.TRANSLATE_FUNCTIONS.Keys);
    }

    [Fact]
    public void SpeechProviderSelection_IsExplicitAndComplete()
    {
        Assert.Equal(3, SpeechRecognitionService.Providers.Count);
        Assert.Contains("WindowsLiveCaptions", SpeechRecognitionService.Providers.Keys);
        Assert.Contains("AzureSpeech", SpeechRecognitionService.Providers.Keys);
        Assert.Contains("GoogleSpeech", SpeechRecognitionService.Providers.Keys);
    }

    [Theory]
    [InlineData("MicrosoftTranslator")]
    [InlineData("GoogleCloudTranslation")]
    [InlineData("TranslatePlus")]
    [InlineData("Langbly")]
    public async Task CloudTranslationProviders_RejectMissingCredentials(string providerId)
    {
        ITranslationProvider provider = TranslationProviderRegistry.Get(providerId);
        TranslateAPIConfig config = providerId switch
        {
            "MicrosoftTranslator" => new MicrosoftTranslatorConfig(),
            "GoogleCloudTranslation" => new GoogleCloudTranslationConfig(),
            "TranslatePlus" => new TranslatePlusConfig(),
            "Langbly" => new LangblyConfig(),
            _ => throw new InvalidOperationException()
        };

        ProviderValidationResult result = await provider.ValidateConfigurationAsync(config, CancellationToken.None);
        Assert.False(result.IsValid);
    }
}
