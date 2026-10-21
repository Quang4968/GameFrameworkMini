using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using MoodBoost.Sdk.Unity;

namespace MoodBoost.Sdk.Editor
{
    // One declared key together with where it was declared - so the window's table can point at the right file when two
    // keys share a name.
    public readonly struct DeclaredParameter
    {
        public readonly IConfigParameter Parameter;
        public readonly string Owner;

        public DeclaredParameter(IConfigParameter parameter, string owner)
        {
            Parameter = parameter;
            Owner = owner;
        }
    }

    // Collects every ConfigKey declared in the project and builds the Firebase Remote Config JSON template.
    //
    // Why reflection is allowed here while SdkProviderCatalog forbids it: there, reflection trades a compile error for a
    // runtime error on a player's device. Here everything runs in the Editor, the result appears immediately in a table
    // in front of whoever pressed the button, and what it buys is far more valuable - it is IMPOSSIBLE to forget to
    // register a key. With a hand-written list, forgetting is silent: the key is missing from the template, the console
    // has no such parameter, the game runs on the default forever and nobody notices until they look at the data.
    public static class RemoteConfigTemplate
    {
        // Firebase's parameter naming rule.
        static readonly Regex ValidName = new Regex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        const int MaxNameLength = 256;
        const int MaxDescriptionLength = 256;

        public static List<DeclaredParameter> Collect()
        {
            var found = new List<DeclaredParameter>();

            var coreAssembly = typeof(ConfigKey<>).Assembly;
            var coreName = coreAssembly.GetName().Name;

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                // An assembly not referencing Core cannot contain any ConfigKey. Skipped early to avoid loading the types
                // of all of UnityEngine and the BCL.
                if (assembly != coreAssembly
                    && assembly.GetReferencedAssemblies().All(reference => reference.Name != coreName))
                {
                    continue;
                }

                foreach (var type in SafeGetTypes(assembly))
                {
                    if (type == null) continue;

                    var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);

                    foreach (var field in fields)
                    {
                        if (!field.FieldType.IsGenericType) continue;
                        if (field.FieldType.GetGenericTypeDefinition() != typeof(ConfigKey<>)) continue;

                        if (field.GetValue(null) is IConfigParameter parameter)
                        {
                            found.Add(new DeclaredParameter(parameter, $"{type.Name}.{field.Name}"));
                        }
                    }
                }
            }

            found.Sort((left, right) =>
            {
                var byGroup = string.CompareOrdinal(left.Parameter.Group, right.Parameter.Group);
                return byGroup != 0 ? byGroup : string.CompareOrdinal(left.Parameter.Name, right.Parameter.Name);
            });

