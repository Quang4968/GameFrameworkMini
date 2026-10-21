using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Game.Core.Tests.Architecture
{
    // One forbidden pattern, matched line by line against source with comments and string literals blanked out.
    public sealed class SourceRule
    {
        public readonly string Id;
        public readonly Regex Pattern;
        public readonly string Fix;

        readonly string[] folders;
        readonly string[] exemptFiles;

        public SourceRule(string id, string pattern, string fix, string[] folders, params string[] exemptFiles)
        {
            Id = id;
            Pattern = new Regex(pattern, RegexOptions.CultureInvariant);
            Fix = fix;
            this.folders = folders;
            this.exemptFiles = exemptFiles;
        }

        public bool Applies(string path) =>
            folders.Any(folder => path.StartsWith(folder, StringComparison.Ordinal)) && !exemptFiles.Contains(path);

        // Names the test case in the Test Runner.
        public override string ToString() => Id;
    }
}
