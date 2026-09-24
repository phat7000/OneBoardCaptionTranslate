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
        private readonly IExternalAudioDeviceService externalAudioDeviceService = new ExternalAudioDeviceService();
        private bool initialized;
        private bool isCompactLayout;
        private const double WideLayoutThreshold = 900;

        public SettingPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = Translator.Setting;

            SpeechProviderBox.ItemsSource = SpeechRecognitionService.Providers
                .Select(item => new ProviderChoice(item.Key, item.Value))
                .ToList();
            SpeechProviderBox.SelectedValue = Translator.Setting.SpeechProviderId;

            AudioSourceBox.ItemsSource = Enum.GetValues<AudioSourceType>()
                .Select(value => new AudioSourceChoice(value, value.ToDisplayName()))
                .ToList();
            AudioSourceBox.SelectedValue = Translator.Setting.AudioSource;
            RefreshExternalAudioDevices(reportMissing: false);

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
            UpdateProviderSettingsButton();
            _ = UpdateTranslationLanguageStatusAsync();

            Loaded += (_, _) =>
            {
                if (Application.Current?.MainWindow is MainWindow window)
                    window.AutoHeightAdjust(minHeight: 300, maxHeight: 330);
                ApplyResponsiveTopLayout(TopSettingsGrid.ActualWidth);
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

        private async void AudioSourceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized || AudioSourceBox.SelectedValue is not AudioSourceType audioSource ||
                Translator.Setting.SpeechProviderId == "WindowsLiveCaptions")
                return;

            if (Translator.Setting.AudioSource == audioSource)
                return;

            Translator.Setting.AudioSource = audioSource;
            AudioSourceBox.ToolTip = audioSource.ToDisplayName();
            UpdateSpeechControls();
            await SpeechRecognitionService.RestartSelectedAsync();
        }

        private async void ExternalAudioDeviceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initialized || ExternalAudioDeviceBox.SelectedItem is not AudioInputDevice selected)
                return;

            if (string.Equals(Translator.Setting.ExternalAudioDeviceId, selected.DeviceId, StringComparison.Ordinal))
                return;

            Translator.Setting.ExternalAudioDeviceId = selected.DeviceId;
            Translator.Setting.ExternalAudioDeviceDisplayName = selected.DisplayName;
            ExternalAudioDeviceBox.ToolTip = selected.DisplayName;

            if (Translator.Setting.AudioSource == AudioSourceType.ExternalAudioInput &&
                Translator.Setting.SpeechProviderId != "WindowsLiveCaptions")
                await SpeechRecognitionService.RestartSelectedAsync();
        }

        private void RefreshDevicesButton_Click(object sender, RoutedEventArgs e) =>
            RefreshExternalAudioDevices(reportMissing: true);

        private void RefreshExternalAudioDevices(bool reportMissing)
        {
            try
            {
                IReadOnlyList<AudioInputDevice> devices = externalAudioDeviceService.GetActiveCaptureDevices();
                ExternalAudioDeviceBox.ItemsSource = devices;
                ExternalAudioDeviceBox.SelectedValue = Translator.Setting.ExternalAudioDeviceId;

                bool hasSavedDevice = !string.IsNullOrWhiteSpace(Translator.Setting.ExternalAudioDeviceId);
                bool savedDeviceAvailable = devices.Any(device =>
                    string.Equals(device.DeviceId, Translator.Setting.ExternalAudioDeviceId, StringComparison.Ordinal));
                if (reportMissing && hasSavedDevice && !savedDeviceAvailable)
                    Translator.Setting.SpeechStatus = "[ERROR] Selected external audio device is unavailable. Select another input device.";
                else if (reportMissing && devices.Count == 0)
                    Translator.Setting.SpeechStatus = "[ERROR] No active Windows recording devices were found.";
            }
            catch (Exception ex)
            {
                ExternalAudioDeviceBox.ItemsSource = Array.Empty<AudioInputDevice>();
                if (reportMissing)
                    Translator.Setting.SpeechStatus = $"[ERROR] Unable to enumerate recording devices: {ex.Message}";
            }
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
            UpdateProviderSettingsButton();
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
            AudioSourceBox.IsEnabled = !usesWindowsCaptions;
            AudioSourceLabel.Text = usesWindowsCaptions ? "Audio Source (Not used)" : "Audio Source";
            AudioSourceBox.ToolTip = usesWindowsCaptions
                ? "Windows Live Captions manages its own audio input."
                : (AudioSourceBox.SelectedItem as AudioSourceChoice)?.DisplayName;
            bool usesExternalInput = !usesWindowsCaptions &&
                Translator.Setting.AudioSource == AudioSourceType.ExternalAudioInput;
            ExternalAudioDevicePanel.Visibility = usesExternalInput ? Visibility.Visible : Visibility.Collapsed;
            ExternalAudioDeviceBox.IsEnabled = usesExternalInput;
            RefreshDevicesButton.IsEnabled = usesExternalInput;
        }

        private void UpdateProviderSettingsButton()
        {
            string displayName = TranslationProviderRegistry.GetDisplayName(Translator.Setting.ApiName);
            ProviderSettingsButton.Content = $"Configure {displayName}";
            ProviderSettingsButton.ToolTip = $"Open credentials and options for {displayName}.";
        }

        private void TopSettingsGrid_SizeChanged(object sender, SizeChangedEventArgs e) =>
            ApplyResponsiveTopLayout(e.NewSize.Width);

        private void ApplyResponsiveTopLayout(double availableWidth)
        {
            bool compact = availableWidth < WideLayoutThreshold;
            if (compact == isCompactLayout && TopSettingsGrid.IsLoaded)
                return;

            isCompactLayout = compact;
            if (compact)
            {
                TopSettingsGrid.ColumnDefinitions[0].Width = new GridLength(1.1, GridUnitType.Star);
                TopSettingsGrid.ColumnDefinitions[1].Width = new GridLength(1.1, GridUnitType.Star);
                TopSettingsGrid.ColumnDefinitions[2].Width = new GridLength(0);

                PositionPanel(SpeechProviderPanel, 0, 0);
                PositionPanel(AudioSourcePanel, 0, 1);
                PositionPanel(SpeechLanguagePanel, 1, 0);
                PositionPanel(ExternalAudioDevicePanel, 1, 1);
                PositionPanel(TranslationProviderPanel, 2, 0);
                PositionPanel(TargetLanguagePanel, 2, 1);
            }
            else
            {
                double[] widths = [1.1, 1.1, 1.4];
                for (int index = 0; index < widths.Length; index++)
                    TopSettingsGrid.ColumnDefinitions[index].Width = new GridLength(widths[index], GridUnitType.Star);

                PositionPanel(SpeechProviderPanel, 0, 0);
                PositionPanel(AudioSourcePanel, 0, 1);
                PositionPanel(SpeechLanguagePanel, 0, 2);
                PositionPanel(ExternalAudioDevicePanel, 1, 0);
                PositionPanel(TranslationProviderPanel, 1, 1);
                PositionPanel(TargetLanguagePanel, 1, 2);
            }
        }

        private static void PositionPanel(UIElement panel, int row, int column)
        {
            Grid.SetRow(panel, row);
            Grid.SetColumn(panel, column);
            Grid.SetColumnSpan(panel, 1);
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
        private sealed record AudioSourceChoice(AudioSourceType Value, string DisplayName);
    }
}
