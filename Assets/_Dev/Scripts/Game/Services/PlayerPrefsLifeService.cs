using System;
using System.Globalization;
using Game.Core.GameLogic;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // Lưu số tim vào PlayerPrefs, cùng lối với PlayerPrefsCoinService. Luật nằm ở Core —
    // LifeClockGuard quyết định công nhận bao nhiêu thời gian, LifeRegenRules quyết định thời gian
    // đó đổi ra mấy tim; ở đây chỉ đọc đồng hồ, gọi luật, rồi lưu kết quả.
    //
    // Máy chưa chơi bao giờ thì đầy tim; máy đã chơi rồi thì giữ nguyên số còn lại. Trần được kẹp
    // lại mỗi lần đọc để hạ maxLives trong asset không để lại số dư lớn hơn trần.
    //
    // Không có coroutine/Update nào chạy nền: ILifeService bị dựng lại mỗi lần đổi scene (đăng ký
    // riêng ở từng LifetimeScope — xem "Đổi scene và DI theo scene" trong ARCHITECTURE.md), nên
    // Refresh() ở constructor đã đủ để bắt kịp thời gian đã trôi qua lúc scene mới mở lên. Ai cần
    // số tươi hơn giữa lúc đứng yên trong scene (đếm ngược ở HUD/popup) tự gọi Refresh().
    public sealed class PlayerPrefsLifeService : ILifeService
    {
        public const string LivesKey = "Game.Lives";
        public const string RemainingKey = "Game.LivesRefillRemainingTicks";
        public const string AnchorKey = "Game.LivesClockAnchorUtcTicks";
        public const string UnlimitedEndKey = "Game.UnlimitedLifeEndUtcTicks";

        // Khoá của bản cũ lưu MỐC TUYỆT ĐỐI. Đọc một lần để chuyển sang dạng thời gian còn lại rồi
        // xoá, để máy đang chơi dở không bị reset đồng hồ về trọn một khoảng.
        const string LegacyNextRefillKey = "Game.LivesNextRefillUtcTicks";

        readonly IClockService clock;
        readonly TimeSpan refillInterval;

        DateTime anchorUtc;
        DateTime unlimitedEndUtc;

        public int Lives { get; private set; }

        public int MaxLives { get; }

        public TimeSpan TimeUntilNextLife { get; private set; }

        public event Action<int> LivesChanged;

        public TimeSpan UnlimitedTimeLeft { get; private set; }

        public event Action<bool> UnlimitedChanged;

        public PlayerPrefsLifeService(IClockService clock, int maxLives, TimeSpan refillInterval)
        {
            this.clock = clock ?? new SystemClockService();
            MaxLives = Mathf.Max(1, maxLives);
            this.refillInterval = refillInterval;

            Lives = Mathf.Clamp(PlayerPrefs.GetInt(LivesKey, MaxLives), 0, MaxLives);
            anchorUtc = ReadUtc(AnchorKey);
            TimeUntilNextLife = ReadRemaining();
            unlimitedEndUtc = ReadUtc(UnlimitedEndKey);
            UnlimitedTimeLeft = UnlimitedLifeRules.TimeLeft(unlimitedEndUtc, this.clock.UtcNow);

            // Bắt ngay lúc dựng service: máy tắt qua đúng lúc tim đang hồi thì mở app lên phải
            // thấy số đã cộng, không phải chờ một sự kiện nào khác kích hoạt. Máy chưa có mốc nào
            // thì ép ghi một lần để lần chạy sau có cái mà đối chiếu.
            Recompute(Lives, forcePersist: anchorUtc == DateTime.MinValue);
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;

            Recompute(Lives);
            if (Lives >= MaxLives) return;

            Recompute(Mathf.Min(Lives + amount, MaxLives));
        }

        public bool TrySpend(int amount)
        {
            if (amount <= 0) return false;

            Recompute(Lives);
            if (Lives < amount) return false;

            Recompute(Lives - amount);
            return true;
        }

        public void AddUnlimited(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero) return;

            unlimitedEndUtc = UnlimitedLifeRules.Extend(unlimitedEndUtc, clock.UtcNow, duration);
            PlayerPrefs.SetString(UnlimitedEndKey, unlimitedEndUtc.Ticks.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

            RecomputeUnlimited();
        }

        public void Refresh()
        {
            Recompute(Lives);
            RecomputeUnlimited();
        }

        // Only the end is persisted, so a window that expires needs no write.
        void RecomputeUnlimited()
        {
            var wasUnlimited = UnlimitedTimeLeft > TimeSpan.Zero;
            UnlimitedTimeLeft = UnlimitedLifeRules.TimeLeft(unlimitedEndUtc, clock.UtcNow);

            var isUnlimited = UnlimitedTimeLeft > TimeSpan.Zero;
            if (isUnlimited != wasUnlimited) UnlimitedChanged?.Invoke(isUnlimited);
        }

        void Recompute(int lives, bool forcePersist = false)
        {
            var (elapsed, nextAnchor) = LifeClockGuard.Measure(anchorUtc, clock.UtcNow);
            anchorUtc = nextAnchor;

            var result = LifeRegenRules.Apply(lives, MaxLives, TimeUntilNextLife, elapsed, refillInterval);

            var changed = result.lives != Lives;
            Lives = result.lives;
            TimeUntilNextLife = result.remaining;

            // Refresh() được gọi mỗi frame (đếm ngược ở HUD/popup) nên chỉ ghi PlayerPrefs khi SỐ
            // TIM đổi, không ghi theo từng nhịp đếm ngược. Vẫn đúng khi bị kill giữa chừng: mốc và
            // thời gian còn lại luôn được ghi thành một CẶP, nên lần chạy sau đo lại từ cặp cũ ra
            // đúng bằng lượng thời gian đã trôi qua.
            if (changed || forcePersist) Persist();

            if (changed) LivesChanged?.Invoke(Lives);
        }

        void Persist()
        {
            PlayerPrefs.SetInt(LivesKey, Lives);
            PlayerPrefs.SetString(RemainingKey, TimeUntilNextLife.Ticks.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(AnchorKey, anchorUtc.Ticks.ToString(CultureInfo.InvariantCulture));

            // Save ngay thay vì đợi Unity tự ghi lúc thoát: game mobile bị kill nền bất cứ lúc
            // nào, và tim vừa mất mà không ghi kịp là người chơi chơi lại được miễn phí.
            PlayerPrefs.Save();
        }

        static DateTime ReadUtc(string key)
        {
            var ticks = ReadTicks(key);
            return ticks > 0 ? new DateTime(ticks, DateTimeKind.Utc) : DateTime.MinValue;
        }

        TimeSpan ReadRemaining()
        {
            if (PlayerPrefs.HasKey(RemainingKey))
            {
                var ticks = ReadTicks(RemainingKey);
                return ticks > 0 ? TimeSpan.FromTicks(ticks) : TimeSpan.Zero;
            }

            return ReadLegacyRemaining();
        }

        // Bản cũ lưu mốc tuyệt đối của lần hồi kế tiếp. Quy ra thời gian còn lại theo giờ máy hiện
        // tại, kẹp trong [0, refillInterval] để một cái mốc rác không cho tim ngay hoặc treo mãi.
        TimeSpan ReadLegacyRemaining()
        {
            var ticks = ReadTicks(LegacyNextRefillKey);
            PlayerPrefs.DeleteKey(LegacyNextRefillKey);

            if (ticks <= 0) return TimeSpan.Zero;

            var remaining = new DateTime(ticks, DateTimeKind.Utc) - clock.UtcNow;
            if (remaining <= TimeSpan.Zero) return TimeSpan.Zero;

            return remaining > refillInterval ? refillInterval : remaining;
        }

        static long ReadTicks(string key)
        {
            return long.TryParse(PlayerPrefs.GetString(key, "0"), NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var ticks) ? ticks : 0;
        }
    }
}
