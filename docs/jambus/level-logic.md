# Jam Bus: logic level

Tài liệu này mô tả cách bản Jam Bus: Sandfall 37.0 (Cocos2d-x, `libMyGame.so` armeabi-v7a) ghép **layout box** với **tranh cát** và chia màu cát cho từng xe khi vào một level. Mục đích là để dựng lại logic đó trong Unity. Dữ liệu nằm ở `Assets/_Dev/Data/Level/`. Bản cài đặt tham chiếu chạy được là `docs/jambus/car_allocator_ref.py`.

## Tóm tắt

- **Một level gồm 2 file độc lập:** layout box `MoreTopSpace/Level_XXX.json` và tranh cát `SandPic/<bản>/SandPic_XXX.json`.
  - Không file dữ liệu nào ghi level nào đi với tranh nào. Việc ghép do code quyết định, theo số level và **nhóm A/B (G)** mà server gán cho người chơi.
  - Bảng ghép đã tính sẵn nằm ở `LevelMap.json`.
- **Màu `color` của box trong file level không phải màu cuối cùng.** Khi vào level, game **chia lại màu cho mọi xe theo lượng cát từng màu trong tranh**: xe to nhận màu nhiều cát, số cát mỗi xe phải hút tỉ lệ với `bulletCount`.
  - Nếu chỉ đọc màu từ file level rồi đem so với tranh thì sẽ không khớp. Đây là lý do.
- **Tổng cát luôn được bảo toàn:** với mỗi màu, tổng `amount` của các xe mang màu đó đúng bằng số hạt cát màu đó trong tranh.
  - Số hạt của một màu = số pixel × `sands`², với `sands` = 2.
- **Xe ra được ngay phải chở màu của phần cát bị lấy trước** (bắt đầu từ hàng `height-1` của tranh):
  - Xe có `realOutStep` = 1 phải đủ màu cho 10% cát dưới cùng (front).
  - Xe có `realOutStep` = 2 phải đủ màu cho phần 10–30% tiếp theo (mid).
  - Game chỉ **hoán đổi** cặp (màu, lượng) giữa các xe, không chia lại lượng.
- **Level 1:** xe đầu tiên bị ép nhận màu chiếm nhiều nhất trong phần front (dùng cho tutorial).
- **Người chơi bị đánh dấu "special area"** (`UserGroupMgr::isMarkedAsSpecialAreaUser`) sẽ:
  - nhận các cặp (màu, lượng) **xáo ngẫu nhiên** giữa các xe, không qua bước front/mid;
  - không dùng hậu tố thư mục tranh nào, tức luôn dùng tranh bản `Default`.
- **Nếu bước chia màu thất bại** (ví dụ số xe ít hơn số màu), game chuyển sang đường dự phòng: chỉ đổi màu những box có màu không có trong tranh. Chi tiết ở mục 6.

## 1. Dữ liệu

### Layout box: `MoreTopSpace/Level_001..200.json`

Đây là file gốc `configs/level{id}-上方保留更多空间.json`, giữ nguyên từng byte.

| Field | Ý nghĩa / giá trị quan sát được trên 200 file |
|---|---|
| `LevelInfo.SlotCount` | Luôn là 6: số ô đỗ phía dưới. |
| `LevelInfo.UnlockSlot` | 0 hoặc 2: số ô bị khoá, mở bằng quảng cáo hoặc coin (chưa xác minh). |
| `LevelInfo.GameAreaHeight` | 1000 hoặc 990. |
| `boxInfos[]` | Các xe trên bàn, mỗi xe có các field bên dưới. |
| `boxInfos[].type` | 0, 1 hoặc 2, tương ứng 1-1 với `bulletCount` 4, 6, 10 (cỡ xe). |
| `boxInfos[].bulletCount` | **Sức chứa** của xe. Đây là trọng số khi chia cát. |
| `boxInfos[].color` | 1–9: màu khởi tạo. Bước chia màu sẽ ghi đè màu này. |
| `boxInfos[].posX/posY` | Toạ độ của xe trong vùng chơi. Gốc toạ độ và điểm neo chưa xác minh. |
| `boxInfos[].angle` | Góc xoay của xe. |
| `boxInfos[].direction` | Hướng xe chạy ra, gồm 8 hướng (0..7): 0 ↔ 0°, 4 ↔ 180°, 2 ↔ −90°, 6 ↔ 90°, các số lẻ là hướng chéo. |
| `isFrozen`, `isColorful`, `keyId`, `lockId` | Luôn false hoặc 0 trong bản này, nhưng code vẫn hỗ trợ (xem `realOutStep`). |
| `conveyorInfos` | Chỉ 3 level có dữ liệu. Các list còn lại (`caveInfos`, `bigIceInfos`, …) đều rỗng. |

