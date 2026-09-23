using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text;
using System.Windows.Automation;

using LiveCaptionsTranslator.apis;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.speech;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public static class Translator
    {
        private static AutomationElement? window = null;
        private static readonly Caption caption;
        private static readonly Setting setting;

        private static readonly ConcurrentQueue<PendingSpeechText> pendingTextQueue = new();
        private static readonly TranslationTaskQueue translationTaskQueue = new();
        private static int cloudPartialCount;
        private static int windowsIdleCount;
        private static int windowsSyncCount;

        public static AutomationElement? Window
        {
            get => window;
            set => window = value;
        }
        public static Caption Caption => caption;
        public static Setting Setting => setting;

        public static bool LogOnlyFlag { get; set; } = false;
        public static bool FirstUseFlag { get; set; } = false;

        public static event Action? TranslationLogged;

        static Translator()
        {
            if (!models.Setting.IsConfigExist())
                FirstUseFlag = true;

            setting = Setting.Load();
            caption = Caption.GetInstance();
        }

        public static void AcceptSpeechResult(SpeechResult result)
        {
            if (string.IsNullOrWhiteSpace(result.Text))
                return;

            if (result.IsFullSnapshot)
            {
                ProcessWindowsCaptionSnapshot(result.Text, result.Timestamp);
                return;
            }

            string text = result.Text.Trim();
            Caption.DisplayOriginalCaption = text;
            Caption.OverlayOriginalCaption = text;
            Caption.OriginalCaption = text;
            if (result.IsFinal)
            {
                cloudPartialCount = 0;
                Caption.OriginalTranscript.FinalizeLine(text, result.Timestamp);
                pendingTextQueue.Enqueue(new PendingSpeechText(text, true));
            }
            else
            {
                Caption.OriginalTranscript.UpdatePartial(text, result.Timestamp);
                cloudPartialCount++;
                if (Encoding.UTF8.GetByteCount(text) >= TextUtil.SHORT_THRESHOLD &&
                    cloudPartialCount > Setting.MaxSyncInterval)
                {
                    cloudPartialCount = 0;
                    pendingTextQueue.Enqueue(new PendingSpeechText(text, false));
                }
            }
        }

        private static void ProcessWindowsCaptionSnapshot(string snapshot, DateTimeOffset timestamp)
        {
            string fullText = RegexPatterns.Acronym().Replace(snapshot, "$1$2");
            fullText = RegexPatterns.AcronymWithWords().Replace(fullText, "$1 $2");
            fullText = RegexPatterns.PunctuationSpace().Replace(fullText, "$1 ");
            fullText = RegexPatterns.CJPunctuationSpace().Replace(fullText, "$1");
            fullText = TextUtil.ReplaceNewlines(fullText, TextUtil.MEDIUM_THRESHOLD);
            if (string.IsNullOrWhiteSpace(fullText))
                return;

            if (fullText.IndexOfAny(TextUtil.PUNC_EOS) == -1 && Caption.Contexts.Count > 0)
                ClearContexts();

            int lastEOSIndex = Array.IndexOf(TextUtil.PUNC_EOS, fullText[^1]) != -1
                ? fullText[0..^1].LastIndexOfAny(TextUtil.PUNC_EOS)
                : fullText.LastIndexOfAny(TextUtil.PUNC_EOS);
            string latestCaption = fullText[(lastEOSIndex + 1)..];
            if (lastEOSIndex > 0 && Encoding.UTF8.GetByteCount(latestCaption) < TextUtil.SHORT_THRESHOLD)
            {
                lastEOSIndex = fullText[0..lastEOSIndex].LastIndexOfAny(TextUtil.PUNC_EOS);
                latestCaption = fullText[(lastEOSIndex + 1)..];
            }

            Caption.OverlayOriginalCaption = latestCaption;
            Caption.DisplayOriginalCaption = TextUtil.ShortenDisplaySentence(latestCaption, TextUtil.VERYLONG_THRESHOLD);
            bool hasFinalSentence = latestCaption.Length > 0 &&
                                    Array.IndexOf(TextUtil.PUNC_EOS, latestCaption[^1]) != -1;
            if (hasFinalSentence)
                Caption.OriginalTranscript.FinalizeLine(latestCaption, timestamp);
            else
                Caption.OriginalTranscript.UpdatePartial(latestCaption, timestamp);

            int lastEOS = latestCaption.LastIndexOfAny(TextUtil.PUNC_EOS);
            string translatable = lastEOS == -1 ? latestCaption : latestCaption[..(lastEOS + 1)];
            if (!string.IsNullOrWhiteSpace(translatable) &&
                !string.Equals(Caption.OriginalCaption, translatable, StringComparison.Ordinal))
            {
                Caption.OriginalCaption = translatable;
                windowsIdleCount = 0;
                if (hasFinalSentence)
                {
                    windowsSyncCount = 0;
                    pendingTextQueue.Enqueue(new PendingSpeechText(translatable, true));
                }
                else if (Encoding.UTF8.GetByteCount(translatable) >= TextUtil.SHORT_THRESHOLD)
                {
                    windowsSyncCount++;
                }
            }
            else
            {
                windowsIdleCount++;
            }

            if (!string.IsNullOrWhiteSpace(Caption.OriginalCaption) &&
                (windowsSyncCount > Setting.MaxSyncInterval || windowsIdleCount == Setting.MaxIdleInterval))
            {
                windowsSyncCount = 0;
                pendingTextQueue.Enqueue(new PendingSpeechText(Caption.OriginalCaption, false));
            }
        }

        public static async Task TranslateLoop()
        {
            while (true)
            {
                // Translate
                if (pendingTextQueue.TryDequeue(out PendingSpeechText? pending) && pending != null)
                {
                    string originalSnapshot = pending.Text;

                    if (LogOnlyFlag)
                    {
                        bool isOverwrite = await IsOverwrite(originalSnapshot);
                        await LogOnly(originalSnapshot, isOverwrite);
                    }
                    else
                    {
                        translationTaskQueue.Enqueue(token => Task.Run(
                            () => Translate(originalSnapshot, pending.IsFinal, token), token), originalSnapshot);
                    }
                }

                Thread.Sleep(40);
            }
        }

        public static async Task DisplayLoop()
        {
            while (true)
            {
                var (translatedText, isChoke) = translationTaskQueue.Output;

                if (LogOnlyFlag)
                {
                    Caption.TranslatedCaption = string.Empty;
                    Caption.DisplayTranslatedCaption = "[Paused]";
                    Caption.OverlayNoticePrefix = "[Paused]";
                    Caption.OverlayCurrentTranslation = string.Empty;
                }
                else if (!string.IsNullOrEmpty(RegexPatterns.NoticePrefix().Replace(
                             translatedText, string.Empty).Trim()) &&
                         string.CompareOrdinal(Caption.TranslatedCaption, translatedText) != 0)
                {
                    // Main page
                    Caption.TranslatedCaption = translatedText;
                    Caption.DisplayTranslatedCaption =
                        TextUtil.ShortenDisplaySentence(Caption.TranslatedCaption, TextUtil.VERYLONG_THRESHOLD);

                    if (isChoke)
                        Caption.TranslatedTranscript.FinalizeLine(translatedText, DateTimeOffset.UtcNow);
                    else
                        Caption.TranslatedTranscript.UpdatePartial(translatedText, DateTimeOffset.UtcNow);

                    // Overlay window
                    if (Caption.TranslatedCaption.Contains("[ERROR]") || Caption.TranslatedCaption.Contains("[WARNING]"))
                        Caption.OverlayCurrentTranslation = Caption.TranslatedCaption;
                    else
                    {
                        var match = RegexPatterns.NoticePrefixAndTranslation().Match(Caption.TranslatedCaption);
                        Caption.OverlayNoticePrefix = match.Groups[1].Value.Trim();
                        Caption.OverlayCurrentTranslation = match.Groups[2].Value.Trim();
                    }
                }

                Thread.Sleep(40);
            }
        }

        public static async Task<(string, bool)> Translate(string text, CancellationToken token = default)
        {
            bool inferredFinal = !string.IsNullOrEmpty(text) && Array.IndexOf(TextUtil.PUNC_EOS, text[^1]) != -1;
            return await Translate(text, inferredFinal, token);
        }

        private static async Task<(string, bool)> Translate(
            string text,
            bool isFinal,
            CancellationToken token = default)
        {
            string translatedText;
            bool isChoke = isFinal;

            try
            {
                var sw = Setting.MainWindow.LatencyShow ? Stopwatch.StartNew() : null;

                if (Setting.ContextAware && !TranslateAPI.IsLLMBased)
                {
                    translatedText = await TranslateAPI.TranslateFunction($"{Caption.AwareContextsCaption} 🔤 {text} 🔤", token);
                    translatedText = RegexPatterns.TargetSentence().Match(translatedText).Groups[1].Value;
                }
                else
                {
                    translatedText = await TranslateAPI.TranslateFunction(text, token);
                    translatedText = translatedText.Replace("🔤", "");
                }

                if (sw != null)
                {
                    sw.Stop();
                    translatedText = $"[{sw.ElapsedMilliseconds,4} ms] " + translatedText;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return ($"[ERROR] Translation Failed: {ex.Message}", isChoke);
            }

            return (translatedText, isChoke);
        }

        private sealed record PendingSpeechText(string Text, bool IsFinal);

        public static async Task Log(string originalText, string translatedText,
            bool isOverwrite = false, CancellationToken token = default)
        {
            string targetLanguage, apiName;
            if (Setting != null)
            {
                targetLanguage = Setting.TargetLanguage;
                apiName = Setting.ApiName;
            }
            else
            {
                targetLanguage = "N/A";
                apiName = "N/A";
            }

            try
            {
                if (isOverwrite)
                    await SQLiteHistoryLogger.DeleteLastTranslation(token);
                await SQLiteHistoryLogger.LogTranslation(originalText, translatedText, targetLanguage, apiName);
                TranslationLogged?.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                SnackbarHost.Show("[ERROR] Logging history failed.", ex.Message, SnackbarType.Error,
                    timeout: 2, closeButton: true);
            }
        }

        public static async Task LogOnly(string originalText,
            bool isOverwrite = false, CancellationToken token = default)
        {
            try
            {
                if (isOverwrite)
                    await SQLiteHistoryLogger.DeleteLastTranslation(token);
                await SQLiteHistoryLogger.LogTranslation(originalText, "N/A", "N/A", "LogOnly");
                TranslationLogged?.Invoke();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                SnackbarHost.Show("[ERROR] Logging history failed.", ex.Message, SnackbarType.Error,
                    timeout: 2, closeButton: true);
            }
        }

        public static async Task AddContexts(CancellationToken token = default)
        {
            var lastLog = await SQLiteHistoryLogger.LoadLastTranslation(token);
            if (lastLog == null)
                return;

            if (Caption?.Contexts.Count >= Caption.MAX_CONTEXTS)
                Caption.Contexts.Dequeue();
            Caption?.Contexts.Enqueue(lastLog);

            Caption?.OnPropertyChanged("DisplayLogCards");
            Caption?.OnPropertyChanged("OverlayPreviousTranslation");
        }

        public static void ClearContexts()
        {
            Caption?.Contexts.Clear();

            Caption?.OnPropertyChanged("DisplayLogCards");
            Caption?.OnPropertyChanged("OverlayPreviousTranslation");
        }

        // If this text is too similar to the last one, overwrite it when logging.
        public static async Task<bool> IsOverwrite(string originalText, CancellationToken token = default)
        {
            string lastOriginalText = await SQLiteHistoryLogger.LoadLastSourceText(token);
            if (lastOriginalText == null)
                return false;

            int minLen = Math.Min(originalText.Length, lastOriginalText.Length);
            originalText = originalText.Substring(0, minLen);
            lastOriginalText = lastOriginalText.Substring(0, minLen);

            double similarity = TextUtil.Similarity(originalText, lastOriginalText);
            return similarity > TextUtil.SIM_THRESHOLD;
        }
    }
}
