# Cấu trúc code của stack

Đọc file này khi cần biết **ranh giới các lớp, DI theo scene, chuyển cảnh, pooling, hoặc file mới đặt
ở đâu**. Sửa trong một hệ thống có sẵn thì không cần: mở thẳng doc của hệ thống đó (bảng tra trong
`CLAUDE.md`). Muốn **công thức dựng một hệ thống mới** thì đọc [adding-a-system.md](adding-a-system.md).
Luật cấm nằm ở `.claude/rules/`, không lặp lại ở đây. Phần riêng của từng game (scene, level data,
installer nào gọi ở đâu) nằm ở `ARCHITECTURE.md` của game đó.

`<Game>` là namespace prefix, và mọi đường dẫn tính từ root — cả hai ghi trong project card của
`CLAUDE.md` (template mặc định: root `Assets/_Dev/`, prefix `Game`).

## Tóm tắt

- Hai assembly runtime: `<Game>.Core` thuần C# (chỉ thêm `UnityEngine.CoreModule` cho kiểu toán),
  `<Game>.Game` là mọi thứ chạy trên Unity. `Game` biết `Core`; `Core` **không bao giờ** biết `Game`.
- Mỗi scene một `LifetimeScope`, **huỷ hoàn toàn** khi rời scene, không có scope toàn cục. Service
  dùng ở nhiều scene gom vào `<Feature>Installer` (extension method), không chép tay.
- Component có mặt ở nhiều scene (`PopupHost`) không được nhận service riêng của một scene: thiếu
  một đăng ký là **cả container** của scene đó dựng hỏng, trông như lỗi nút bấm.
- Chuyển cảnh cắm bằng decorator quanh `ISceneLoader` (`TransitionSceneLoader`); chỗ gọi chỉ biết
  interface.
- Pool Boss bọc sau `IObjectPoolService`, dựng từ `PoolCatalogSO` bằng `RegisterObjectPool` +
  `RegisterBuildCallback`; prefab quên khai catalog vẫn chạy nhưng rơi về `Instantiate` kèm cảnh báo.

**Bắt đầu từ**: `Game/SceneFlow/{ISceneLoader,TransitionSceneLoader}.cs`,
`Game/Services/{IObjectPoolService,PoolBossObjectPoolService}.cs`, `Game/Views/Popup/PopupHost.cs`.

## Hai lớp

- **Core** = logic thuần C#, không đụng `MonoBehaviour`/`GameObject`/`Transform`.
- **Game** = mọi thứ chạy trên Unity (MonoBehaviour, hiển thị, input, animation, DI wiring).

```
<Game>.Core        <-- references: [] (chỉ UnityEngine.CoreModule cho Vector2Int/Vector3Int/Mathf)
     ^
<Game>.Game         -- Core, UniTask, VContainer, LitMotion, InputSystem, Addressables,
     ^                 TextMeshPro, DarkTonic.PoolBoss, MoodBoost.Sdk.* (facade, haptics included)
<Game>.Editor       -- Core + Game, chỉ chạy trong Editor
<Game>.Core.Tests   -- Core, NUnit EditMode (không cần mở scene)
```

R3 và UniTask là NuGet/Plugins auto-referenced nên không xuất hiện trong asmdef; test kiến trúc vẫn
bắt được vì soi metadata của assembly đã biên dịch.

## Thư mục

| `Core/` | Chứa gì |
|---|---|
| `Domain/` | Entity, value object thuần; thư mục con cho một họ domain (`Domain/Obstacles/`) |
| `GameLogic/` | Luật chơi, thuật toán — hàm thuần, test được không cần Unity |
| `UseCases/` | 1 hành động nghiệp vụ trọn vẹn (`SpendLifeOnLevelStartUseCase`) |
| `Services/` | Interface, KHÔNG chứa implementation (`ILifeService`, `IClockService`) |

Không có lớp state riêng. State màn chơi là chính các entity ở `Domain/` (view cầm tham chiếu trực
tiếp); state lâu dài (tim, coin, cài đặt) nằm sau interface ở `Core/Services/` và báo đổi bằng
`event Action<T>`. R3 chỉ dùng cho state phía `Game` (view/presenter), **không bao giờ** ở `Core` hay
trong chữ ký của `Core/Services` — xem [reactive-and-motion.md](reactive-and-motion.md).

| `Game/` | Chứa gì |
|---|---|
| `Views/<Feature>/` | View của một tính năng: gameplay, popup, UI ngoài gameplay, transition |
| `Animation/` | Hiệu ứng và tween dùng chung (`ButtonPressEffect`, effect prefab) |
| `Services/` | Implementation của `Core/Services`, cộng interface phải nói bằng kiểu Unity (`IObjectPoolService`) |
| `SceneFlow/` | Tên scene, loader, transition (`SceneLoader`, `TransitionSceneLoader`) |
| `Boot/` | Entry point của từng scene (`BootstrapRunner`, `IBootStep`) |
| `Splash/` | Scene Splash — cố ý không phụ thuộc gì, chạy trước cả Boot |
| `Installers/` | `LifetimeScope` của từng scene + `<Feature>Installer` dùng chung |
| `Data/` | `ScriptableObject` — dữ liệu, KHÔNG chứa logic (`*SettingsSO`, `PoolCatalogSO`) |

## Scene và boot

