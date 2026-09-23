using System.IO;
using System.Security;

namespace LiveCaptionsTranslator.utils
{
    public static class ApplicationDataPaths
    {
        public const string SettingsFileName = "setting.json";
        public const string HistoryDatabaseFileName = "translation_history.db";

        public static string ApplicationDataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneBoard",
            "OneBoard Capture Translate");

        private static string LegacyApplicationDataDirectory { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OneBoard",
            "OneBoard Caption Translate");

        public static string SettingsFile { get; } =
            Path.Combine(ApplicationDataDirectory, SettingsFileName);

        public static string HistoryDatabase { get; } =
            Path.Combine(ApplicationDataDirectory, HistoryDatabaseFileName);

        static ApplicationDataPaths()
        {
            try
            {
                Directory.CreateDirectory(ApplicationDataDirectory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
            {
                return;
            }

            MigrateLegacyFile(SettingsFileName, SettingsFile);
            MigrateLegacyFile(HistoryDatabaseFileName, HistoryDatabase);
        }

        private static void MigrateLegacyFile(string fileName, string destinationPath)
        {
            if (File.Exists(destinationPath))
                return;

            string[] legacyDirectories =
            {
                LegacyApplicationDataDirectory,
                AppContext.BaseDirectory,
                Directory.GetCurrentDirectory()
            };

            foreach (string legacyDirectory in legacyDirectories.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                string sourcePath = Path.Combine(legacyDirectory, fileName);
                if (string.Equals(sourcePath, destinationPath, StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(sourcePath))
                    continue;

                try
                {
                    File.Copy(sourcePath, destinationPath, overwrite: false);
                    return;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException)
                {
                    if (File.Exists(destinationPath))
                        return;
                }
            }
        }
    }
}
