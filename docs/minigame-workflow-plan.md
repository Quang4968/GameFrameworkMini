# Plan: workflow `/minigame` cho TikTok Mini Game

Mục tiêu: dựng một game hoàn chỉnh từ asset có sẵn, nhanh nhất và cần ít input nhất. Người dùng chỉ
tham gia ở bước lên plan; sau khi chốt plan, hệ thống chạy loop tự động tới bản build TikTok.

## 0. Việc cần làm trước bản build TikTok đầu tiên

- **AppId:** nhập trong `TikTokGame/Build Minigame` (hiện chưa có `StarkBuilderSettings`).
- **SdkSettings:** tạo qua cửa sổ SDK settings (`Assets/SDKSetting/`), điền ad unit rewarded và
  interstitial.
- **Các mục FIX dung lượng:** IL2CPP OptimizeSize, thêm `link.xml` trước khi nâng stripping
  lên Medium, bỏ post-process data trên renderer, dùng `Mobile_RPAsset`, template `PROJECT:Portrait`.

## 1. Một skill duy nhất: `/minigame`

Gồm `.claude/skills/minigame/SKILL.md`, cùng `reference/` (mỗi stage một file) và `scripts/` (quét
asset, quản lý state). Skill chạy qua 3 phase.

### Phase 0: kiểm tra asset (tự động, dừng nếu thiếu art)

- **Bắt buộc:**
  - Art gameplay.
  - **UI demo cho từng màn hình** (`Textures/UI/<Feature>/*demo*`): Home, HUD, Win, Lose, Out of
    lives, Settings… vì framework không còn UI nào.
  - Sprite mà các demo dùng: nút, khung popup, icon coin/tim/booster.

  Script `asset_scan.py` liệt kê và phân loại asset, đối chiếu demo với danh sách màn hình trong
  `popup-behaviours.md`, và in ra những gì còn thiếu rồi dừng.
- **Tùy chọn:** GDD (`docs/<game>/gdd.*`) và SFX (`Audio/`).
  - Không có SFX: gắn các `AudioCue` có sẵn của framework vào clip mặc định nếu có, nếu không thì để
    im và ghi vào report.
  - **VFX:** project không còn vendor pack VFX nào (Epic Toon FX, JMO, Free Slash không có ở đây).
    VFX dựng bằng Particle System trên sprite của game, hoặc lấy từ pack mà game tự thêm vào.

### Phase 1: lên plan (lần duy nhất cần người dùng tham gia)

- Đọc GDD. Nếu không có GDD thì suy ra từ art. Chỉ đặt câu hỏi mở khi còn mơ hồ (core loop,
  win/lose, số level, độ dài session, đường cong độ khó).
- Viết `docs/<game>/build-plan.md` gồm các mục:
  1. Gameplay spec
  2. Level format và generator
  3. Cân bằng độ khó: win-rate mục tiêu theo level, các metric mô phỏng, sai số cho phép
  4. UI: danh sách màn hình, mỗi màn một demo, theo hợp đồng trong `popup-behaviours.md`
  5. Bảng SFX/VFX: sự kiện → cue/effect → asset
  6. Tutorial: các bước và `E_TutorialId`
  7. SDK: `MB_TIKTOK`, ad unit, các event tracking qua `ITrackingService`
  8. Monet: chỉ rewarded (revive / booster / x2 coin / thêm tim) và interstitial (cap, cooldown,
     level bắt đầu). Không có IAP, nên phải chốt luôn việc khi người chơi thiếu coin thì mời xem
     rewarded hay làm mờ nút.
  9. Build: ngân sách dung lượng gói, kèm các mục FIX dung lượng
- **Gom mọi input và quyền ngay trong phase này:**
  - **Input:** appid, ad unit id.
  - **Quyền:** tạo `SdkSettings`, commit, chạy build, sửa `ProjectSettings` qua `eval` (các mục FIX
    dung lượng, template Portrait).

  Khi người dùng duyệt, skill ghi một lần vào `settings.local.json` và `Library/Toolkit/unlock`.
- Duyệt xong thì ghi `Library/Toolkit/minigame/state.json`. Repo không có git: phase này hỏi luôn có
  `git init` và commit mốc đầu tiên không.

### Phase 2: loop tự động, không hỏi thêm

1. Core gameplay (logic thuần trong Core, kèm test)
2. View gameplay và nối `GameplayBridge` (`MainGameLifetimeScope.RegisterGameplay`)
3. Generator, simulator, cân bằng: mô phỏng và chỉnh tham số tới khi đường cong độ khó nằm trong sai
   số, xuất dữ liệu level và report
