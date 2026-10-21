namespace MoodBoost.Sdk.Tracking
{
    // One resource moving in or out of the player's pocket. Pass a list of these when several move at once - the
    // parallel lists the plan asks for are then built for you, index aligned.
    public readonly struct ResourceChange
    {
        public readonly ResourceType Type;

        // gold, heart, hammer... - the name differs per game, so it stays a string.
        public readonly string Name;

        public readonly long Amount;

        // What the player holds AFTER this change.
        public readonly long Balance;

        public ResourceChange(ResourceType type, string name, long amount, long balance)
        {
            Type = type;
            Name = name;
            Amount = amount;
            Balance = balance;
        }
    }
}
