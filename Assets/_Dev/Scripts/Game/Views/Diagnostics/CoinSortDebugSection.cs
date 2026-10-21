#if MB_DEBUG_FPS
using Game.Game.Services;
using UnityEngine;

namespace Game.Game.Views.Diagnostics
{
    // DebugConsole's Coin Sort block, shown only where a CoinSortSession runs (MainGame): picks a coin
    // number and switches the forced deal on or off. While on, every Deal gives one slot a group of
    // that number (DealRules); the session keeps it across games until it is switched off.
    sealed class CoinSortDebugSection
    {
        const int MaxTier = 999;

        int tier = 1;
        string input = "1";

        public void Draw(CoinSortSession session, float unit, GUIStyle labelStyle, GUIStyle buttonStyle, GUIStyle fieldStyle)
        {
            GUILayout.Space(unit * 0.3f);
            var highest = session.Board != null ? session.Board.MaxCoin.ToString() : "--";
            GUILayout.Label($"DEAL TIER  (highest {highest})", labelStyle);

            var forced = session.ForcedDealNumber > 0;
            if (forced && session.ForcedDealNumber != tier) SetTier(session, session.ForcedDealNumber);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-1", buttonStyle, GUILayout.Height(unit))) SetTier(session, tier - 1);
            // The text is kept apart from the tier so the field can be cleared before typing a new one.
            input = GUILayout.TextField(input, 3, fieldStyle, GUILayout.Height(unit));
            if (int.TryParse(input, out var typed) && typed != tier) SetTier(session, typed, typed < 1 || typed > MaxTier);
            if (GUILayout.Button("+1", buttonStyle, GUILayout.Height(unit))) SetTier(session, tier + 1);
            GUILayout.EndHorizontal();

            var caption = forced ? $"FORCE DEAL: ON ({tier})" : "FORCE DEAL: OFF";
            if (GUILayout.Button(caption, buttonStyle, GUILayout.Height(unit)))
            {
                session.ForcedDealNumber = forced ? 0 : tier;
            }
        }

        void SetTier(CoinSortSession session, int value, bool showValue = true)
        {
            tier = Mathf.Clamp(value, 1, MaxTier);
            if (showValue) input = tier.ToString();
            if (session.ForcedDealNumber > 0) session.ForcedDealNumber = tier;
        }
    }
}
#endif
