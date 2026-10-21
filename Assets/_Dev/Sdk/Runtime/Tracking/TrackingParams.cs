namespace MoodBoost.Sdk.Tracking
{
    // The parameters of an event, built fluently:
    //
    //     tracking.Log("level_complete", new TrackingParams()
    //         .With("level", level)
    //         .With("duration", seconds));
    //
    // A struct with fixed slots rather than a Dictionary: analytics events fire densely (every move, every win or
    // loss) and a Dictionary means an allocation per call. Firebase still has to build its own Parameter[] array, but
    // that only runs when a real provider is enabled.
    //
    // 16 slots because the tracking plan mandates single events that wide - level_end alone carries 15 parameters and
    // cannot be split without breaking the schema. Still well inside GA4's limit of 25 parameters per event.
    public struct TrackingParams
    {
        public const int Capacity = 16;

        // Not an array, because an array is one more allocation - which is the very thing this struct avoids.
        string k0, k1, k2, k3, k4, k5, k6, k7, k8, k9, k10, k11, k12, k13, k14, k15;
        TrackingValue v0, v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15;

        public int Count { get; private set; }

        public TrackingParams With(string key, string value) => Add(key, TrackingValue.Of(value));

        public TrackingParams With(string key, long value) => Add(key, TrackingValue.Of(value));

        public TrackingParams With(string key, double value) => Add(key, TrackingValue.Of(value));

        public TrackingParams With(string key, bool value) => Add(key, TrackingValue.Of(value ? 1L : 0L));

        TrackingParams Add(string key, TrackingValue value)
        {
            if (string.IsNullOrEmpty(key)) return this;

            switch (Count)
            {
                case 0: k0 = key; v0 = value; break;
                case 1: k1 = key; v1 = value; break;
                case 2: k2 = key; v2 = value; break;
                case 3: k3 = key; v3 = value; break;
                case 4: k4 = key; v4 = value; break;
                case 5: k5 = key; v5 = value; break;
                case 6: k6 = key; v6 = value; break;
                case 7: k7 = key; v7 = value; break;
                case 8: k8 = key; v8 = value; break;
                case 9: k9 = key; v9 = value; break;
                case 10: k10 = key; v10 = value; break;
                case 11: k11 = key; v11 = value; break;
                case 12: k12 = key; v12 = value; break;
                case 13: k13 = key; v13 = value; break;
                case 14: k14 = key; v14 = value; break;
                case 15: k15 = key; v15 = value; break;
                default:
                    SdkLog.Warn(nameof(TrackingParams), $"More than {Capacity} parameters, dropping '{key}'.");
                    return this;
            }

            Count++;
            return this;
        }

        public string GetKey(int index)
        {
            switch (index)
            {
                case 0: return k0;
                case 1: return k1;
                case 2: return k2;
                case 3: return k3;
                case 4: return k4;
                case 5: return k5;
                case 6: return k6;
                case 7: return k7;
                case 8: return k8;
                case 9: return k9;
                case 10: return k10;
                case 11: return k11;
                case 12: return k12;
                case 13: return k13;
                case 14: return k14;
                case 15: return k15;
                default: return null;
            }
        }

        public TrackingValue GetValue(int index)
        {
            switch (index)
            {
                case 0: return v0;
                case 1: return v1;
                case 2: return v2;
                case 3: return v3;
                case 4: return v4;
                case 5: return v5;
                case 6: return v6;
                case 7: return v7;
                case 8: return v8;
                case 9: return v9;
                case 10: return v10;
                case 11: return v11;
                case 12: return v12;
                case 13: return v13;
                case 14: return v14;
                case 15: return v15;
                default: return default;
            }
        }
    }
}
