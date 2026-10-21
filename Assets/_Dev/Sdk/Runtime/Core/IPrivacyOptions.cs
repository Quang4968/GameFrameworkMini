using System;

namespace MoodBoost.Sdk
{
    // The door for the player to REOPEN the consent form after the first time.
    //
    // This is a hard requirement from Google when the consent form is the "privacy options" kind: a player in
    // the EU must be able to withdraw what they consented to, and the only way in is a button in the game's
    // Settings. Missing that button gets the app rejected at review, not merely marked as missing a feature.
    //
    // Separate from IConsentProvider because they are two different things: IConsentProvider is the STATE the ad
    // networks read at init, this is the ACTION of opening a UI, and the default implementation has no UI to open.
    public interface IPrivacyOptions
    {
        // When false, hide the button: this region's form has no options to change, so showing the button leads
        // to a tap that does nothing. Only accurate AFTER SdkRuntime has initialized — before that it is always false.
        bool IsRequired { get; }

        // Open the form. The callback receives null on a normal close, or an error description when it cannot open.
        // New consent takes effect on the NEXT APP LAUNCH: the ad networks have already initialized with the old flags.
        void Show(Action<string> onClosed);
    }
}
