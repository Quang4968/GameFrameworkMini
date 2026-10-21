using UnityEngine;

namespace Game.Game.Data
{
    // Thông số kinh tế coin dùng CHUNG cho cả game — chỉnh ở asset là mọi chỗ đổi theo, không
    // scene nào được tự khai con số riêng.
    [CreateAssetMenu(menuName = "Game/Coin Settings", fileName = "CoinSettings")]
    public sealed class CoinSettingsSO : ScriptableObject
    {
        [Tooltip("Số coin tặng mỗi lần thắng một màn.")]
        [SerializeField, Min(0)] int coinsPerWin = 30;

        [Tooltip("Số coin có sẵn lúc mở game lần đầu. Đổi số này KHÔNG ảnh hưởng máy đã chơi rồi " +
            "vì số dư đã nằm trong PlayerPrefs.")]
        [SerializeField, Min(0)] int startingCoins;

        [Tooltip("Revive price on the Lose popup: pay this many coins for the game's ILevelReviveHandler " +
            "hàng trên bục, lấy lại chỗ trống rồi chơi tiếp. Để 0 thì cứu miễn phí.")]
        [SerializeField, Min(0)] int reviveCost = 900;

        public int CoinsPerWin => coinsPerWin;
        public int ReviveCost => reviveCost;
        public int StartingCoins => startingCoins;
    }
}
