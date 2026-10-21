# Thêm một hệ thống mới

Công thức nối một tính năng mới (service + lưu dữ liệu + UI) vào project, theo đúng quy ước đang chạy.
Thêm vào hệ thống **đã có** thì không cần file này — mở doc của hệ thống đó. `<Game>` là namespace
prefix trong project card của `CLAUDE.md`; đường dẫn tính từ root cũng ghi ở đó.

## Tóm tắt

- Đường đi cố định: `interface` ở `Core/Services` → implement ở `Game/Services` → asset
  `XSettingsSO` ở `Game/Data` → đăng ký ở `LifetimeScope` → view `[Inject]`.
- State đổi thì bắn `event Action<T>`; **view nghe sự kiện, không poll**.
- Lưu dữ liệu: `PlayerPrefsXService`, `const string PrefsKey = "<Game>.X"`, giữ state trong RAM,
  ghi + `PlayerPrefs.Save()` ngay lúc đổi.
- Mỗi `[SerializeField]` asset trên scope phải có `[Tooltip]` **nói rõ cái gì hỏng nếu bỏ trống**, và
  code phải chạy được khi nó null — thiếu asset thì tính năng đó tắt, **không làm văng cả scene**.
- Chọn sai kiểu `Register` hỏng **im lặng** hoặc làm vỡ cả container — xem bảng quyết định bên dưới.
- Dùng ở từ hai scene trở lên → tách thành extension installer, đừng chép tay.

**Mẫu để chép**: hai file ở hàng *Reference implementations* của project card trong `CLAUDE.md`.

## Sáu bước

### 1. Interface ở `Core/Services/`

```csharp
namespace <Game>.Core.Services
{
    // Một câu nói service này giữ cái gì, và ai lưu nó là việc của Game.
    public interface IDailyRewardService
    {
        int Streak { get; }
        bool TryClaim();

        // Bắn ra giá trị MỚI sau mỗi lần đổi — view chỉ cần nghe, không phải poll mỗi frame.
        event Action<int> StreakChanged;
    }
}
```

Core **không** được biết PlayerPrefs, `MonoBehaviour`, hay ScriptableObject nào. Cần đọc giờ thì
inject `IClockService` — đó là chỗ **duy nhất** trong game gọi `DateTime.UtcNow`, và nhờ vậy luật
test được bằng giờ giả.

Luật thật (tính streak, điều kiện nhận) đi vào `Core/GameLogic/` dưới dạng hàm **thuần**, hoặc
`Core/UseCases/` nếu là một hành động trọn vẹn. Service chỉ giữ state và lưu.

### 2. Implement ở `Game/Services/`

```csharp
public sealed class PlayerPrefsDailyRewardService : IDailyRewardService
{
    public const string PrefsKey = "<Game>.DailyStreak";

    public int Streak { get; private set; }
    public event Action<int> StreakChanged;

    public PlayerPrefsDailyRewardService(IClockService clock)
    {
        Streak = Mathf.Max(0, PlayerPrefs.GetInt(PrefsKey, 0));
    }

    void SetStreak(int value)
    {
        Streak = value;
        PlayerPrefs.SetInt(PrefsKey, value);
        // Save ngay thay vì đợi Unity ghi lúc thoát: game mobile bị kill nền bất cứ lúc nào.
        PlayerPrefs.Save();
        StreakChanged?.Invoke(value);
    }
}
```

Quy ước: key luôn `"<Game>.<Tên>"`; state giữ trong RAM (HUD đọc mỗi frame không tốn gì); chỉ ghi
đĩa lúc đổi; kẹp trần/sàn **trong service** (`Add` bỏ qua số <= 0, `TrySpend` trả `false` thay vì trừ
lố) để chỗ gọi không phải tự kiểm tra.

### 3. Asset thông số ở `Game/Data/`

