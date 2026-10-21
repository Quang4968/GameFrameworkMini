using Game.Core.Domain;

namespace Game.Game.Services
{
    // resource_name của tracking plan, và cũng là phần {resource} trong key balance_{resource}_n.
    // Một chỗ duy nhất, vì hai nơi đó BẮT BUỘC phải khớp nhau: dashboard join số dư với giao dịch
    // bằng chính chuỗi này.
    public static class ResourceNames
    {
        public const string Coin = "coin";
        public const string Life = "life";

        // Amounts are minutes. No balance_ property carries it: the Firebase property cap is tight.
        public const string UnlimitedLife = "unlimited_life";

        // Tên enum viết thường: shuffle, lift, dig, extraslot, vacuum.
        public static string Of(BoosterType type) => type.ToString().ToLowerInvariant();
    }
}