`Group42/` và `Group48/` là các biến thể box mà **bản 37.0 không còn dùng tới**, nên có thể bỏ qua.

### Tranh cát: `SandPic/<bản>/SandPic_XXX.json`

```json
{ "1": { "name": "...", "difficulty": 1, "capacity": 5,
         "sands": { "width": 128, "height": 96, "sands": 2, "data": [ /* width*height mã màu */ ] },
         "spools": [] } }
```

- `data[y*width + x]` là mã màu của pixel. Giá trị 0 hoặc nhỏ hơn nghĩa là ô trống.
  - Game coi hàng `height-1` là phần cát được lấy trước (front) và đọc ngược lên tới hàng 0. Hàng `height-1` có nằm ở đáy khi vẽ hay không thì chưa xác minh trên màn hình.
- `sands`: mỗi pixel là một khối `sands × sands` hạt.
- Kích thước tranh là 128×96. Bản 3D và `MultiColor256` dùng 256×192.

### Bảng màu: `SandPic/lsMeta.json`

- `global.colors` cho biết mã màu → hex.
- `groups[G].colors`, nếu có, **thay thế** bảng màu `global` cho nhóm G. Các nhóm 18, 19, 20 có 12 màu, trong đó mã 4 có màu khác với bản global.
- Mã màu sau cùng của xe chính là mã màu cát, nên xe được tô bằng `colors[mã]`.

## 2. Chọn file cho level N

Code tham chiếu: `/Users/akira4968/APK/work_jambus/tools/jambus_level_map.py`. Kết quả đã tính sẵn cho 19 nhóm × 230 level nằm ở `LevelMap.json`.

- **Box:** M = 200 nếu G = 42, M = 48 nếu G = 22, các nhóm khác M = 25.
  - Nếu N ≤ M thì id = N.
  - Nếu N > M thì id = (N − 6) mod (M − 5) + 6, tức vòng lặp từ level 6 đến M.
- **Tranh:** số tranh T = 230 cho G ∈ {29, 42, 43}, T = 100 cho G ∈ {15, 16}, các nhóm khác T = 225.
  - id = (N − 1) mod T + 1.
  - Thư mục tranh (`Default`, `Group18`, `Group25`, `3D`, …) chọn theo G và theo id; quy tắc nằm trong `pic_file()`.
- **Gợi ý:** dùng profile của nhóm 42. Đây là nhóm duy nhất dùng đủ 200 layout box và 230 tranh.

## 3. Chuỗi xử lý khi vào level N

Hàm gốc là `SandBusGameManager::createGame`. Các bước theo thứ tự:

1. `GameManager::createGame(N)`: dựng các box từ file layout.
2. `parseLevelConfigData`: đọc tranh và bảng màu ở mục 1–2.
3. `setSandFillTileSands(sands)`.
4. **`_applyDynamicCarAllocation(tranh)`**: thuật toán chính, mô tả ở mục 4.
5. Chỉ chạy nếu bước 4 trả về false: `_applyParkingBoxSandColorIds`, rồi `_bindParkingBoxesToSandLevelColors` (mục 6).
6. `SandUpperController::init`: dựng phần cát phía trên, rồi bắt đầu `onUpdate`.

## 4. Thuật toán chia màu cát cho xe

Bản port chạy được là `car_allocator_ref.py`; bản Unity nên port từ file đó. Phần mô tả dưới đây đi theo đúng thứ tự trong code gốc.

**Chuẩn bị** (`_applyDynamicCarAllocation`):

1. **Gộp màu trùng RGB:** các mã màu có cùng hex trong bảng màu được gộp về mã nhỏ nhất, và lưới tranh được viết lại theo mã đã gộp.
2. **Đếm cát:** `counts[màu] += sands²` cho mỗi pixel có màu.
3. **Tính front/mid** (`_computeFrontMidSandCounts`):
   - Duyệt pixel từ hàng đáy lên trên, mỗi hàng từ trái qua phải; mỗi pixel lặp lại `sands` lần.
   - `front` = số hạt theo màu trong 10% đầu của chuỗi (phần cát bị lấy trước); `mid` = số hạt theo màu trong đoạn 10–30%.
   - Hai mốc 10 và 30 là giá trị mặc định của `GameTuningParams::frontSegmentPercent` và `midSegmentPercent`.
4. **Danh sách xe:** mỗi box có `bulletCount ≥ 1` được đưa vào với `{id, cap = bulletCount, step = realOutStep}`.
   - Nếu box chưa có id thì id = thứ tự của box + 1.
   - Cách tính `realOutStep` ở mục 5.
5. **Ép xe đầu (chỉ level 1):** nếu đang ở level 1 và `front` có dữ liệu, gán `forced = (id của xe đầu danh sách, màu có nhiều hạt nhất trong front)`.

