using System;
using System.Collections.Generic;

namespace Game.Core.Services
{
    // The displayed language and its key -> value table. Languages are string codes ("en", "pt-BR"),
    // so a new language is data only.
    public interface ILocalizationService
    {
        string CurrentLanguage { get; }

        // In settings order; the language selector cycles through it.
        IReadOnlyList<string> Languages { get; }

        // Raised with the new code, only when it actually changes.
        event Action<string> LanguageChanged;

        // Current language -> source language -> the key itself, so a missing key shows on screen.
        string Get(string key);

        // Falls back to the unformatted value when the placeholders do not match the arguments.
        string Format(string key, params object[] args);

        bool Has(string key);

        // Unknown codes come back unchanged.
        string DisplayName(string language);

        // Codes without a table are ignored.
        void SetLanguage(string language);
    }
}
