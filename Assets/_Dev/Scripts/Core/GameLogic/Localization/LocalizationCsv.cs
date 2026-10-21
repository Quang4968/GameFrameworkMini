using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Game.Core.GameLogic.Localization
{
    // Turns the localization sheet (one tab: key | note | <lang> | <lang> ...) into one key -> value
    // table per language column, plus the problems a translator should fix before shipping.
    public static class LocalizationCsv
    {
        const string KeyColumn = "key";

        static readonly Regex Placeholder = new Regex(@"\{(\d+)[^}]*\}", RegexOptions.Compiled);

        // Columns named "note"/"comment", or starting with '#', are for humans; every other header
        // after "key" is a language code. Rows whose key starts with '#' are section separators.
        public static LocalizationSheet Parse(string csv, string sourceLanguage)
        {
            var sheet = new LocalizationSheet();
            var rows = ReadRows(csv ?? string.Empty);
            if (rows.Count == 0)
            {
                sheet.Issues.Add("The sheet is empty.");
                return sheet;
            }

            var header = rows[0];
            if (header.Count == 0 || !string.Equals(header[0].Trim(), KeyColumn, StringComparison.OrdinalIgnoreCase))
            {
                sheet.Issues.Add("The first column header must be \"key\".");
                return sheet;
            }

            var columns = new List<int>();
            for (var c = 1; c < header.Count; c++)
            {
                var name = header[c].Trim();
                if (IsHumanColumn(name)) continue;
                columns.Add(c);
                sheet.AddLanguage(name);
            }

            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var r = 1; r < rows.Count; r++)
            {
                var row = rows[r];
                var key = row.Count > 0 ? row[0].Trim() : string.Empty;
                if (key.Length == 0 || key[0] == '#') continue;

                var line = r + 1;
                if (seen.TryGetValue(key, out var first))
                {
                    sheet.Issues.Add($"Row {line}: duplicate key \"{key}\" (first on row {first}); kept the first.");
                    continue;
                }
                seen[key] = line;

                foreach (var c in columns)
                {
                    var value = c < row.Count ? row[c] : string.Empty;
                    sheet.Add(header[c].Trim(), key, value);
                }
            }

            Validate(sheet, sourceLanguage);
            return sheet;
        }

        // Highest placeholder index + 1, so "{0} of {2}" needs three arguments.
        public static int PlaceholderCount(string value)
        {
            var count = 0;
            if (string.IsNullOrEmpty(value)) return count;
            foreach (Match match in Placeholder.Matches(value))
                count = Math.Max(count, int.Parse(match.Groups[1].Value) + 1);
            return count;
        }

        static void Validate(LocalizationSheet sheet, string sourceLanguage)
        {
            var source = sheet.Table(sourceLanguage);
            if (source == null)
            {
                sheet.Issues.Add($"No column for the source language \"{sourceLanguage}\".");
                return;
            }

            foreach (var language in sheet.Languages)
            {
                var table = sheet.Table(language);
                var missing = 0;
                foreach (var pair in source)
                {
                    table.TryGetValue(pair.Key, out var value);
                    if (string.IsNullOrEmpty(value))
                    {
                        missing++;
                        continue;
                    }

                    if (language != sourceLanguage && PlaceholderCount(value) != PlaceholderCount(pair.Value))
                        sheet.Issues.Add($"{language} \"{pair.Key}\": placeholders differ from {sourceLanguage}.");
                }

                if (missing > 0) sheet.Issues.Add($"{language}: {missing} key(s) without a value.");
            }
        }

        static bool IsHumanColumn(string name) =>
            name.Length == 0 || name[0] == '#' ||
            string.Equals(name, "note", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(name, "comment", StringComparison.OrdinalIgnoreCase);

        // RFC 4180: quoted cells may hold commas, doubled quotes and line breaks.
        static List<List<string>> ReadRows(string csv)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var cell = new StringBuilder();
            var quoted = false;

            for (var i = 0; i < csv.Length; i++)
            {
                var ch = csv[i];
                if (quoted)
                {
                    if (ch != '"') cell.Append(ch);
                    else if (i + 1 < csv.Length && csv[i + 1] == '"') cell.Append(csv[++i]);
                    else quoted = false;
                    continue;
                }

                switch (ch)
                {
                    case '"': quoted = true; break;
                    case ',': row.Add(cell.ToString()); cell.Clear(); break;
                    case '\r': break;
                    case '\n': EndRow(rows, row, cell); row = new List<string>(); break;
                    default: cell.Append(ch); break;
                }
            }

            EndRow(rows, row, cell);
            return rows;
        }

        static void EndRow(List<List<string>> rows, List<string> row, StringBuilder cell)
        {
            row.Add(cell.ToString());
            cell.Clear();
            if (row.Count > 1 || row[0].Length > 0) rows.Add(row);
        }
    }
}