**`SandBusCarAllocator::allocate`:**

1. **Kiểm tra đầu vào.** Gặp một trong các lỗi sau thì trả về thất bại và chuyển sang mục 6:
   - `no sand colors`
   - `fewer vehicles than sand colors`
   - `invalid sand color count`
   - `no sand`
   - `invalid vehicle boxId`
2. **Sắp xếp:**
   - Xe: theo `cap` giảm dần, rồi `id` tăng dần.
   - Màu: theo lượng cát giảm dần, rồi mã màu tăng dần.
   - Mỗi màu có `ideal = tổngCap × cát_màu / tổngCát`.
3. **Mỗi màu nhận trước một xe:** xe thứ i nhận màu thứ i, nên xe to nhất nhận màu nhiều cát nhất.
4. **Các xe còn lại**, lần lượt theo thứ tự đã sắp, mỗi xe đi vào màu có `ideal − assigned` lớn nhất.
   - Hai màu được xem là bằng nhau khi chênh lệch ≤ 1e-6. Khi bằng nhau, ưu tiên màu nhiều cát hơn, rồi mã màu nhỏ hơn.
5. **Chia lượng cát của một màu cho các xe mang màu đó:**
   - Mỗi xe nhận `floor(cát × cap / tổngCap)`, tối thiểu 1.
   - Nếu tổng vượt lượng cát của màu: lấy bớt từng hạt, mỗi lần từ xe có lượng lớn nhất (khi bằng nhau thì xe có cap lớn hơn).
   - Phần dư chia lần lượt cho các xe, theo phần lẻ giảm dần, rồi cap giảm dần, rồi id tăng dần.
6. **Hoán đổi theo tầng ra xe** (người chơi thường, hàm `FUN_0130f6e8` và `FUN_0131282c`):
   - Chia xe thành 3 tầng theo `step`: tầng 1 (step = 1), tầng 2 (step = 2), và tầng còn lại. Step ≤ 0 được tính là 999.
   - **Front:** duyệt các màu của `front` theo lượng giảm dần. Với mỗi màu cần `need` hạt, nếu tổng `amount` của màu đó trong tầng 1 chưa đủ thì:
     - **Xe cho:** các xe đang mang màu đó ở tầng 2 hoặc tầng còn lại, lấy xe có `amount` lớn trước.
     - **Xe nhận:** xe ở tầng 1 mang màu khác và có `amount` nhỏ nhất, nhưng không lấy xe mà nếu đổi sẽ làm một màu front đã xử lý trước đó rơi xuống dưới mức cần.
     - Hoán đổi cặp (màu, amount) giữa xe cho và xe nhận, lặp lại tới khi đủ.
     - Xử lý xong thì màu đó được ghi vào danh sách màu front cần giữ.
   - **Mid:** làm giống front, nhưng tầng đích là tầng 2 và xe cho lấy từ tầng còn lại rồi đến tầng 1.
7. **Người chơi special area:** bỏ qua bước 6. Thay vào đó, các cặp (màu, amount) được xáo Fisher–Yates giữa các xe, dùng `mt19937` với seed từ `/dev/urandom`.
8. **Áp dụng `forced`:** nếu xe bị ép chưa mang đúng màu, hoán đổi nó với xe đang mang màu đó có `amount` gần với nó nhất.
9. **Ghi kết quả vào box:** `box.sandColorId = color` và `box.sandAmount = max(0, amount)`.
   - `_applyDynamicCarAllocation` kiểm tra lại tổng theo từng màu trước khi ghi; nếu lệch thì không ghi gì.

Ví dụ: chạy với 3 màu (40, 30, 10 hạt) và 5 xe có cap 4, 4, 6, 10, 4 cho kết quả:
- xe cap 10 mang màu 1, 29 hạt; xe cap 4 (id 5) mang màu 1, 11 hạt;
- xe cap 6 mang màu 2, 18 hạt; xe cap 4 (id 2) mang màu 2, 12 hạt;
- xe cap 4 (id 1) mang màu 3, 10 hạt.

Có thể chạy lại ví dụ bằng hàm `allocate` trong `car_allocator_ref.py`.

## 5. `realOutStep`: cần bao nhiêu xe rời đi thì xe này mới ra được

Hàm gốc là `CCubeBox::calculateRealOutStep`, gọi từ `OperationLayer::calculateAllBoxesMoveSteps`.

- **Xe chặn:** `findRealHitBoxes` trả về các xe có hình chữ nhật va chạm (OBB) nằm trên **đường chạy thẳng của xe theo `direction`** cho tới khi ra khỏi bàn.
- **`realOutStep` = số phần tử của tập {chính nó} ∪ toàn bộ xe chặn, tính bắc cầu.**
  - Xe không bị chặn có `realOutStep` = 1.
  - Xe bị một xe tự do chặn có `realOutStep` = 2.
