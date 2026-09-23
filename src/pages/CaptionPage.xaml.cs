using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using LiveCaptionsTranslator.Utils;

namespace LiveCaptionsTranslator
{
    public partial class CaptionPage : Page
    {
        private static CaptionPage? instance;
        private bool originalAutoFollow = true;
        private bool translatedAutoFollow = true;

        public static CaptionPage? Instance => instance;

        public CaptionPage()
        {
            InitializeComponent();
            DataContext = Translator.Caption;
            instance = this;

            if (Translator.Caption != null)
            {
                Translator.Caption.OriginalTranscript.Changed += OriginalTranscript_Changed;
                Translator.Caption.TranslatedTranscript.Changed += TranslatedTranscript_Changed;
            }

            Loaded += (_, _) =>
            {
                AutoHeight();
                if (Application.Current?.MainWindow is MainWindow window)
                    window.CaptionLogButton.Visibility = Visibility.Visible;
                FollowOriginal();
                FollowTranslated();
            };
            Unloaded += (_, _) =>
            {
                if (Application.Current?.MainWindow is MainWindow window)
                    window.CaptionLogButton.Visibility = Visibility.Collapsed;
            };

            CollapseTranslatedCaption(Translator.Setting.MainWindow.CaptionLogEnabled);
            ApplyFontSizes();
        }

        private async void TextBlock_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            if (sender is not TextBlock textBlock)
                return;
            try
            {
                Clipboard.SetText(textBlock.Text);
                SnackbarHost.Show("Copied.", textBlock.Text, SnackbarType.Info, 100);
            }
            catch
            {
                SnackbarHost.Show("Copy failed.", string.Empty, SnackbarType.Error, 100);
            }
            await Task.Delay(500);
        }

        private void ApplyFontSizes()
        {
            OriginalTranscriptItems.FontSize = Translator.Setting.MainWindow.OriginalFontSize;
            TranslatedTranscriptItems.FontSize = Translator.Setting.MainWindow.TranslatedFontSize;
        }

        private void OriginalCard_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;
            Translator.Setting.MainWindow.OriginalFontSize =
                AdjustFontSize(Translator.Setting.MainWindow.OriginalFontSize, e.Delta);
            ApplyFontSizes();
            e.Handled = true;
        }

        private void TranslatedCard_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
                return;
            Translator.Setting.MainWindow.TranslatedFontSize =
                AdjustFontSize(Translator.Setting.MainWindow.TranslatedFontSize, e.Delta);
            ApplyFontSizes();
            e.Handled = true;
        }

        private static int AdjustFontSize(int current, int wheelDelta)
        {
            int next = current + (wheelDelta > 0 ? StyleConsts.DELTA_FONT_SIZE : -StyleConsts.DELTA_FONT_SIZE);
            return Math.Clamp(next, StyleConsts.MIN_FONT_SIZE, StyleConsts.MAX_FONT_SIZE);
        }

        public void CollapseTranslatedCaption(bool showLogCards)
        {
            LogCards.Visibility = showLogCards ? Visibility.Visible : Visibility.Collapsed;
            CaptionLogCard_Row.Height = showLogCards ? GridLength.Auto : new GridLength(0);
        }

        public void AutoHeight()
        {
            if (Application.Current?.MainWindow is MainWindow window)
                window.AutoHeightAdjust(minHeight: (int)window.MinHeight);
        }

        private void OriginalTranscript_Changed(object? sender, EventArgs e)
        {
            if (originalAutoFollow)
                Dispatcher.BeginInvoke(FollowOriginal, DispatcherPriority.Background);
        }

        private void TranslatedTranscript_Changed(object? sender, EventArgs e)
        {
            if (translatedAutoFollow)
                Dispatcher.BeginInvoke(FollowTranslated, DispatcherPriority.Background);
        }

        private void OriginalScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) =>
            originalAutoFollow = UpdateAutoFollow(OriginalScrollViewer, e, originalAutoFollow);

        private void TranslatedScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e) =>
            translatedAutoFollow = UpdateAutoFollow(TranslatedScrollViewer, e, translatedAutoFollow);

        private static bool UpdateAutoFollow(ScrollViewer viewer, ScrollChangedEventArgs e, bool current)
        {
            if (e.ExtentHeightChange != 0 || e.ViewportHeightChange != 0)
                return current;
            return viewer.ScrollableHeight - viewer.VerticalOffset <= 2;
        }

        private void FollowOriginal()
        {
            OriginalScrollViewer.ScrollToEnd();
            originalAutoFollow = true;
        }

        private void FollowTranslated()
        {
            TranslatedScrollViewer.ScrollToEnd();
            translatedAutoFollow = true;
        }
    }
}
