using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator.models
{
    public class Caption : INotifyPropertyChanged
    {
        public const int MAX_CONTEXTS = 10;

        private static Caption? instance = null;
        public event PropertyChangedEventHandler? PropertyChanged;

        private string displayOriginalCaption = string.Empty;
        private string displayTranslatedCaption = string.Empty;
        private string overlayOriginalCaption = " ";
        private string overlayCurrentTranslation = " ";
        private string overlayNoticePrefix = " ";

        public string OriginalCaption { get; set; } = string.Empty;
        public string TranslatedCaption { get; set; } = string.Empty;

        public Queue<TranslationHistoryEntry> Contexts { get; } = new(MAX_CONTEXTS);

        public TranscriptBuffer OriginalTranscript { get; } = new();
        public TranscriptBuffer TranslatedTranscript { get; } = new();
        public IEnumerable<TranscriptLine> OriginalTranscriptLines => OriginalTranscript.Lines;
        public IEnumerable<TranscriptLine> TranslatedTranscriptLines => TranslatedTranscript.Lines;
        public string LiveOriginalText => OriginalTranscript.CombinedText;
        public string LiveTranslatedText => TranslatedTranscript.CombinedText;

        public IEnumerable<TranslationHistoryEntry> AwareContexts => GetPreviousContexts(Translator.Setting.NumContexts);
        public string AwareContextsCaption => GetPreviousText(Translator.Setting.NumContexts, TextType.Caption);

        public IEnumerable<TranslationHistoryEntry> DisplayLogCards =>
            GetPreviousContexts(MAX_CONTEXTS).Reverse();

        public string DisplayOriginalCaption
        {
            get => displayOriginalCaption;
            set
            {
                displayOriginalCaption = value;
                OnPropertyChanged("DisplayOriginalCaption");
            }
        }
        public string DisplayTranslatedCaption
        {
            get => displayTranslatedCaption;
            set
            {
                displayTranslatedCaption = value;
                OnPropertyChanged("DisplayTranslatedCaption");
            }
        }

        public string OverlayOriginalCaption
        {
            get => overlayOriginalCaption;
            set
            {
                overlayOriginalCaption = value;
                OnPropertyChanged("OverlayOriginalCaption");
            }
        }
        public string OverlayNoticePrefix
        {
            get => overlayNoticePrefix;
            set
            {
                overlayNoticePrefix = value;
                OnPropertyChanged("OverlayNoticePrefix");
            }
        }
        public string OverlayCurrentTranslation
        {
            get => overlayCurrentTranslation;
            set
            {
                overlayCurrentTranslation = value;
                OnPropertyChanged("OverlayCurrentTranslation");
            }
        }

        public string OverlayPreviousTranslation => TranslatedTranscript.FinalText;

        private Caption()
        {
            OriginalTranscript.Changed += (_, _) =>
            {
                OnPropertyChanged(nameof(OriginalTranscriptLines));
                OnPropertyChanged(nameof(LiveOriginalText));
                OnPropertyChanged(nameof(OverlayOriginalCaption));
            };
            TranslatedTranscript.Changed += (_, _) =>
            {
                OnPropertyChanged(nameof(TranslatedTranscriptLines));
                OnPropertyChanged(nameof(LiveTranslatedText));
                OnPropertyChanged(nameof(OverlayPreviousTranslation));
                OnPropertyChanged(nameof(OverlayCurrentTranslation));
            };
        }

        public static Caption GetInstance()
        {
            if (instance != null)
                return instance;
            instance = new Caption();
            return instance;
        }

        public string GetPreviousText(int count, TextType textType)
        {
            if (count <= 0 || Contexts.Count == 0)
                return string.Empty;

            List<string> previousTexts = Contexts
                .Reverse().Take(count).Reverse()
                .Select(entry => entry == null || string.CompareOrdinal(entry.TranslatedText, "N/A") == 0 ||
                                 entry.TranslatedText.Contains("[ERROR]") || entry.TranslatedText.Contains("[WARNING]") ?
                    "" : (textType == TextType.Caption ? entry.SourceText : entry.TranslatedText))
                .Where(text => !string.IsNullOrWhiteSpace(text))
                .ToList();
            if (previousTexts.Count == 0)
                return string.Empty;

            string prev = previousTexts.Aggregate((accu, cur) =>
                {
                    if (!string.IsNullOrEmpty(accu))
                    {
                        if (Array.IndexOf(TextUtil.PUNC_EOS, accu[^1]) == -1)
                            accu += TextUtil.isCJChar(accu[^1]) ? "。" : ". ";
                        else
                            accu += TextUtil.isCJChar(accu[^1]) ? "" : " ";
                    }
                    cur = RegexPatterns.NoticePrefix().Replace(cur, "");
                    return accu + cur;
                });

            if (textType == TextType.Translation)
                prev = RegexPatterns.NoticePrefix().Replace(prev, "");
            if (!string.IsNullOrEmpty(prev) && Array.IndexOf(TextUtil.PUNC_EOS, prev[^1]) == -1)
                prev += TextUtil.isCJChar(prev[^1]) ? "。" : ".";
            if (!string.IsNullOrEmpty(prev) && Encoding.UTF8.GetByteCount(prev[^1].ToString()) < 2)
                prev += " ";
            return prev;
        }

        public IEnumerable<TranslationHistoryEntry> GetPreviousContexts(int count)
        {
            if (count <= 0 || Contexts.Count == 0)
                return [];

            return Contexts
                .Reverse().Take(count).Reverse()
                .Where(entry => entry != null && string.CompareOrdinal(entry.TranslatedText, "N/A") != 0 &&
                                !entry.TranslatedText.Contains("[ERROR]") &&
                                !entry.TranslatedText.Contains("[WARNING]"));
        }

        public void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public enum TextType
    {
        Caption,
        Translation
    }
}