4. UI từ demo cho mọi màn hình (Home, HUD, Win, Lose, Out of lives, Buy booster, Settings…): tự
   duyệt spec, tự so sánh ảnh chụp với demo
5. SFX/VFX
6. Tutorial
7. SDK và tracking (TikTok: `MB_TIKTOK`, các event qua `ITrackingService`)
8. Monet: placement rewarded và interstitial theo plan
9. Smoke test trong Play mode
10. Size audit: áp dụng các mục FIX, rồi `build-tiktok`
11. Report cuối

Quy tắc của loop:

- Mỗi stage kết thúc bằng một cổng kiểm tra: compile, EditMode test, giới hạn 300 dòng. Qua cổng
  thì tự commit stage đó, nếu repo có git.
- **Mọi `unity command` đều kèm `--project-path`** và kiểm tra `projectPath` trước lệnh có thay đổi.
  Máy thường mở nhiều Editor cùng lúc (project này dùng port 7801).
- **Cơ chế loop:** thêm Stop hook `minigame-continue.sh`. Khi state còn stage chưa xong, hook trả
  lượt về kèm lệnh "tiếp stage X".
- Mỗi stage được thử lại tối đa 3 lần. Nếu vẫn fail thì ghi blocker vào report và chuyển sang stage
  kế tiếp.
- Gặp lựa chọn mà plan chưa có thì tự chọn mặc định hợp lý và ghi lại vào
  `docs/<game>/decisions.md`.

## 2. Gỡ bớt hook và quy trình

| Thay đổi | Hook / skill | Lý do |
| --- | --- | --- |
| **Bỏ** | `supervisor-events.sh`, `remind-tests.sh`, `track-edits.sh`, agent `supervisor` | Hai hook đầu trả lượt về để hỏi nên làm gãy loop. Cổng kiểm tra trong loop thay chỗ cho chúng. `track-edits.sh` chỉ phục vụ supervisor. |
| **Bỏ** | `/lead`, `/design` | `/minigame` thay thế chúng. Bước chạy blind lanes chậm. |
| **Giữ** | `protect-paths.sh` | Vẫn bảo vệ SDK/vendor, mở được theo quyền cấp ở Phase 1. Hook chặn cả lệnh shell chỉ nhắc tới đường dẫn được bảo vệ hoặc `.meta`, nên script nên viết ra file riêng. |
| **Giữ** | `check-file-length.sh` | Chỉ góp ý, không chặn. |
| **Giữ** | `editor-lock.sh` | Chỉ chặn khi có session khác dùng Editor. |
| **Giữ** | `session-start.sh` | |
| **Giữ** | `/task`, `/verify`, `/build-tiktok`, `/ui-from-demo`, `/new-popup`, `/playable-ads` | `/minigame` gọi lại các skill này bên trong. `/task` để sửa nhỏ bằng tay. |

**Thay đổi trong rules:**

- `workflow.md`: một lần duyệt `/minigame` áp dụng cho cả pipeline.
- `ui-from-demo.md`: trong `/minigame`, spec và so sánh với demo được tự duyệt. - `unity-editor-cli.md`: bắt buộc `--project-path` cho mọi `unity command`.
- `commits.md`: tự commit theo từng stage. Repo không có `develop`, nên branch cắt từ nhánh có sẵn hoặc
  từ mốc sau `git init`.
- `arch-reviewer`: chỉ chạy một lần ở cuối pipeline thay vì sau mỗi lần verify.

**Cập nhật thêm:** `CLAUDE.md` (dòng tra cứu `/minigame`), tạo doc mới `docs/stack/minigame-pipeline.md`.

## 3. Điểm cần chốt (mặc định là phương án đề xuất)

1. **Git:** thư mục này không có `.git`, nên không cắt branch hay hoàn tác được. Mặc định: `git init`,
   commit mốc hiện tại, rồi làm workflow trên `feat/minigame-workflow`. Phương án khác: tiếp tục không
   có git.
2. **Mô phỏng cân bằng:** mặc định viết bằng C# chạy headless trong EditMode, dùng lại logic trong Core.
   Phương án khác là Python, như `docs/jambus/car_allocator_ref.py`.
3. **Hook và skill bị bỏ:** mặc định xóa hẳn. Phương án khác là chỉ gỡ khỏi `settings.json` và giữ lại
   file.
4. **Loop:** mặc định dùng Stop hook để chạy tiếp. Phương án khác là `/loop` tự điều nhịp.
5. **Thiếu coin khi không có IAP:** mặc định mời xem rewarded để nhận coin (một placement riêng); khi
   chưa có quảng cáo thì làm mờ nút. Phương án khác: chỉ làm mờ nút.
