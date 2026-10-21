using System;

namespace MoodBoost.Sdk
{
    // The default when no consent module is enabled: there is no form to open, so the button in Settings hides
    // itself.
    public sealed class NullPrivacyOptions : IPrivacyOptions
    {
        public bool IsRequired => false;

        public void Show(Action<string> onClosed) => onClosed?.Invoke("No consent module is enabled.");
    }
}
