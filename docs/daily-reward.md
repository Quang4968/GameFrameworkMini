# Quà đăng nhập hằng ngày (Daily Reward)

Chu kỳ quà đăng nhập, popup ở Home, danh sách quà theo từng ngày lấy từ remote config.

## Tóm tắt

- **Ô tiếp theo tính theo số lần NHẬN, không theo ngày lịch**: `ClaimedCount % số ngày`. Nghỉ bao
  lâu cũng nhận đúng ô kế tiếp (không mất ngày, không nhảy cóc); nhận xong ô cuối thì quay về ô 0.
- **Sang ngày mới lúc 0h giờ máy**, và phải **lớn hơn hẳn** ngày nhận gần nhất (`Today > LastClaimDay`),
  không chỉ khác ngày: vặn đồng hồ lùi không mở lại được ngày đã nhận; đã nhận lúc đồng hồ vặn tới thì phải đợi giờ thật đuổi kịp.
- **Ghi nhận claim TRƯỚC, phát quà SAU** (`ClaimDailyRewardUseCase`): lỡ bị ngắt giữa hai bước thì người chơi mất
  quà của ngày đó chứ không nhận được hai lần.
- **Cả chu kỳ nằm trong MỘT key JSON là `daily_reward_cycle`** (`DailyRewardRemoteConfig`). Yêu cầu: số ngày là **bội của 7**
  và ngày nào cũng có ít nhất một món với id hợp lệ và amount > 0. Sai thì **giữ bộ quà hiện có** (lần đầu thì dùng bộ mặc định)
  và ghi warning. Đổi số ngày trên remote thì vị trí hiện tại của người chơi bị tính lại theo modulo mới và có thể nhảy sang ô khác.
- **`IDailyRewardGranter` là chỗ DUY NHẤT phụ thuộc vào từng game.** Mặc định là `ShopDailyRewardGranter`, đi qua
  `DailyRewardPayoutRules` rồi `GrantShopRewardsUseCase`. Id không nhận ra thì bị **bỏ qua kèm warning**:
  gõ sai id trên remote chỉ làm mất đúng món đó.
- Id quà: `coins`, `lives`, `unlimited_lives_minutes` (đơn vị **phút**, cần tính năng tim vô hạn), và **mọi `BoosterType` viết
  snake_case** (`ExtraSlot` → `extra_slot`). Game thêm booster vào enum là dùng được ngay, không phải sửa code.
  Mỗi id cần **một dòng icon** trong `DailyRewardSettings.asset`; thiếu dòng thì quà vẫn được phát nhưng thẻ chỉ hiện con số.
- **JSON mặc định chỉ có coin, tim và tim vô hạn**, vì `BoosterType` của template đang rỗng. Game nào dùng template thì viết lại
  bộ mặc định trong `DailyRewardRemoteConfig` bằng booster của mình.
- **Ngày rương** được đánh dấu bằng `"chest": true` trong JSON (bộ mặc định: ngày 3 tuần lẻ, ngày 5 tuần chẵn). Thẻ chỉ hiện hộp quà
  (`chestIcon`), nhận xong thì panel `DailyRewardChestReveal` hiện ra: nền đen, các món bung ra, và nút COLLECT. **Quà được phát
  lúc bấm CLAIM**, còn COLLECT chỉ đóng popup, nên tắt app giữa lúc panel đang mở cũng không mất quà.
- **Mỗi lần mở app popup chỉ tự bật một lần** (`DailyRewardAutoOpen`), và chỉ khi tính năng đã mở khoá (`unlock_level` so với
  `LevelShow`) và hôm nay còn quà chưa nhận. Người chơi đóng mà không nhận thì những lần về Home sau **không** tự bật lại.
  Cờ này là `static` và được reset ở `SubsystemRegistration` (vì Domain Reload đang tắt). Popup đợi màn loading mở hẳn (`SceneTransitionProvider.IsCovering`) rồi mới bật.
- Đăng ký bằng `builder.RegisterDailyReward<TGranter>(settings)`. `RegisterEntryPoint<DailyRewardAutoOpen>()` **chỉ đăng ký khi
  có PopupHost và có settings**: popup cần resolve `DailyRewardSettingsSO`, nên thiếu asset mà vẫn đăng ký thì popup lỗi ngay lúc mở.
  Scope phải có sẵn `IClockService` (từ `RegisterLifeSystem`) và `IRemoteConfigService` (từ SDK).
