using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator.models
{
    public abstract class SpeechProviderConfig : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            Translator.Setting?.Save();
        }
    }

    public sealed class AzureSpeechConfig : SpeechProviderConfig
    {
        private string apiKey = string.Empty;
        private string region = string.Empty;

        [JsonIgnore]
        public string ApiKey
        {
            get => apiKey;
            set { apiKey = value; OnPropertyChanged(); }
        }

        [JsonInclude, JsonPropertyName("ApiKey")]
        public string ProtectedApiKey
        {
            get => SecretProtector.Protect(apiKey);
            private set => apiKey = SecretProtector.Unprotect(value);
        }

        public string Region
        {
            get => region;
            set { region = value; OnPropertyChanged(); }
        }
    }

    public sealed class GoogleSpeechConfig : SpeechProviderConfig
    {
        private string credentialsFilePath = string.Empty;

        /// <summary>
        /// Path to a user-managed service-account JSON file. The file itself is never copied or packaged.
        /// </summary>
        public string CredentialsFilePath
        {
            get => credentialsFilePath;
            set { credentialsFilePath = value; OnPropertyChanged(); }
        }
    }
}