```csharp
[CreateAssetMenu(menuName = "<Game>/Daily Reward Settings", fileName = "DailyRewardSettings")]
public sealed class DailyRewardSettingsSO : ScriptableObject
{
    [SerializeField, Min(0)] int coinsPerDay = 50;
    public int CoinsPerDay => coinsPerDay;
}
```

Con số chỉnh được **không bao giờ** hard-code trong scene hay trong view.

### 4. Đăng ký ở `LifetimeScope`

```csharp
[Tooltip("Thông số phần thưởng hằng ngày. Bỏ trống thì không phát thưởng, scene vẫn chạy bình thường.")]
[SerializeField] DailyRewardSettingsSO dailyRewardSettings;

protected override void Configure(IContainerBuilder builder)
{
    builder.Register<IDailyRewardService>(
        resolver => new PlayerPrefsDailyRewardService(resolver.Resolve<IClockService>()),
        Lifetime.Singleton);
}
```

**Tooltip là bắt buộc và phải nói hậu quả cụ thể**, vì người kéo asset vào là designer chứ không phải
người viết code. Xem các field có sẵn trong các `LifetimeScope` của game để lấy giọng.

### 5. View

View nằm sẵn trong scene thì nhận DI bằng **method injection**:

```csharp
[Inject]
public void Construct(IDailyRewardService daily) => this.daily = daily;

void OnEnable()  => daily.StreakChanged += Redraw;
void OnDisable() => daily.StreakChanged -= Redraw;
```

View **chỉ đọc state và vẽ**, gọi use case khi có input. Không viết luật trong `MonoBehaviour`.

State **riêng của view/presenter** (không thuộc service nào) thì dùng R3 `ReactiveProperty<T>` và gắn subscription vào
`destroyCancellationToken` bằng `.RegisterTo(...)`. Interface ở `Core/Services` vẫn giữ `event Action<T>`.

### 6. Test

`Scripts/Tests/EditMode/Core/` — test phần thuần ở Core với service giả. Chạy:

```bash
unity command run_tests --mode EditMode
```

Cuối cùng: viết `docs/<tên>.md` theo [doc-template.md](doc-template.md) (mở đầu bằng khối `Tóm tắt`)
và thêm một dòng vào bảng tra trong `CLAUDE.md`.

## Bảng quyết định: dùng kiểu `Register` nào

| Tình huống | Viết | Hỏng thế nào nếu chọn sai |
|---|---|---|
| Service C# thuần, không tham số | `Register<IFoo, Foo>(Lifetime.Singleton)` | — |
| Service cần asset hoặc service khác | `Register<IFoo>(r => new Foo(r.Resolve<IBar>(), settings), Lifetime.Singleton)` | — |
| Một instance phục vụ **nhiều** interface | `Register(...).As<IA>().As<IB>()` | đăng ký hai lần = **hai** instance, state tách đôi |
| Vừa là entry point vừa cần resolve trực tiếp | `Register<T>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf()` | `RegisterEntryPoint` thường thì `Resolve<T>()` không ra |
| Asset **có thể bỏ trống** | `Register(_ => settings, Lifetime.Singleton)` | `RegisterInstance(null)` → container dựng hỏng |
| Asset chắc chắn có (đã guard `if != null`) | `RegisterInstance(settings)` | — |
| View **luôn có mặt** trong scene | `RegisterComponentInHierarchy<T>()` | resolve **ngay lúc build container** — scene thiếu object đó là **văng cả container** |
| Component **có thể vắng** trong scene | field `[SerializeField]` + `if (x != null) builder.RegisterComponent(x)` | dùng `...InHierarchy` cho thứ tuỳ chọn = scene nào bỏ nó đi là vỡ |
| Orchestrator C# thuần (`IStartable`/`ITickable`/`IDisposable`) | `RegisterEntryPoint<T>()` | — |
| Cần truyền prefab/transform vào entry point | `RegisterEntryPoint<T>().WithParameter(prefab).WithParameter<Transform>(container)` | — |
| Phải chạy **ngay khi mở scene**, không đợi ai inject | thêm `RegisterBuildCallback(c => c.Resolve<IFoo>())` | lazy → nhạc nền im tới nhát chém đầu, preload pool dồn vào lúc đang chơi |
| Component **không ai resolve** nhưng cần `[Inject]` chạy | `RegisterComponentInHierarchy<T>()` **+** `RegisterBuildCallback(c => c.Resolve<T>())` | `[Inject]` **không bao giờ chạy**, dependency null im lặng |

