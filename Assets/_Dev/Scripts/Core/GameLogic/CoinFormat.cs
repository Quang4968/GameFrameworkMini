using System.Globalization;

namespace Game.Core.GameLogic
{
    // How a coin amount is printed in a fixed-width slot: 1000 -> "1K", 1500 -> "1.5K",
    // 1000000 -> "1M". Lives in Core so the HUD counter and every price label shorten the same way.
    public static class CoinFormat
    {
        const int Thousand = 1000;

        static readonly int[] Steps = { 1000000000, 1000000, 1000 };
        static readonly string[] Suffixes = { "B", "M", "K" };

        // Truncates instead of rounding: 999999 prints as "999K", never as the "1M" the player does
        // not own yet. The decimal is dropped past 100 units to keep the label short.
        public static string Short(int coins)
        {
            if (coins > -Thousand && coins < Thousand) return coins.ToString(CultureInfo.InvariantCulture);

            var sign = coins < 0 ? "-" : string.Empty;
            var value = coins < 0 ? -(long)coins : coins;

            for (var i = 0; i < Steps.Length; i++)
            {
                var step = Steps[i];
                if (value < step) continue;

                var whole = value / step;
                var tenth = whole < 100 ? value % step / (step / 10) : 0;
                return tenth == 0
                    ? string.Concat(sign, whole.ToString(CultureInfo.InvariantCulture), Suffixes[i])
                    : string.Concat(sign, whole.ToString(CultureInfo.InvariantCulture), ".",
                        tenth.ToString(CultureInfo.InvariantCulture), Suffixes[i]);
            }

            return coins.ToString(CultureInfo.InvariantCulture);
        }
    }
}