- **Giá trị đặc biệt:**
  - 99: xe bị khoá, đóng băng, hoặc bị khối băng (`CBigIce`) chưa vỡ đè lên.
  - 999: các xe chặn nhau thành vòng.
- **Unity cần tự tính giá trị này** bằng hệ va chạm của mình. Kích thước va chạm thật của từng `type` xe chưa được dịch ngược.
  - Script tham chiếu đặt mọi step = 1 khi tự kiểm tra, nên bước 6 của thuật toán gần như không đổi gì trong lần chạy đó.

## 6. Đường dự phòng khi `allocate` thất bại

Phần này chỉ chạy khi `allocate` thất bại; hàm tương ứng trong script là `fallback_bind`.

1. `_applyParkingBoxSandColorIds`: mỗi box có màu bị trùng RGB với màu khác được đổi về mã nhỏ nhất của nhóm trùng.
2. `_bindParkingBoxesToSandLevelColors`:
   - Lấy danh sách các màu có trong tranh, sắp tăng dần.
   - Lấy các box có màu **không** nằm trong danh sách đó, sắp theo mã màu tăng dần.
   - Box thứ i trong danh sách thứ hai nhận màu thứ i trong danh sách thứ nhất; box thừa giữ nguyên màu.

## 7. Chưa dịch ngược (cần tự thiết kế khi làm trong Unity)

- **Phần cát phía trên** (`SandUpperController`, `SLSandPaintingAdapter`): cách cát rơi, cách xe hút cát, `suctionRowCount`, `collectionInterval`, …
  - Các key này có trong parser nhưng file tranh không chứa chúng, nên game dùng giá trị mặc định nằm trong code.
- **Băng chuyền và hàng đợi** (`BusQueue::BQGameSystem`, `队列游戏配置*.json`, `关卡队列路径线*.json`). Các file này chưa được export.
- **Kích thước va chạm** của xe theo `type`; booster/prop (`prepareProp1Use`, `reshuffleBoxColors`); hồi sinh (`startRespawnAbsorption`).
- **Remote config:** có thể đã ghi đè `frontSegmentPercent`/`midSegmentPercent` và nhóm A/B. Bản phân tích này chỉ dùng giá trị mặc định có sẵn trong binary.

## 8. Kiểm chứng

- **Ghép level ↔ tranh:** mô phỏng nhóm 0–60 × level 1–400 (48.800 lần mở file) thì không thiếu file nào.
  - Thử cố ý làm sai: lệch id 1 đơn vị thì thiếu 96 file; bỏ vòng lặp box thì thiếu 12.200 file.
- **Thuật toán chia màu** (`python3 docs/jambus/car_allocator_ref.py`): 4.370 level (19 nhóm × 230) đều qua.
  - Điều kiện qua: mỗi màu được bảo toàn đúng tổng cát, mỗi xe nhận ≥ 1 hạt, và mỗi box có đúng một kết quả.
  - Thử cố ý làm sai bằng cách bỏ bước chia phần dư: 4.370/4.370 level báo lỗi.
- **Lưu ý:** điều kiện bảo toàn chỉ chứng minh bản port nhất quán với chính nó. Nó **không** chứng minh từng tie-break giống hệt game gốc.
  - Các tie-break được đọc từ mã decompile (Ghidra 12.1.4) của những hàm liệt kê ở mục 9.
  - Muốn khớp tuyệt đối thì phải so với log chạy thật trên máy, việc này chưa làm.

## 9. Nguồn (địa chỉ trong `libMyGame.so`)

| Hàm | Địa chỉ |
|---|---|
| `SandBusGameManager::createGame` | 0x12f9720 |
| `GameManager::createGame` | 0x146c050 |
| `SandBusGameManager::_applyDynamicCarAllocation` | 0x12fcee0 |
| `SandBusGameManager::_computeFrontMidSandCounts` | 0x12fd794 |
| `SandBusCarAllocator::allocate` | 0x12fe6f8 |
| Hoán đổi theo tầng front/mid | 0x12ff6e8, 0x130282c |
| `_applyParkingBoxSandColorIds` / `_bindParkingBoxesToSandLevelColors` | 0x12fca28 / 0x12fcb68 |
| `CCubeBox::calculateRealOutStep` | 0x145356c |
| `sl::SLConfigService::parseLevelConfigData` | 0x144d684 |

Mã decompile nằm ở `/Users/akira4968/APK/work_jambus/out/decomp/`. Tên mỗi file là địa chỉ cộng 0x10000, vì Ghidra nạp thư viện ở image base đó.
