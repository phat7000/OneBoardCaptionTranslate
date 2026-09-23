using System.Windows;

using LiveCaptionsTranslator.speech;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }

        App()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
            Translator.Setting?.Save();

            Task.Run(() => Translator.TranslateLoop());
            Task.Run(() => Translator.DisplayLoop());
            Task.Run(() => SpeechRecognitionService.StartSelectedAsync());
        }

        private static void OnProcessExit(object? sender, EventArgs e)
        {
            try
            {
                using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(3));
                SpeechRecognitionService.StopAsync(timeout.Token).GetAwaiter().GetResult();
            }
            catch
            {
                if (Translator.Window != null)
                {
                    LiveCaptionsHandler.RestoreLiveCaptions(Translator.Window);
                    LiveCaptionsHandler.KillLiveCaptions(Translator.Window);
                }
            }
        }
    }
}
