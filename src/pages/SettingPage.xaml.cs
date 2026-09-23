using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

using Wpf.Ui.Appearance;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.services;
using LiveCaptionsTranslator.speech;
using LiveCaptionsTranslator.translation;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class SettingPage : Page
    {
        private static SettingWindow? settingWindow;
        private bool initialized;

        public SettingPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = Translator.Setting;

            SpeechProviderBox.ItemsSource = SpeechRecognitionService.Providers
                .Select(item => new ProviderChoice(item.Key, item.Value))
                .ToList();
            SpeechProviderBox.SelectedValue = Translator.Setting.SpeechProviderId;

            TranslateAPIBox.ItemsSource = TranslationProviderRegistry.ProviderIds
                .Select(id => new ProviderChoice(id, TranslationProviderRegistry.GetDisplayName(id)))
                .ToList();
            TranslateAPIBox.SelectedValue = Translator.Setting.ApiName;

            SourceLangBox.ItemsSource = LanguageCatalog.All;
            TargetLangBox.ItemsSource = LanguageCatalog.All;
            SourceLangBox.SelectedValue = Translator.Setting.SpeechLanguage;
            TargetLangBox.SelectedValue = Translator.Setting.TargetLanguage;

            initialized = true;
            UpdateSpeechControls();
            _ = UpdateTranslationLanguageStatusAsync();

            Loaded += (_, _) =>
            {
                if (Application.Current?.MainWindow is MainWindow window)
                    window.AutoHeightAdjust(minHeight: 250, maxHeight: 250);
                CheckForFirstUse();
            };
        }

        private void LiveCaptionsButton_click(object sender, RoutedEventArgs e)
        {
            if (Translator.Setting.SpeechProviderId != "WindowsLiveCaptions" || Translator.Window == null)
                return;

            bool hidden = Translator.Window.Current.BoundingRectangle == Rect.Empty;
            if (hidden)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                ButtonText.Text = "Hide";
            }
            else
            {
                LiveCaptionsHandler.HideLiveCaptions(Translator.Window);
                ButtonText.Text = "Show";
            }
        }

        private async void SpeechProviderBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized || SpeechProviderBox.SelectedValue is not string providerId)
                return;

            Translator.Setting.SpeechProviderId = providerId;
            UpdateSpeechControls();
            await SpeechRecognitionService.SwitchAsync(providerId);
        }

        private async void SourceLangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized || SourceLangBox.SelectedValue is not string languageCode)
                return;

            Translator.Setting.SpeechLanguage = languageCode;
            await SpeechRecognitionService.RestartSelectedAsync();
        }

        private async void SourceLangBox_LostFocus(object sender, RoutedEventArgs e)
        {
            LanguageDefinition? language = ResolveLanguageInput(SourceLangBox.Text);
            if (language == null)
            {
                Translator.Setting.SpeechStatus = $"[ERROR] Unknown speech language: {SourceLangBox.Text.Trim()}";
                SourceLangBox.SelectedValue = Translator.Setting.SpeechLanguage;
                return;
            }

            if (!string.Equals(Translator.Setting.SpeechLanguage, language.CanonicalCode, StringComparison.OrdinalIgnoreCase))
            {
                Translator.Setting.SpeechLanguage = language.CanonicalCode;
                await SpeechRecognitionService.RestartSelectedAsync();
            }
        }

        private void TranslateAPIBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized || TranslateAPIBox.SelectedValue is not string providerId)
                return;

            Translator.Setting.ApiName = providerId;
            _ = UpdateTranslationLanguageStatusAsync();
        }

        private void TargetLangBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized || TargetLangBox.SelectedValue is not string languageCode)
                return;

            Translator.Setting.TargetLanguage = languageCode;
            _ = UpdateTranslationLanguageStatusAsync();
        }

        private void TargetLangBox_LostFocus(object sender, RoutedEventArgs e)
        {
            LanguageDefinition? language = ResolveLanguageInput(TargetLangBox.Text);
            if (language == null)
            {
                TranslationLanguageStatusText.Foreground = Brushes.IndianRed;
                TranslationLanguageStatusText.Text = $"Unknown target language: {TargetLangBox.Text.Trim()}";
                TargetLangBox.SelectedValue = Translator.Setting.TargetLanguage;
                return;
            }

            Translator.Setting.TargetLanguage = language.CanonicalCode;
            _ = UpdateTranslationLanguageStatusAsync();
        }

        private static LanguageDefinition? ResolveLanguageInput(string input)
        {
            string value = input.Trim();
            return LanguageCatalog.All.FirstOrDefault(item =>
                       string.Equals(item.DisplayLabel, value, StringComparison.CurrentCultureIgnoreCase) ||
                       string.Equals(item.CanonicalCode, value, StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(item.SpeechLocale, value, StringComparison.OrdinalIgnoreCase))
                   ?? LanguageCatalog.Find(value);
        }

        private void APISettingButton_click(object sender, RoutedEventArgs e)
        {
            if (settingWindow is { IsLoaded: true })
            {
                settingWindow.Activate();
                return;
            }

            settingWindow = new SettingWindow();
            settingWindow.Closed += (_, _) => settingWindow = null;
            settingWindow.Show();
        }

        private void Contexts_ValueChanged(object sender, Wpf.Ui.Controls.NumberBoxValueChangedEventArgs args)
        {
            Translator.Caption?.OnPropertyChanged(nameof(Caption.DisplayLogCards));
        }

        private void CheckForFirstUse()
        {
            if (Translator.FirstUseFlag && Translator.Setting.SpeechProviderId == "WindowsLiveCaptions")
                ButtonText.Text = "Hide";
        }

        private void UpdateSpeechControls()
        {
            bool usesWindowsCaptions = Translator.Setting.SpeechProviderId == "WindowsLiveCaptions";
            LiveCaptionsButton.IsEnabled = usesWindowsCaptions;
            ButtonText.Text = usesWindowsCaptions ? "Show" : "Not used";
        }

        private async Task UpdateTranslationLanguageStatusAsync()
        {
            string providerId = Translator.Setting.ApiName;
            string providerCode = LanguageCatalog.GetTranslationCode(Translator.Setting.TargetLanguage, providerId);
            ITranslationProvider provider = TranslationProviderRegistry.Get(providerId);
            try
            {
                using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
                IReadOnlySet<string> supported = await provider.GetSupportedLanguagesAsync(
                    Translator.Setting[providerId],
                    timeout.Token);
                if (supported.Count > 0 && !supported.Contains(providerCode))
                {
                    TranslationLanguageStatusText.Foreground = Brushes.IndianRed;
                    TranslationLanguageStatusText.Text =
                        $"Unsupported by {provider.DisplayName}: {Translator.Setting.TargetLanguage} ({providerCode}). No fallback will be used.";
                }
                else
                {
                    TranslationLanguageStatusText.Foreground = Brushes.SeaGreen;
                    TranslationLanguageStatusText.Text =
                        $"{provider.DisplayName} target: {providerCode}.";
                }
            }
            catch
            {
                TranslationLanguageStatusText.Foreground = Brushes.DarkGoldenrod;
                TranslationLanguageStatusText.Text =
                    $"Offline catalog target: {providerCode}. Provider support will be verified on request.";
            }
        }

        private sealed record ProviderChoice(string Id, string DisplayName);
    }
}