- `resource_earn` dùng placement `daily_reward`, reason `reward`, detail `day_<n>`, ánh xạ tài nguyên qua `ShopGrantTracking` giống shop.
- PlayerPrefs `Game.DailyRewardClaimed` và `Game.DailyRewardLastDay`: bấm nhận quà khi play-test sẽ ghi vào hai key này.
- Test tay bằng `Game/Daily Reward Debug`. Muốn **đặt tiến độ** (nhảy ngày, xoá dữ liệu) thì phải làm **lúc chưa Play**, vì
  service đang chạy giữ số trong RAM và sẽ ghi đè PlayerPrefs. Lúc Play chỉ mở popup, nhận quà và tua giờ; tua giờ dùng chung
  `SystemClockService.DebugOffset` với Life Debug, nên **tim cũng hồi theo**.

**Bắt đầu từ**: `Core/GameLogic/DailyRewardRules.cs`, `Core/UseCases/ClaimDailyRewardUseCase.cs`,
`Game/Views/DailyReward/DailyRewardAutoOpen.cs`.

## Luồng

```
Home Start -> DailyRewardAutoOpen --(mở khoá && còn quà && chưa mời lần này)--> PopupDailyReward
CLAIM -> ClaimDailyRewardUseCase -> IDailyRewardService.RecordClaim   (PlayerPrefs)
                                 -> IDailyRewardGranter.Grant         (quà + resource_earn)
      -> popup của game: ngày thường đợi StampSeconds + CloseDelaySeconds rồi đóng; ngày rương hiện các món
         (quà đã phát) -> COLLECT -> đóng
```

## Trang và trạng thái thẻ

Framework không còn view của popup này: game dựng lại từ demo (xem [stack/popup-behaviours.md](stack/popup-behaviours.md)).
Hai luật dưới đây là phần popup mới phải giữ.

- Mỗi trang là 7 ô: tuần lẻ gồm ô 0–6, tuần chẵn gồm ô 7–13. Trang được hiện là trang chứa **ô sắp nhận**. Nếu hôm nay đã nhận rồi
  thì hiện trang chứa **ô vừa nhận**, để khi vừa nhận xong ngày 7, người chơi vẫn thấy tuần đó được tick đủ thay vì một tuần mới còn trống.
- Hiệu ứng lặp trên thẻ đang nhận được chạy theo giờ không co giãn (popup dừng game) và **dừng ở `OnDisable`**: popup đóng chỉ
  tắt object chứ không huỷ, nên không dừng thì vòng lặp vẫn chạy ngầm.

## Mang sang game khác

- **Dùng nguyên**: `Core/Domain/DailyReward/`, `DailyRewardRules`, `IDailyRewardService`, `IDailyRewardSchedule`,
  `IDailyRewardGranter`, `ClaimDailyRewardUseCase`, `PlayerPrefsDailyRewardService`, `RemoteDailyRewardSchedule`,
  `DailyRewardSettingsSO`, `DailyRewardInstaller`, `DailyRewardAutoOpen`. Popup và art do từng game dựng.
- **Mỗi game tự điền**: JSON mặc định trong `DailyRewardRemoteConfig`, bảng icon trong `DailyRewardSettings.asset` (thêm dòng cho
  từng booster). Chỉ khi quà không phải coin, tim hay booster thì mới cần thay `ShopDailyRewardGranter`.

## Test

- `Tests/EditMode/Core/GameLogic/DailyRewardRulesTests.cs`: sang ngày, vặn giờ, quay vòng, trang và trạng thái thẻ.
- `Tests/EditMode/Core/UseCases/ClaimDailyRewardUseCaseTests.cs` và `DailyRewardPayoutRulesTests.cs` (mọi `BoosterType` map được theo tên).
- `Tests/EditMode/Game/Services/RemoteDailyRewardScheduleTests.cs`: bộ mặc định là một chu kỳ 14 ngày hợp lệ, rương ở đúng ô.
