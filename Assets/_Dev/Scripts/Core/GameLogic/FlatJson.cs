using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Game.Core.GameLogic
{
    // Reads a one-level JSON object of integer values, {"coins":2000,"shuffle":2}, the shape
    // remote config uses for reward tables keyed by resource name. Anything else fails the parse.
    public static class FlatJson
    {
        static readonly Regex Pair = new Regex("\"([^\"]+)\"\\s*:\\s*(-?\\d+)", RegexOptions.Compiled);

        public static bool TryParseInts(string json, out Dictionary<string, int> values)
        {
            values = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            var trimmed = json.Trim();
            if (trimmed.Length < 2 || trimmed[0] != '{' || trimmed[trimmed.Length - 1] != '}') return false;

            values = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Match match in Pair.Matches(trimmed))
            {
                if (int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    values[match.Groups[1].Value] = value;
            }

            return true;
        }
    }
}
