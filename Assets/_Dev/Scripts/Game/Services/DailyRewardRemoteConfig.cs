using MoodBoost.Sdk;

namespace Game.Game.Services
{
    // The whole daily reward cycle in ONE key. The default is a 14-day cycle (odd week, then even week)
    // in coins and lives only; each game rewrites it with its own boosters and amounts. It is also what
    // RemoteDailyRewardSchedule falls back to when the remote value is unusable.
    public static class DailyRewardRemoteConfig
    {
        const string Retention = "Retention";

        public static readonly ConfigKey<string> Cycle = ConfigKey.Json(
            "daily_reward_cycle",
            "{\"unlock_level\":0,\"days\":[" +
            Day("{\"id\":\"unlimited_lives_minutes\",\"amount\":15}") +
            Day("{\"id\":\"coins\",\"amount\":50}") +
            Chest("{\"id\":\"coins\",\"amount\":100},{\"id\":\"unlimited_lives_minutes\",\"amount\":15}") +
            Day("{\"id\":\"lives\",\"amount\":1}") +
            Day("{\"id\":\"unlimited_lives_minutes\",\"amount\":30}") +
            Day("{\"id\":\"coins\",\"amount\":100}") +
            Day("{\"id\":\"unlimited_lives_minutes\",\"amount\":60},{\"id\":\"coins\",\"amount\":200}," +
                "{\"id\":\"lives\",\"amount\":2}") +
            Day("{\"id\":\"unlimited_lives_minutes\",\"amount\":15}") +
            Day("{\"id\":\"lives\",\"amount\":1}") +
            Day("{\"id\":\"unlimited_lives_minutes\",\"amount\":30}") +
            Day("{\"id\":\"coins\",\"amount\":100}") +
            Chest("{\"id\":\"coins\",\"amount\":100},{\"id\":\"unlimited_lives_minutes\",\"amount\":15}") +
            Day("{\"id\":\"coins\",\"amount\":150}") +
            "{\"items\":[{\"id\":\"unlimited_lives_minutes\",\"amount\":60},{\"id\":\"coins\",\"amount\":300}," +
            "{\"id\":\"lives\",\"amount\":2}]}]}",
            "Daily login cycle, one entry per login day in whole weeks of 7. Item ids: coins, lives, " +
            "unlimited_lives_minutes (amount in minutes), or any BoosterType in snake_case (extra_slot). " +
            "\"chest\":true hides a day's items behind a gift until claimed. " +
            "unlock_level: displayed level that opens the feature, 0 = always.",
            Retention);

        static string Day(string items) => "{\"items\":[" + items + "]},";

        static string Chest(string items) => "{\"chest\":true,\"items\":[" + items + "]},";
    }
}
