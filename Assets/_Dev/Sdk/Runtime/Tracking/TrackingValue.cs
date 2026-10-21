namespace MoodBoost.Sdk.Tracking
{
    public enum TrackingValueKind
    {
        String = 0,
        Long = 1,
        Double = 2,
    }

    // The value of one parameter. Keeps its TYPE rather than coercing everything to string, because GA4 can only
    // build average/sum charts on numeric parameters - pushing a number through as a string loses that ability and
    // cannot be repaired for data already sent.
    public readonly struct TrackingValue
    {
        public readonly TrackingValueKind Kind;
        public readonly string StringValue;
        public readonly long LongValue;
        public readonly double DoubleValue;

        TrackingValue(TrackingValueKind kind, string s, long l, double d)
        {
            Kind = kind;
            StringValue = s;
            LongValue = l;
            DoubleValue = d;
        }

        public static TrackingValue Of(string value) => new TrackingValue(TrackingValueKind.String, value ?? string.Empty, 0L, 0d);

        public static TrackingValue Of(long value) => new TrackingValue(TrackingValueKind.Long, null, value, 0d);

        public static TrackingValue Of(double value) => new TrackingValue(TrackingValueKind.Double, null, 0L, value);

        public override string ToString()
        {
            switch (Kind)
            {
                case TrackingValueKind.Long: return LongValue.ToString();
                case TrackingValueKind.Double: return DoubleValue.ToString("R");
                default: return StringValue;
            }
        }
    }
}
