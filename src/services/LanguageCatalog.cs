using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.services
{
    /// <summary>
    /// Stable, offline language catalog. Provider discovery may filter this list but never replaces it,
    /// so a network failure cannot empty either language selector.
    /// </summary>
    public static class LanguageCatalog
    {
        private static readonly IReadOnlyList<LanguageDefinition> languages = BuildCatalog();
        private static readonly IReadOnlyDictionary<string, LanguageDefinition> byCode = languages
            .ToDictionary(language => language.CanonicalCode, StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyList<LanguageDefinition> All => languages;

        public static LanguageDefinition? Find(string? canonicalOrProviderCode)
        {
            if (string.IsNullOrWhiteSpace(canonicalOrProviderCode))
                return null;

            if (byCode.TryGetValue(canonicalOrProviderCode, out LanguageDefinition? exact))
                return exact;

            return languages.FirstOrDefault(language =>
                string.Equals(language.TranslationCode, canonicalOrProviderCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(language.SpeechLocale, canonicalOrProviderCode, StringComparison.OrdinalIgnoreCase));
        }

        public static string GetTranslationCode(string? canonicalCode, string providerId)
        {
            LanguageDefinition? language = Find(canonicalCode);
            return language?.GetProviderCode(providerId, speech: false) ?? canonicalCode ?? string.Empty;
        }

        public static string GetSpeechLocale(string? canonicalCode, string providerId)
        {
            LanguageDefinition? language = Find(canonicalCode);
            return language?.GetProviderCode(providerId, speech: true) ?? canonicalCode ?? string.Empty;
        }

        public static bool IsKnown(string? code) => Find(code) != null;

        private static IReadOnlyList<LanguageDefinition> BuildCatalog()
        {
            static LanguageDefinition L(
                string canonical,
                string name,
                string native,
                string translation,
                string speech,
                bool rtl = false,
                IReadOnlyDictionary<string, string>? providerCodes = null) =>
                new(canonical, name, native, translation, speech, rtl, providerCodes);

            static IReadOnlyDictionary<string, string> Codes(params (string Provider, string Code)[] entries) =>
                entries.ToDictionary(entry => entry.Provider, entry => entry.Code, StringComparer.OrdinalIgnoreCase);

            return new List<LanguageDefinition>
            {
                L("af-ZA", "Afrikaans (South Africa)", "Afrikaans", "af", "af-ZA"),
                L("sq-AL", "Albanian (Albania)", "Shqip", "sq", "sq-AL"),
                L("am-ET", "Amharic (Ethiopia)", "አማርኛ", "am", "am-ET"),
                L("ar-SA", "Arabic (Saudi Arabia)", "العربية", "ar", "ar-SA", true,
                    Codes(("Baidu", "ara"))),
                L("hy-AM", "Armenian (Armenia)", "Հայերեն", "hy", "hy-AM"),
                L("az-AZ", "Azerbaijani (Azerbaijan)", "Azərbaycanca", "az", "az-AZ"),
                L("eu-ES", "Basque (Spain)", "Euskara", "eu", "eu-ES"),
                L("bn-BD", "Bengali (Bangladesh)", "বাংলা", "bn", "bn-BD"),
                L("bs-BA", "Bosnian (Bosnia and Herzegovina)", "Bosanski", "bs", "bs-BA"),
                L("bg-BG", "Bulgarian (Bulgaria)", "Български", "bg", "bg-BG"),
                L("my-MM", "Burmese (Myanmar)", "မြန်မာ", "my", "my-MM"),
                L("ca-ES", "Catalan (Spain)", "Català", "ca", "ca-ES"),
                L("zh-CN", "Chinese (Simplified)", "简体中文", "zh-CN", "zh-CN", false,
                    Codes(("MicrosoftTranslator", "zh-Hans"), ("DeepL", "ZH-HANS"), ("Baidu", "zh"),
                          ("MTranServer", "zh"), ("LibreTranslate", "zh"))),
                L("zh-TW", "Chinese (Traditional)", "繁體中文", "zh-TW", "zh-TW", false,
                    Codes(("MicrosoftTranslator", "zh-Hant"), ("DeepL", "ZH-HANT"), ("Baidu", "cht"),
                          ("MTranServer", "zh"), ("LibreTranslate", "zh"))),
                L("hr-HR", "Croatian (Croatia)", "Hrvatski", "hr", "hr-HR"),
                L("cs-CZ", "Czech (Czechia)", "Čeština", "cs", "cs-CZ"),
                L("da-DK", "Danish (Denmark)", "Dansk", "da", "da-DK"),
                L("nl-NL", "Dutch (Netherlands)", "Nederlands", "nl", "nl-NL"),
                L("en-US", "English (United States)", "English", "en", "en-US", false,
                    Codes(("DeepL", "EN-US"))),
                L("en-GB", "English (United Kingdom)", "English", "en", "en-GB", false,
                    Codes(("DeepL", "EN-GB"))),
                L("et-EE", "Estonian (Estonia)", "Eesti", "et", "et-EE"),
                L("fil-PH", "Filipino (Philippines)", "Filipino", "fil", "fil-PH"),
                L("fi-FI", "Finnish (Finland)", "Suomi", "fi", "fi-FI"),
                L("fr-FR", "French (France)", "Français", "fr", "fr-FR", false,
                    Codes(("Baidu", "fra"))),
                L("fr-CA", "French (Canada)", "Français canadien", "fr", "fr-CA"),
                L("gl-ES", "Galician (Spain)", "Galego", "gl", "gl-ES"),
                L("ka-GE", "Georgian (Georgia)", "ქართული", "ka", "ka-GE"),
                L("de-DE", "German (Germany)", "Deutsch", "de", "de-DE"),
                L("el-GR", "Greek (Greece)", "Ελληνικά", "el", "el-GR"),
                L("gu-IN", "Gujarati (India)", "ગુજરાતી", "gu", "gu-IN"),
                L("ht-HT", "Haitian Creole (Haiti)", "Kreyòl ayisyen", "ht", "ht-HT"),
                L("ha-NG", "Hausa (Nigeria)", "Hausa", "ha", "ha-NG"),
                L("he-IL", "Hebrew (Israel)", "עברית", "he", "he-IL", true),
                L("hi-IN", "Hindi (India)", "हिन्दी", "hi", "hi-IN"),
                L("hu-HU", "Hungarian (Hungary)", "Magyar", "hu", "hu-HU"),
                L("is-IS", "Icelandic (Iceland)", "Íslenska", "is", "is-IS"),
                L("ig-NG", "Igbo (Nigeria)", "Igbo", "ig", "ig-NG"),
                L("id-ID", "Indonesian (Indonesia)", "Bahasa Indonesia", "id", "id-ID"),
                L("ga-IE", "Irish (Ireland)", "Gaeilge", "ga", "ga-IE"),
                L("it-IT", "Italian (Italy)", "Italiano", "it", "it-IT"),
                L("ja-JP", "Japanese (Japan)", "日本語", "ja", "ja-JP", false,
                    Codes(("Baidu", "jp"))),
                L("jv-ID", "Javanese (Indonesia)", "Basa Jawa", "jv", "jv-ID"),
                L("kn-IN", "Kannada (India)", "ಕನ್ನಡ", "kn", "kn-IN"),
                L("kk-KZ", "Kazakh (Kazakhstan)", "Қазақша", "kk", "kk-KZ"),
                L("km-KH", "Khmer (Cambodia)", "ខ្មែរ", "km", "km-KH"),
                L("ko-KR", "Korean (Korea)", "한국어", "ko", "ko-KR", false,
                    Codes(("Baidu", "kor"))),
                L("ku-TR", "Kurdish (Türkiye)", "Kurdî", "ku", "ku-TR"),
                L("lo-LA", "Lao (Laos)", "ລາວ", "lo", "lo-LA"),
                L("lv-LV", "Latvian (Latvia)", "Latviešu", "lv", "lv-LV"),
                L("lt-LT", "Lithuanian (Lithuania)", "Lietuvių", "lt", "lt-LT"),
                L("mk-MK", "Macedonian (North Macedonia)", "Македонски", "mk", "mk-MK"),
                L("ms-MY", "Malay (Malaysia)", "Bahasa Melayu", "ms", "ms-MY"),
                L("ml-IN", "Malayalam (India)", "മലയാളം", "ml", "ml-IN"),
                L("mt-MT", "Maltese (Malta)", "Malti", "mt", "mt-MT"),
                L("mr-IN", "Marathi (India)", "मराठी", "mr", "mr-IN"),
                L("mn-MN", "Mongolian (Mongolia)", "Монгол", "mn", "mn-MN"),
                L("ne-NP", "Nepali (Nepal)", "नेपाली", "ne", "ne-NP"),
                L("nb-NO", "Norwegian Bokmål (Norway)", "Norsk bokmål", "no", "nb-NO"),
                L("ps-AF", "Pashto (Afghanistan)", "پښتو", "ps", "ps-AF", true),
                L("fa-IR", "Persian (Iran)", "فارسی", "fa", "fa-IR", true),
                L("pl-PL", "Polish (Poland)", "Polski", "pl", "pl-PL"),
                L("pt-BR", "Portuguese (Brazil)", "Português (Brasil)", "pt-BR", "pt-BR", false,
                    Codes(("GoogleCloudTranslation", "pt"), ("TranslatePlus", "pt"), ("Langbly", "pt"),
                          ("DeepL", "PT-BR"), ("MTranServer", "pt"), ("LibreTranslate", "pt"))),
                L("pt-PT", "Portuguese (Portugal)", "Português (Portugal)", "pt-PT", "pt-PT", false,
                    Codes(("GoogleCloudTranslation", "pt"), ("TranslatePlus", "pt"), ("Langbly", "pt"),
                          ("DeepL", "PT-PT"), ("MTranServer", "pt"), ("LibreTranslate", "pt"))),
                L("pa-IN", "Punjabi (India)", "ਪੰਜਾਬੀ", "pa", "pa-IN"),
                L("ro-RO", "Romanian (Romania)", "Română", "ro", "ro-RO"),
                L("ru-RU", "Russian (Russia)", "Русский", "ru", "ru-RU"),
                L("sr-RS", "Serbian (Serbia)", "Српски", "sr", "sr-RS"),
                L("si-LK", "Sinhala (Sri Lanka)", "සිංහල", "si", "si-LK"),
                L("sk-SK", "Slovak (Slovakia)", "Slovenčina", "sk", "sk-SK"),
                L("sl-SI", "Slovenian (Slovenia)", "Slovenščina", "sl", "sl-SI"),
                L("so-SO", "Somali (Somalia)", "Soomaali", "so", "so-SO"),
                L("es-ES", "Spanish (Spain)", "Español (España)", "es", "es-ES", false,
                    Codes(("Baidu", "spa"))),
                L("es-MX", "Spanish (Mexico)", "Español (México)", "es", "es-MX", false,
                    Codes(("Baidu", "spa"))),
                L("es-US", "Spanish (United States)", "Español (Estados Unidos)", "es", "es-US", false,
                    Codes(("Baidu", "spa"))),
                L("sw-KE", "Swahili (Kenya)", "Kiswahili", "sw", "sw-KE"),
                L("sv-SE", "Swedish (Sweden)", "Svenska", "sv", "sv-SE"),
                L("ta-IN", "Tamil (India)", "தமிழ்", "ta", "ta-IN"),
                L("te-IN", "Telugu (India)", "తెలుగు", "te", "te-IN"),
                L("th-TH", "Thai (Thailand)", "ไทย", "th", "th-TH"),
                L("tr-TR", "Turkish (Türkiye)", "Türkçe", "tr", "tr-TR"),
                L("uk-UA", "Ukrainian (Ukraine)", "Українська", "uk", "uk-UA"),
                L("ur-PK", "Urdu (Pakistan)", "اردو", "ur", "ur-PK", true),
                L("uz-UZ", "Uzbek (Uzbekistan)", "Oʻzbekcha", "uz", "uz-UZ"),
                L("vi-VN", "Vietnamese (Vietnam)", "Tiếng Việt", "vi", "vi-VN"),
                L("cy-GB", "Welsh (United Kingdom)", "Cymraeg", "cy", "cy-GB"),
                L("yo-NG", "Yoruba (Nigeria)", "Yorùbá", "yo", "yo-NG"),
                L("zu-ZA", "Zulu (South Africa)", "isiZulu", "zu", "zu-ZA")
            }
            .OrderBy(language => language.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        }
    }
}