Scene đầu (`Splash`) không có `LifetimeScope`: hiện logo rồi tự chuyển. Scene `Boot` có
`BootLifetimeScope` chạy các `IBootStep` theo `Order` (init SDK, nạp dữ liệu) rồi mới sang scene
chính. Mỗi scene chơi có scope riêng và một entry point `IStartable` (`*Bootstrap`). Tên scene gom ở
một chỗ (`GameScenes`); danh sách thật của từng game ở `ARCHITECTURE.md` của game.

## Đổi scene và DI theo scene

Mỗi scene có `LifetimeScope` riêng, và scope bị **huỷ hoàn toàn** khi rời scene. Ba hệ quả:

- Service nào scene sau vẫn cần thì scene sau **phải tự đăng ký lại**. Hệ nào dùng ở từ hai scene
  gom vào `<Feature>Installer` để hai scene không chép tay rồi lệch nhau.
- `CancellationToken` lấy từ scope hoặc từ object trong scene **tắt ngay giữa lúc đổi scene**. Việc
  phải sống qua ranh giới scene thì không được nghe token đó — xem `TransitionSceneLoader`.
- Object phải sống xuyên scene (transition, audio) không nhờ container giữ được. Cách đang dùng:
  `static` + `DontDestroyOnLoad`, dựng lười ở lần cần đầu tiên (`SceneTransitionProvider`, `AudioHost`).

Transition cắm bằng **decorator ở tầng `ISceneLoader`**, không gọi tay ở từng chỗ:

```
nơi gọi (nút Play, popup Win/Lose, BootstrapRunner) -- chỉ biết ISceneLoader
      v
TransitionSceneLoader   hiện màn loading -> gọi tiếp -> giữ 1 nhịp -> tắt màn loading
      v  bọc quanh              v
   SceneLoader           ISceneTransition <- LoadingTransitionView
```

Đổi kiểu chuyển cảnh = implement `ISceneTransition` mới + đổi prefab trong
`SceneTransitionSettingsSO`. Không đụng chỗ gọi.

### Component dùng chung nhiều scene

`PopupHost` là ví dụ: cùng một component ở nhiều scene, mỗi scene một instance, nhưng các scene
**không đăng ký cùng một bộ service**.

1. **Không được nhận service riêng của một scene.** Thêm dependency vào nó là bắt mọi scene có nó
   phải đăng ký service đó. Buộc phải thêm thì rà lại **toàn bộ** `LifetimeScope` đang đăng ký nó.
2. **Dependency của một popup khai ở controller của popup đó**, không đẩy lên host. Host dựng popup
   bằng `resolver.Instantiate` để container tự rót `[Inject]`.
3. **Service dùng chung gom vào một installer.** Khối nào còn copy-paste ở nhiều scope là nợ: đừng
   nhân bản thêm.

Vì sao nghiêm: `RegisterComponent` resolve ngay lúc `LifetimeScope.Awake()`, nên thiếu một đăng ký là
**cả container của scene đó dựng hỏng** — entry point không chạy, UI bấm không ăn, trông như lỗi nút
bấm chứ không lộ ra là lỗi DI.

## Object pooling

Hiệu ứng ngắn hạn (mỗi frame một `GameObject` kèm `TrailRenderer`/`ParticleSystem`) là nguồn rác lớn
nhất của màn chơi. Pool Boss lo phần đó, nhưng nó vốn là **singleton scene** (`static PoolBoss.X` +
một object đặt sẵn trong Hierarchy). Cách đang dùng: bọc sau service, cho container quản đời, scene
không mang theo object nào.

```
PoolCatalog.asset      prefab nào được pool, preload bao nhiêu, trần bao nhiêu
      v  kéo vào field của
ObjectPoolInstaller.RegisterObjectPool   Register + RegisterBuildCallback (dựng ngay), gọi ở mọi scene cần pool
      v
PoolBossObjectPoolService   tự AddComponent<PoolBoss> lúc sinh, tự Destroy lúc Dispose
      v  implement
IObjectPoolService (Game/Services)   IsReady / Contains / Spawn / Despawn
      v  [Inject] vào
effect component      tới được vì view dựng prefab bằng resolver.Instantiate
```

Interface ở `Game/Services` chứ không phải `Core/Services` vì nó nói bằng `GameObject`/`Transform`.

- **Thứ tự dựng host là bắt buộc**: `SetActive(false)` **trước** `AddComponent<PoolBoss>()`.
  `AddComponent` chạy `Awake` ngay, mà `Awake` tự `Initialize()` lúc danh sách còn rỗng → Pool Boss
  kẹt vĩnh viễn ở trạng thái "đã init nhưng chưa ready", không cứu được. Cùng lý do: catalog rỗng thì
  **không** gọi `Initialize()`.
- **Prefab quên khai trong catalog vẫn chạy**, chỉ rơi về `Instantiate`/`Destroy` kèm đúng một cảnh
  báo cho cả game. Mất hiệu ứng vì lỗi cấu hình còn tệ hơn tốn rác.
- Scene khác cần pool thì **phải tự đăng ký lại** service, với catalog riêng (scene nhẹ dùng catalog
  nhỏ hơn để khỏi preload hiệu ứng nặng).

Khuôn của một prefab hiệu ứng (`TimedDespawner`, animation chạy trên chính object hiệu ứng,
`OnSpawned`/`OnDespawned`): [reactive-and-motion.md](reactive-and-motion.md).

## Vì sao tách vậy

- `Core` không phụ thuộc Unity → test NUnit thuần, chạy vài giây, không cần PlayMode
  (`unity command run_tests --mode EditMode`).
- Đổi UI/animation không đụng luật chơi và ngược lại — review PR biết ngay PR đụng logic hay chỉ visual.
