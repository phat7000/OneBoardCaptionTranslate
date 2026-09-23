using System.Net.Http;
using System.Text.Json;

namespace LiveCaptionsTranslator.utils
{
    public static class UpdateUtil
    {
        // Disabled until an authoritative OneBoard release feed is configured.
        public static bool AutomaticUpdateChecksEnabled => false;

        public const string GitHubRepoUrl = "https://github.com/SakiRinn/LiveCaptions-Translator";
        public const string GitHubReleasesUrl = "https://github.com/SakiRinn/LiveCaptions-Translator/releases";
        public const string GitHubLatestReleaseApi = "https://api.github.com/repos/SakiRinn/LiveCaptions-Translator/releases/latest";

        public static async Task<string> GetLatestVersion()
        {
            string apiUrl = GitHubLatestReleaseApi;

            using var client = new HttpClient()
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("OneBoardCaptureTranslate");
            var response = await client.GetStringAsync(apiUrl);
            using var doc = JsonDocument.Parse(response);
            var latestVersionRaw = doc.RootElement.GetProperty("tag_name").GetString();
            var latestVersion = string.IsNullOrEmpty(latestVersionRaw)
                ? String.Empty
                : RegexPatterns.VersionNumber().Replace(latestVersionRaw, "");
            return latestVersion;
        }


    }
}
