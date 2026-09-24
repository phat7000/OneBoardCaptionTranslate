using System.Text.Json.Serialization;

namespace LiveCaptionsTranslator.speech
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AudioSourceType
    {
        SystemAudio,
        Microphone,
        ExternalAudioInput
    }

    public static class AudioSourceTypeExtensions
    {
        public static string ToDisplayName(this AudioSourceType sourceType) => sourceType switch
        {
            AudioSourceType.SystemAudio => "System Audio",
            AudioSourceType.Microphone => "Microphone",
            AudioSourceType.ExternalAudioInput => "External Audio Input",
            _ => sourceType.ToString()
        };
    }
}
