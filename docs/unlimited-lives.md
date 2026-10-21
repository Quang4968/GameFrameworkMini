# Tim vô hạn (Unlimited Lives)

Một cửa sổ thời gian trong đó vào màn không tốn tim, nằm trong `ILifeService` cạnh hệ tim thường.

## Tóm tắt

- **Tim vô hạn** nằm trong `ILifeService` (`UnlimitedTimeLeft`, `AddUnlimited`): đang chạy thì vào
  màn không trừ tim và luôn qua cổng Hết tim (`CanStartLevelUseCase`). Số tim vẫn giữ nguyên và vẫn hồi như thường.
- Đồng hồ là **mốc kết thúc UTC** (`Game.UnlimitedLifeEndUtcTicks`), chạy theo giờ thật kể cả khi tắt app,
  và **cố ý không chống vặn giờ** (không đi qua `LifeClockGuard`, khác hẳn đồng hồ hồi tim).
- Nhận thêm lúc đang chạy thì **cộng dồn** vào mốc kết thúc; lúc đã hết thì tính từ bây giờ (`UnlimitedLifeRules.Extend`).
- `UnlimitedChanged(bool)` chỉ bắn lúc cửa sổ **bắt đầu** và lúc **hết**; `Refresh()` (mỗi giây qua `LifeTicker`)
  là thứ phát hiện lúc hết. Cộng dồn vào cửa sổ đang chạy thì không bắn.
- Lúc thắng **không hỏi lại luật**: `WinLifeRefundPresenter` hoàn đúng `LevelStartLifePresenter.Charged`,
  con số đã trừ thật lúc vào màn. Hỏi lại "có đang vô hạn không" thì màn vào miễn phí đẻ ra tim, vì cửa sổ có thể hết giữa màn.
- `SpendLifeOnLevelStartUseCase` gọi `Refresh()` trước khi xét, để cửa sổ vừa hết một giây trước không cho vào màn miễn phí.
- Nút Home lúc đang chơi **bỏ qua** popup xác nhận "-1 tim" khi đang vô hạn (`LeavingLevelCostsLifeUseCase`).
- Nguồn: reward shop `UnlimitedLives` (số **phút**, cuối `E_ShopRewardKind`), quà Daily Reward `unlimited_lives_minutes`
  ([daily-reward.md](daily-reward.md)), và `LifeDebugWindow` (+15 phút / +1 giờ).

**Bắt đầu từ**: `Core/GameLogic/UnlimitedLifeRules.cs`, `Game/Services/PlayerPrefsLifeService.cs`,
`Game/Views/Gameplay/LevelStartLifePresenter.cs`.

## HUD

Framework không còn view HUD; HUD do game dựng lại từ demo phải giữ các luật sau.

- Trong lúc vô hạn, ô tim hiện icon vô hạn thay cho con số, và đồng hồ đếm ngược thời gian vô hạn còn lại
  (`h:mm:ss` khi quá 1 giờ), ưu tiên hơn đồng hồ hồi tim lẫn chữ `Full`.
- Nút "+" mua tim ẩn đi trong lúc vô hạn. Nút nghe sự kiện từ `Construct` chứ không phải `OnEnable`, vì object bị ẩn có
  thể chính là cha của nó.

## Test

`UnlimitedLifeRulesTests`, `LeavingLevelCostsLifeUseCaseTests`, và phần tim vô hạn trong `LevelLifeFlowUseCaseTests`,
`CanStartLevelUseCaseTests`, `GrantShopRewardsUseCaseTests`.
