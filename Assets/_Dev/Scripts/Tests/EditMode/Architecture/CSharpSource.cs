using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Core.Tests.Architecture
{
    // A project source file, read once per test run, with comments and string literals blanked so that prose such as
    // "never call PoolBoss.X" in a header comment does not count as a violation. Line numbers are preserved.
    public sealed class CSharpSource
    {
        public readonly string Path;
        public readonly int LineCount;

        readonly string[] codeLines;

        static List<CSharpSource> cache;

        CSharpSource(string path, string text)
        {
            Path = path;
            LineCount = CountLines(text);
            codeLines = StripCommentsAndStrings(text).Split('\n');
        }

        public static IReadOnlyList<CSharpSource> All => cache ??= Load();

        public List<int> LinesMatching(Regex pattern)
        {
            var hits = new List<int>();
            for (var i = 0; i < codeLines.Length; i++)
            {
                if (pattern.IsMatch(codeLines[i])) hits.Add(i + 1);
            }
            return hits;
        }

        static List<CSharpSource> Load()
        {
            var projectRoot = Directory.GetParent(Application.dataPath).FullName;
            var root = System.IO.Path.Combine(projectRoot, ArchitectureConfig.ScriptsRoot);

            return Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Select(file => new CSharpSource(
                    System.IO.Path.GetRelativePath(root, file).Replace('\\', '/'),
                    File.ReadAllText(file)))
                .OrderBy(source => source.Path, StringComparer.Ordinal)
                .ToList();
        }

        static int CountLines(string text)
        {
            if (text.Length == 0) return 0;
            var count = text.Count(c => c == '\n');
            return text[text.Length - 1] == '\n' ? count : count + 1;
        }

        static string StripCommentsAndStrings(string src)
        {
            var output = new StringBuilder(src.Length);
            var n = src.Length;
            var i = 0;

            while (i < n)
            {
                var c = src[i];
                var d = i + 1 < n ? src[i + 1] : '\0';

                if (c == '/' && d == '/')
                {
                    var end = src.IndexOf('\n', i);
                    i = end < 0 ? n : end;
                }
                else if (c == '/' && d == '*')
                {
                    var end = src.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    end = end < 0 ? n : end + 2;
                    AppendNewlines(output, src, i, end);
                    i = end;
                }
                else if ((c == '@' && d == '"') || (c == '$' && d == '@') || (c == '@' && d == '$'))
                {
                    var j = d == '"' ? i + 2 : i + 3;
                    while (j < n)
                    {
                        if (src[j] == '"' && j + 1 < n && src[j + 1] == '"') { j += 2; continue; }
                        if (src[j] == '"') break;
                        j++;
                    }
                    output.Append("\"\"");
                    AppendNewlines(output, src, i, Math.Min(j + 1, n));
                    i = j + 1;
                }
                else if (c == '"' || c == '\'')
                {
                    var j = i + 1;
                    while (j < n && src[j] != c && src[j] != '\n')
                    {
                        if (src[j] == '\\') j++;
                        j++;
                    }
                    output.Append(c).Append(c);
                    i = j + 1;
                }
                else
                {
                    output.Append(c);
                    i++;
                }
            }

            return output.ToString();
        }

        static void AppendNewlines(StringBuilder output, string src, int start, int end)
        {
            for (var k = start; k < end; k++)
            {
                if (src[k] == '\n') output.Append('\n');
            }
        }
    }
}
