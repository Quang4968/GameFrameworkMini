using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Game.Core.Tests.Architecture
{
    // Fails when code breaks a convention from .claude/rules/ that the compiler cannot see. Legacy hits live in
    // ArchitectureBaseline; never add an entry there to make new code pass.
    public class SourceRulesTests
    {
        [TestCaseSource(typeof(SourceRules), nameof(SourceRules.All))]
        public void NoNewViolations(SourceRule rule)
        {
            var failures = new List<string>();

            foreach (var source in CSharpSource.All.Where(s => rule.Applies(s.Path)))
            {
                var hits = source.LinesMatching(rule.Pattern);
                var allowed = ArchitectureBaseline.Allowed(rule.Id, source.Path);
                if (hits.Count <= allowed) continue;

                failures.Add($"{source.Path}: {hits.Count} hit(s), baseline {allowed}, lines {string.Join(", ", hits)}");
            }

            Assert.IsEmpty(failures, $"[{rule.Id}] {rule.Fix}\n{string.Join("\n", failures)}");
        }

        [Test]
        public void FilesStayUnderLineLimit()
        {
            var failures = CSharpSource.All
                .Where(s => s.LineCount > Ceiling(s.Path))
                .Select(s => $"{s.Path}: {s.LineCount} lines, limit {Ceiling(s.Path)}")
                .ToList();

            Assert.IsEmpty(failures,
                $"Split by responsibility before a file crosses {ArchitectureConfig.MaxFileLines} lines; " +
                $"legacy files may not grow.\n{string.Join("\n", failures)}");
        }

        [Test]
        public void BaselineHasNoPaidOffDebt()
        {
            var byPath = CSharpSource.All.ToDictionary(s => s.Path);
            var rules = SourceRules.All.ToDictionary(r => r.Id);
            var stale = new List<string>();

            foreach (var ruleEntry in ArchitectureBaseline.Violations)
            {
                if (!rules.TryGetValue(ruleEntry.Key, out var rule))
                {
                    stale.Add($"[{ruleEntry.Key}] no such rule");
                    continue;
                }

                foreach (var fileEntry in ruleEntry.Value)
                {
                    var actual = byPath.TryGetValue(fileEntry.Key, out var source)
                        ? source.LinesMatching(rule.Pattern).Count
                        : 0;
                    if (actual < fileEntry.Value)
                        stale.Add($"[{rule.Id}] {fileEntry.Key}: baseline {fileEntry.Value}, now {actual}");
                }
            }

            foreach (var lineEntry in ArchitectureBaseline.FileLines)
            {
                if (!byPath.TryGetValue(lineEntry.Key, out var source) ||
                    source.LineCount <= ArchitectureConfig.MaxFileLines)
                    stale.Add($"[file-lines] {lineEntry.Key}: now within the limit or gone");
            }

            Assert.IsEmpty(stale, $"Lower or remove these ArchitectureBaseline entries:\n{string.Join("\n", stale)}");
        }

        static int Ceiling(string path) =>
            ArchitectureBaseline.FileLines.TryGetValue(path, out var legacy) ? legacy : ArchitectureConfig.MaxFileLines;
    }
}
