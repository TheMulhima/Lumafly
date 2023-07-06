using System.Collections.Generic;

namespace Scarab.Enums;

public enum SupportedLanguages
{
    en,
    es,
    pt,
    fr,
    zh,
    ru,
}

public static class SupportedLanguagesInfo
{
    public static readonly Dictionary<SupportedLanguages, string> SupportedLangToCulture = new()
    {
        { SupportedLanguages.en, "en-US" },
        { SupportedLanguages.es, "es-ES" },
        { SupportedLanguages.pt, "pt-BR" },
        { SupportedLanguages.fr, "fr-FR" },
        { SupportedLanguages.zh, "zh-CN" },
        { SupportedLanguages.ru, "ru-RU" },
    };

    public static readonly Dictionary<SupportedLanguages, string> LocalizedLanguageOptions = new()
    {
        { SupportedLanguages.en, "English" },
        { SupportedLanguages.es, "Español" },
        { SupportedLanguages.pt, "Português" },
        { SupportedLanguages.fr, "Français" },
        { SupportedLanguages.zh, "中文" },
        { SupportedLanguages.ru, "Русский" },
    };
}
