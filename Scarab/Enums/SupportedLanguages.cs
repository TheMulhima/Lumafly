using System.Collections.Generic;

namespace Scarab.Enums;

public enum SupportedLanguages
{
    en,
    pt,
    zh,
    es,
    fr
}

public static class SupportedLanguagesInfo
{
    public static readonly Dictionary<SupportedLanguages, string> SupportedLangToCulture = new()
    {
        { SupportedLanguages.en, "en-US" },
        { SupportedLanguages.es, "es-ES" },
        { SupportedLanguages.pt, "pt-BR" },
        { SupportedLanguages.fr, "fr-FR" },
        { SupportedLanguages.zh, "zh-CN" }
    };
}