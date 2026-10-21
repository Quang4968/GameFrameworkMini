using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using static Game.Core.Tests.Architecture.ArchitectureConfig;

namespace Game.Core.Tests.Architecture
{
    // Checks the compiled references, not the asmdef: auto-referenced packages (VContainer, R3, UniTask) reach every
    // assembly without being listed, so only the metadata shows what the code really uses.
    public class AssemblyDependencyTests
    {
        [Test]
        public void CoreReferencesOnlyBclAndUnityMath()
        {
            var forbidden = ReferencesOf(CoreAssembly)
                .Where(name => !IsBcl(name) && !CoreAllowedAssemblies.Contains(name));

            Assert.IsEmpty(forbidden, "Core must stay pure C#; move this code to Game behind a Core/Services interface.");
        }

        [Test]
        public void GameUsesSdkOnlyThroughFacade()
        {
            var forbidden = ReferencesOf(GameAssembly)
                .Where(name => IsVendor(name) ||
                               (name.StartsWith(SdkAssemblyPrefix, StringComparison.Ordinal) &&
                                !SdkFacadeAssemblies.Contains(name)));

            Assert.IsEmpty(forbidden, "Game must not name a vendor SDK or a MoodBoost provider assembly.");
        }

        [Test]
        public void SdkNeverReferencesGame()
        {
            var failures = SdkAssemblies()
                .SelectMany(sdk => ReferencesOf(sdk)
                    .Where(name => name.StartsWith(GameAssemblyPrefix, StringComparison.Ordinal))
                    .Select(name => $"{sdk} -> {name}"));

            Assert.IsEmpty(failures, "The SDK is shared between games and must not know any of them.");
        }

        [Test]
        public void SdkModulesStayFreeOfDiAndGameLibraries()
        {
            var failures = SdkAssemblies()
                .Where(sdk => !SdkDiAssemblies.Contains(sdk))
                .SelectMany(sdk => ReferencesOf(sdk)
                    .Where(name => SdkForbiddenAssemblyPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal)))
                    .Select(name => $"{sdk} -> {name}"));

            Assert.IsEmpty(failures, "Only MoodBoost.Sdk.Unity wires VContainer; SDK modules stay plain C# + UniTask.");
        }

        [Test]
        public void FacadeAssembliesNeverNameVendors()
        {
            var failures = SdkFacadeAssemblies.Concat(new[] { CoreAssembly })
                .Where(IsLoaded)
                .SelectMany(owner => ReferencesOf(owner).Where(IsVendor).Select(name => $"{owner} -> {name}"));

            Assert.IsEmpty(failures, "Vendor SDK calls belong in a provider assembly gated by an MB_* define.");
        }

        static IEnumerable<string> SdkAssemblies() =>
            AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetName().Name)
                .Where(name => name.StartsWith(SdkAssemblyPrefix, StringComparison.Ordinal))
                .Distinct();

        static bool IsLoaded(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == name);

        static string[] ReferencesOf(string assemblyName)
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);
            Assert.IsNotNull(assembly, $"Assembly {assemblyName} is not loaded; fix ArchitectureConfig.");
            return assembly.GetReferencedAssemblies().Select(r => r.Name).ToArray();
        }

        static bool IsBcl(string name) =>
            name == "netstandard" || name == "mscorlib" ||
            name.StartsWith("System", StringComparison.Ordinal) ||
            name.StartsWith("Microsoft.", StringComparison.Ordinal);

        static bool IsVendor(string name) =>
            VendorAssemblyPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal));
    }
}