Ba điểm hay dính:

- **`RegisterComponentInHierarchy` resolve ngay lúc `LifetimeScope.Awake()`.** Đó là lý do
  `PopupHost` và các component tuỳ chọn khác được đăng ký **theo reference** kèm guard `!= null`:
  scene nào không có chúng vẫn chạy.
- **Thứ tự đăng ký có lúc quan trọng.** Service nào view đọc ngay trong `Construct` (ví dụ tiến độ
  màn để biết dựng model nào) phải đăng ký **trước** view đó.
- **Prefab dựng lúc runtime phải đi qua `resolver.Instantiate`** (`PopupHost` dựng popup, view dựng
  piece). `Object.Instantiate` thì `[Inject]` bên trong prefab im lặng là null.

## Khi nào tách installer dùng chung

Dùng ở **từ hai scene trở lên** → viết extension trong `Game/Installers/`:

```csharp
public static class DailyRewardInstaller
{
    const int DefaultCoinsPerDay = 50;   // dùng khi scene quên gán asset

    public static void RegisterDailyReward(this IContainerBuilder builder, DailyRewardSettingsSO settings)
    {
        builder.Register<IDailyRewardService>(...);
        builder.RegisterEntryPoint<DailyRewardTicker>();
        if (settings != null) builder.RegisterInstance(settings);
    }
}
```

Chỉ phần **riêng của từng scene** mới ở lại scope của scene đó — như hệ tim: Home có cổng chặn nút
Play, MainGame có trừ/hoàn tim.

Đừng chép tay khối đăng ký sang scene thứ hai: hệ tim đã từng bị chép tay và **hai bản trôi khỏi
nhau** trước khi được gom lại.

Nhắc lại vì đây là nguồn bug khó thấy nhất: **scope bị huỷ hoàn toàn khi rời scene**, nên scene sau
phải tự đăng ký lại tất cả. Không có scope toàn cục nào cả. Thứ phải sống xuyên scene (audio,
transition) đi đường `static` + `DontDestroyOnLoad`, dựng lười — xem `AudioHost`,
`SceneTransitionProvider`.

## Checklist

- [ ] Interface ở `Core/Services`, implement ở `Game/Services`, luật thuần ở `Core/GameLogic`/`UseCases`
- [ ] `Core` không `using UnityEngine.*` (ngoài kiểu toán), không VContainer, không LitMotion
- [ ] Namespace khớp thư mục (`<Game>.Core.*`, `<Game>.Game.*`)
- [ ] Đổi state thì bắn `event Action<T>`
- [ ] `PrefsKey` theo mẫu `"<Game>.<Tên>"`, `PlayerPrefs.Save()` ngay lúc đổi
- [ ] Mỗi asset field có `[Tooltip]` nói cái gì hỏng khi bỏ trống, và code chạy được khi null
- [ ] Chọn đúng kiểu `Register` (bảng trên); cần chạy ngay thì thêm `RegisterBuildCallback`
- [ ] Dùng ở nhiều scene thì tách extension installer
- [ ] Enum nào asset lưu theo số thì giá trị mới **nối vào cuối**
- [ ] Có test EditMode cho phần Core
- [ ] `unity command run_tests --mode EditMode` xanh, **kể cả** `Tests/EditMode/Architecture/` — không thêm dòng vào `ArchitectureBaseline` để qua test
- [ ] Viết `docs/<tên>.md` với khối `Tóm tắt`, thêm dòng vào bảng tra trong `CLAUDE.md`
