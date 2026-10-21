using System.Globalization;
using System.Text;

namespace MoodBoost.Sdk.Tracking
{
    // Names of the MMP plan's events, sent only through IAttributionService.LogEvent. A separate sheet from the analytics
    // plan in TrackingEvents, and just as permanent: the UA team maps ad-network postbacks to these exact names.
    //
    // Ad revenue and purchases are not here - they reach the MMP as an impression and a validated transaction instead.
    public static class MmpEvents
    {
        public const string FirstPurchase = "first_purchase";

        // The revenue-carrying purchase event of MMPs that need a token for it (Adjust). Tenjin sends a transaction
        // instead and never looks this name up.
        public const string Purchase = "purchase";

        const string HowToPlayTutorial = "HowToPlay";
        const string LevelAchievedPrefix = "level_achieved_";
        const string TutorialCompletePrefix = "tutorial_complete_";

        public static bool TryLevelAchieved(int level, out string eventName)
        {
            switch (level)
            {
                case 1:
                case 5:
                case 10:
                case 20:
                case 30:
                case 50:
                case 70:
                case 100:
                    eventName = LevelAchievedPrefix + level.ToString(CultureInfo.InvariantCulture);
                    return true;
                default:
                    eventName = string.Empty;
                    return false;
            }
        }

        // Level 1 is already represented by level_achieved_1. Booster lessons keep distinct names because Tenjin drops
        // string parameters, so lessons sharing one name could never be told apart.
        public static bool TryTutorialComplete(string tutorial, out string eventName)
        {
            if (string.IsNullOrEmpty(tutorial) || tutorial == HowToPlayTutorial)
            {
                eventName = string.Empty;
                return false;
            }

            var name = new StringBuilder(TutorialCompletePrefix);

            for (var i = 0; i < tutorial.Length; i++)
            {
                var c = tutorial[i];

                if (char.IsUpper(c) && i > 0 && !char.IsUpper(tutorial[i - 1]) && tutorial[i - 1] != '_')
                {
                    name.Append('_');
                }

                name.Append(char.ToLowerInvariant(c));
            }

            eventName = name.ToString().TrimEnd('_');
            return true;
        }
    }
}