            return found;
        }

        static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException error)
            {
                // One failed type load must not kill the whole scan - the rest is still usable.
                return error.Types;
            }
        }

        // Hard errors: with any of these the file is NOT exported, because a broken template only surfaces at deploy time.
        public static List<string> Validate(IReadOnlyList<DeclaredParameter> declared)
        {
            var problems = new List<string>();
            var seen = new Dictionary<string, string>(declared.Count);

            foreach (var entry in declared)
            {
                var name = entry.Parameter.Name;

                if (string.IsNullOrEmpty(name))
                {
                    problems.Add($"{entry.Owner}: empty name.");
                    continue;
                }

                if (name.Length > MaxNameLength)
                {
                    problems.Add($"{entry.Owner}: name longer than {MaxNameLength} characters.");
                }

                if (!ValidName.IsMatch(name))
                {
                    problems.Add($"{entry.Owner}: name '{name}' has the wrong shape - letters, digits and '_' only, and " +
                        "it cannot start with a digit.");
                }

                if (seen.TryGetValue(name, out var owner))
                {
                    problems.Add($"Duplicate key '{name}': declared in both {owner} and {entry.Owner}. " +
                        "The template can only keep one.");
                    continue;
                }

                seen[name] = entry.Owner;
            }

            return problems;
        }

        // Soft errors: still exportable, just worth fixing.
        public static List<string> Warnings(IReadOnlyList<DeclaredParameter> declared)
        {
            var warnings = new List<string>();

            foreach (var entry in declared)
            {
                if (string.IsNullOrEmpty(entry.Parameter.Description))
                {
                    warnings.Add($"{entry.Parameter.Name}: no description. Whoever changes the numbers on the console " +
                        "is usually not the person who wrote the code.");
                }
                else if (entry.Parameter.Description.Length > MaxDescriptionLength)
                {
                    warnings.Add($"{entry.Parameter.Name}: description longer than {MaxDescriptionLength} characters, " +
                        "the console may truncate it.");
                }
            }

            return warnings;
        }

        // settings may be null: every key then exports its code default.
        public static string Build(IReadOnlyList<DeclaredParameter> declared, SdkSettings settings)
        {
            var defaults = ReadProjectDefaults(settings);
            var ungrouped = declared.Where(entry => string.IsNullOrEmpty(entry.Parameter.Group)).ToList();

            var groups = declared
                .Where(entry => !string.IsNullOrEmpty(entry.Parameter.Group))
                .GroupBy(entry => entry.Parameter.Group)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .ToList();

            var json = new StringBuilder(1024);
            json.Append("{\n");

            if (ungrouped.Count == 0)
            {
                // The "parameters" field still has to be present even when empty - every key being inside a group is
                // perfectly normal.
                json.Append("  \"parameters\": {}");
            }
            else
            {
                json.Append("  \"parameters\": {\n");
                AppendParameters(json, ungrouped, defaults, indent: 4);
                json.Append("  }");
            }

            if (groups.Count > 0)
            {
                json.Append(",\n  \"parameterGroups\": {\n");

                for (var index = 0; index < groups.Count; index++)
                {
                    var group = groups[index];

                    json.Append("    \"").Append(Escape(group.Key)).Append("\": {\n");
                    json.Append("      \"parameters\": {\n");
                    AppendParameters(json, group.ToList(), defaults, indent: 8);
                    json.Append("      }\n");
                    json.Append("    }").Append(index < groups.Count - 1 ? ",\n" : "\n");
                }

                json.Append("  }");
            }

            json.Append("\n}\n");
            return json.ToString();
        }

        // Last entry wins on a repeated key, the same as RemoteConfigDefaults.Set at runtime.
        static Dictionary<string, string> ReadProjectDefaults(SdkSettings settings)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (settings == null) return result;

            foreach (var entry in settings.RemoteConfigDefaults)
                if (!string.IsNullOrWhiteSpace(entry.key)) result[entry.key] = entry.value ?? string.Empty;

            return result;
        }

        static void AppendParameters(StringBuilder json, IReadOnlyList<DeclaredParameter> entries,
            IReadOnlyDictionary<string, string> defaults, int indent)
        {
            var pad = new string(' ', indent);
            var inner = new string(' ', indent + 2);

            for (var index = 0; index < entries.Count; index++)
            {
                var parameter = entries[index].Parameter;

                json.Append(pad).Append('"').Append(Escape(parameter.Name)).Append("\": {\n");

                json.Append(inner).Append("\"defaultValue\": { \"value\": \"")
                    .Append(Escape(defaults.TryGetValue(parameter.Name, out var value)
                        ? value : parameter.DefaultAsTemplateValue)).Append("\" },\n");

                json.Append(inner).Append("\"valueType\": \"").Append(ValueTypeName(parameter.ValueType)).Append('"');

                if (!string.IsNullOrEmpty(parameter.Description))
                {
                    json.Append(",\n").Append(inner).Append("\"description\": \"")
                        .Append(Escape(parameter.Description)).Append('"');
                }

                json.Append('\n').Append(pad).Append('}').Append(index < entries.Count - 1 ? ",\n" : "\n");
            }
        }

        static string ValueTypeName(ConfigValueType type)
        {
            switch (type)
            {
                case ConfigValueType.Bool: return "BOOLEAN";
                case ConfigValueType.Number: return "NUMBER";
                case ConfigValueType.Json: return "JSON";
                default: return "STRING";
            }
        }

        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var escaped = new StringBuilder(value.Length + 8);

            foreach (var character in value)
            {
                switch (character)
                {
                    case '"': escaped.Append("\\\""); break;
                    case '\\': escaped.Append("\\\\"); break;
                    case '\n': escaped.Append("\\n"); break;
                    case '\r': escaped.Append("\\r"); break;
                    case '\t': escaped.Append("\\t"); break;
                    default:
                        if (character < ' ') escaped.Append("\\u").Append(((int)character).ToString("x4"));
                        else escaped.Append(character);
                        break;
                }
            }

            return escaped.ToString();
        }
    }
}
