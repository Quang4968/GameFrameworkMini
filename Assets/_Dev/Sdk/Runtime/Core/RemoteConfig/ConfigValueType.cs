namespace MoodBoost.Sdk
{
    // The type of a remote config parameter. Maps 1-1 onto the "valueType" field in Firebase's template, so do not
    // rename these values to taste.
    public enum ConfigValueType
    {
        String = 0,
        Bool = 1,
        Number = 2,

        // A JSON string. Read it with GetString and parse it yourself — the SDK does not carry a parser.
        Json = 3,
    }
}
