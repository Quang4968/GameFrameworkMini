using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Game.Data
{
    // Thông số tim dùng CHUNG cho cả game — chỉnh ở asset là mọi scene đổi theo, không scene nào
    // được tự khai con số riêng.
    [CreateAssetMenu(menuName = "Game/Life Settings", fileName = "LifeSettings")]
    public sealed class LifeSettingsSO : ScriptableObject
    {
        [Tooltip("Số tim tối đa, cũng là số tim có sẵn lúc mở game lần đầu. Đổi số này KHÔNG " +
            "cộng thêm tim cho máy đã chơi rồi vì số tim đã nằm trong PlayerPrefs — chỉ hạ trần " +
            "mới cắt bớt số đang có.")]
        [SerializeField, Min(1)] int maxLives = 5;

        // Giữ FormerlySerializedAs để asset LifeSettings đang lưu con số ở 'livesPerLoss' tự
        // chuyển sang field này, khỏi phải nhập lại tay.
        [Tooltip("Số tim trả trước mỗi lần vào màn. Thắng thì được hoàn lại đúng số này; thua " +
            "hoặc bỏ màn giữa chừng thì mất.")]
        [FormerlySerializedAs("livesPerLoss")]
        [SerializeField, Min(0)] int livesPerLevel = 1;

        [Tooltip("Số phút để tự hồi 1 tim khi chưa đầy. 0 = tắt hồi tim theo thời gian.")]
        [SerializeField, Min(0)] int refillMinutes = 30;

        // KHÔNG dùng FormerlySerializedAs cho tên cũ 'coinCostPerLife': ý nghĩa đã đổi từ giá MỖI
        // TIM sang giá MỘT LẦN ĐẦY BÌNH, mang con số cũ sang là bán đầy bình với giá của một tim.
        [Tooltip("Số coin để mua một phát đầy tim. Giá cố định: thiếu 1 tim hay thiếu cả 5 đều " +
            "trả bằng nhau. 0 = tắt mua tim bằng coin.")]
        [SerializeField, Min(0)] int coinCostPerRefill = 900;

        [Tooltip("Số tim được cộng sau mỗi lần xem hết một quảng cáo. 0 = tắt nút xem quảng cáo.")]
        [SerializeField, Min(0)] int livesPerAd = 1;

        public int MaxLives => maxLives;
        public int LivesPerLevel => livesPerLevel;
        public TimeSpan RefillInterval => TimeSpan.FromMinutes(refillMinutes);
        public int CoinCostPerRefill => coinCostPerRefill;
        public int LivesPerAd => livesPerAd;
    }
}
