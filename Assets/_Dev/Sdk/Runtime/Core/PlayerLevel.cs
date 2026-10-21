using System;

namespace MoodBoost.Sdk
{
    public sealed class PlayerLevel : IPlayerLevel
    {
        const string Tag = "PlayerLevel";

        Func<int> provider;
        bool warnedMissing;

        public void SetProvider(Func<int> currentLevel)
        {
            provider = currentLevel;
        }

        public bool TryGetCurrent(out int level)
        {
            level = 0;

            if (provider == null)
            {
                // Forgetting SetProvider silently disables every level gate, so say it once.
                if (!warnedMissing)
                {
                    SdkLog.Warn(Tag, "No level provider set - level-gated ad rules are skipped. " +
                        "Call IPlayerLevel.SetProvider at boot.");
                    warnedMissing = true;
                }

                return false;
            }

            try
            {
                level = provider();
                return true;
            }
            catch (Exception error)
            {
                // A bug in the game's function must not take the ad layer down with it.
                SdkLog.Exception(Tag, error);
                return false;
            }
        }
    }
}
