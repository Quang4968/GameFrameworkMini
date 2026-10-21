using System.Text;

namespace Game.Core.GameLogic
{
    // What counts as a usable display name. It lives in Core so the profile popup, the HUD and
    // anything reading the name later all cut it the same way.
    public static class PlayerNameRules
    {
        public const int MaxLength = 14;

        // Shown to a player who never typed a name, and to one who cleared the field.
        public const string FallbackName = "Player";

        // Trims and collapses whitespace runs, so "  a   b  " comes back as "a b". A name left
        // empty falls back instead of returning "" — every screen printing it expects something.
        public static string Sanitize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return FallbackName;

            var builder = new StringBuilder(raw.Length);
            var pendingSpace = false;

            foreach (var c in raw)
            {
                if (char.IsWhiteSpace(c) || char.IsControl(c))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }

                // A separator only earns its slot when the character after it fits too, so a name
                // cut at the limit never ends on a space.
                if (pendingSpace)
                {
                    if (builder.Length + 1 >= MaxLength) break;
                    builder.Append(' ');
                    pendingSpace = false;
                }

                if (builder.Length >= MaxLength) break;
                builder.Append(c);
            }

            return builder.Length == 0 ? FallbackName : builder.ToString();
        }
    }
}
