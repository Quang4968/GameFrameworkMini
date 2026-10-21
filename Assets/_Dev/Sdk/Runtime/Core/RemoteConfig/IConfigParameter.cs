namespace MoodBoost.Sdk
{
    // The NON-generic view of a ConfigKey<T>, so they can be enumerated.
    //
    // Only the Editor template exporter uses it. Nothing calls it at runtime, so going through an interface (which
    // boxes the struct) costs the real build nothing.
    public interface IConfigParameter
    {
        string Name { get; }

        string Description { get; }

        // Empty means the parameter sits at the root of the template; a name puts it inside that parameterGroup.
        string Group { get; }

        ConfigValueType ValueType { get; }

        // Firebase stores EVERY value as a string, including numbers and bools. This is where a C# type is turned
        // into the string form the template accepts.
        string DefaultAsTemplateValue { get; }
    }
}
