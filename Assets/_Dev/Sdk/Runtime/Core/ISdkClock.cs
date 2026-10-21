namespace MoodBoost.Sdk
{
    // The SDK's clock, split into an interface because ad capping/cooldown rules are pure logic and have to be
    // testable with a fake clock — the same reason as the game's IClockService.
    //
    // Deliberately seconds since app launch (realtime) rather than a UTC timestamp: capping only needs the gap
    // between two ad displays, and a UTC timestamp is something the player can tamper with.
    public interface ISdkClock
    {
        double RealtimeSeconds { get; }
    }
}
