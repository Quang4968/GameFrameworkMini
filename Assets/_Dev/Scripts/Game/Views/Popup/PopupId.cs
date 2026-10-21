namespace Game.Game.Views.Popup
{
    // Mỗi popup mới = thêm 1 entry ở đây + 1 dòng trong PopupCatalog.asset.
    public enum PopupId
    {
        Settings,
        Pause,
        Win,
        Lose,

        // Bản Settings dùng trong lúc chơi: y hệt Settings nhưng có thêm nút về Home.
        // Phải là id riêng vì catalog map 1 id -> 1 prefab, mà hai màn dùng hai prefab khác nhau
        // (PopupSettingsIngame là prefab variant của PopupSettings).
        //
        // Thêm giá trị mới luôn nối vào CUỐI: catalog lưu enum bằng số, chèn vào giữa là mọi
        // entry phía sau trỏ nhầm popup.
        SettingsIngame,

        // Hết tim: chặn vào màn mới, hiện đếm ngược tới lúc hồi tim tiếp theo và nút mua tim
        // bằng coin.
        OutOfLives,

        // Mua booster bằng coin. Một popup dùng chung cho MỌI loại booster — loại nào đang bán do
        // BuyBoosterGate đưa vào lúc mở, nên thêm booster mới không phải đẻ thêm popup.
        BuyBooster,

        // Xác nhận bỏ ván đang chơi: nhắc người chơi rằng thoát giữa chừng là mất một tim (tim
        // đã trừ lúc vào màn, chỉ hoàn khi thắng). Mở từ nút về Home của PopupSettingsIngame.
        Out,

        // Unlock reward: hands over the free boosters that come with a newly taught booster. One
        // popup for all five — which one is on offer comes from TutorialRewardGate, the same way
        // BuyBooster reads BuyBoosterGate.
        BoosterUnlock,

        // Player profile: rename yourself and pick an avatar face. Opened from the avatar chip
        // in the Home top bar.
        Profile,

        // Shop. Unused on TikTok (no IAP); kept because the catalog stores ids by number.
        Shop,

        // One-time introduction shown when the player first enters a level containing a newly
        // unlocked obstacle feature.
        NewFeature,

        // Tracking consent. Unused on TikTok, which runs under its own privacy terms; kept for the numbering.
        TrackingPermission,

        // Daily login reward: one page of the cycle and its claim button. Opened on Home only, by
        // DailyRewardAutoOpen.
        DailyReward,

        // Scaffolding để test hệ popup bằng tay. Xoá cùng PopupTest.prefab và
        // PopupTestTrigger khi đã có popup thật.
        Test = 100
    }
}
