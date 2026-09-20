# AstralChorus — những gì UIFramework CHƯA cung cấp

Đối chiếu `com.sinkii09.uiframework` **v3.2.0** với toàn bộ nhu cầu của AstralChorus
(`astral-chorus-gdd.md` + `astral-chorus-content-architecture.md`).

Mỗi mục có **bằng chứng kiểm chứng được** (đường dẫn file / kết quả grep), không phải phỏng đoán.
Mục nào đã có thì nằm ở §0 để không ai xây lại.

> **Một điều phải nói trước.** Yêu cầu là "mọi thứ game cần đều do framework cung cấp". Phần lớn danh
> sách này hợp lý để đưa vào framework. Nhưng **§2 thì không** — nhồi engine chiến đấu turn-based vào một
> package tên là *UIFramework* sẽ làm mọi project khác dùng nó phải gánh theo, và làm chính nó mất định
> nghĩa. Đề xuất ở §2 là **package thứ hai**, không phải "để game tự lo". Yêu cầu vẫn được thoả: mọi thứ
> đến từ package dùng lại được, chỉ là không phải cùng một package.

---

## 0. Framework ĐÃ có — không xây lại

| Nhu cầu | Đã có |
|---|---|
| Điều hướng, state machine, layer, popup | `UINavigator` · `UIStateMachine` · `NavigationRequestQueue` · `UILayer` |
| Vòng đời game, loading, boot | `GameLifecycleManager` · `LoadingContext` · `SceneLoader` |
| Save POCO + migration + backup + slot | `JsonSaveService` · `SaveMigrationRegistry` · `SaveBackupRecovery` · `ISaveSlotContext` |
| Toast gộp theo key | `INotificationService` · `NotificationKey` |
| Tooltip, kể cả dòng chỉ số | `TooltipService` · `TooltipStatLineView` |
| Chuyển cảnh che màn hình | `ITransitionOverlay` · `TransitionOverlayView` |
| Transition DOTween | `FadeTransition` · `ScaleTransition` · `SlideTransition` · `SequenceTransition` |
| List cuộn ảo **một trục** | `RecyclerView` (+ `CellPool`, `PrefixSumOffsets`) |
| Control cơ bản | `Badge` · `ProgressBar` · `ResourceCounter` · `TabView` · `ToggleGroup` · `Draggable`/`DropZone` · `IconLabel` |
| Nút back Android | `BackButtonRouter` · `NewInputSystemBackButtonSource` |
| Safe area | `ISafeAreaProvider` |
| Gộp binding theo frame | `UIRenderScheduler` · `CoalescedBinding` |
| Sinh code binding từ prefab | `PrefabBindingGenerator` |
| Policy view: cache, evict, preload, backdrop | `UIViewPolicyConfig` · `UIViewCacheSweeper` · `UIViewPreloader` |
| **Format số lớn** | `UIFormatUtils.FormatCompact(long)` — `377000` → `377K`. **Không phải lỗ.** |

---

## 1. Lỗ CHẶN — không có thì không build được game

### 1.1 Lưới nhiều cột (ưu tiên cao nhất)

`RecyclerView` **chỉ một trục**. `ScrollAxis` chỉ có 4 hướng tuyến tính, `PrefixSumOffsets`/`UniformOffsets`
đều là 1-D, `ScrollDirection` không có khái niệm cột. Lưới sưu tầm là **màn hình chính** của game.

Cần bổ sung:

| Hạng mục | Vì sao |
|---|---|
| **Multi-column virtualization** | 14 nhân vật Phase 1 thì `GridLayoutGroup` còn sống được, nhưng lưới trang bị (trần kho ~200 món, mục 11 GDD) thì không |
| **Snapping** | `grep Snap` → chỉ khớp `UIViewPreloader`/`BackButtonRouter`, **không có snapping thật**. Carousel chọn banner và lật trang chi tiết nhân vật đều cần |
| **Filter / sort bar** | Lọc theo hệ · bậc · vai trò · đã sở hữu · đã max. `SetItemCount` + provider cho phép lọc ở phía caller, nhưng **UI thanh lọc và việc giữ cuộn khi đổi filter thì chưa có gì** |
| **Sticky header theo nhóm** | Nhóm theo bậc / hệ |
| **Cell identity ổn định qua filter** | Đổi filter mà cell không nhảy lung tung |

> Rủi ro #3 trong GDD đã gọi đây là *"hạng mục kỹ thuật lớn nhất ngoài combat"*. Xác nhận: đúng.

### 1.2 `IUILoader` không diễn đạt được việc nạp asset không phải Component

```csharp
UniTask<T> LoadAsync<T>(string key, CancellationToken ct = default) where T : Component;
```

Ràng buộc `where T : Component` làm cho việc nạp **ScriptableObject định nghĩa nội dung**, **Sprite chân
dung**, **TextAsset hội thoại**, **AudioClip** trở nên **không diễn đạt được** — không phải khó, mà là sai kiểu.

Content pack **được định nghĩa bằng** việc nạp asset không phải Component theo khoá có tiền tố pack. Đây
không phải tính năng thiếu, đây là **chữ ký cần mở rộng** (thêm `IAssetLoader` riêng, hoặc nới ràng buộc
— cả hai đều là breaking change, nên làm sớm).

Kèm theo: `AddressablesUILoader` còn **3 lỗ chưa đóng** (xem memory `addressables-ui-loader-activation`),
và define `ADDRESSABLES` đã bị bỏ ở commit `d87eceb`. Phase 2 muốn tách pack khỏi bytes của build thì phải
đóng cả ba.

### 1.3 Không có registry định nghĩa nội dung — **XONG 2026-09-20, ở GAME**

> **Quyết định: registry viết trong `Assets/UIFramework/Features/AstralChorus/`, KHÔNG đưa vào
> framework** (user chốt 2026-09-20). Lý do: đúng một consumer thật, mà public API trong một
> package đã publish thì khoá vĩnh viễn — sai hình dạng là phải bump major. Ở tầng game thì
> promote lên framework chỉ là đổi namespace. **Mốc promote: khi game THỨ HAI thật sự cần**
> (ColorStackSort gói level, AircraftStriker định nghĩa wave), không phải khi "trông có vẻ generic".
>
> Đã có: `ContentId` · `ContentDefinition` · `ContentPackManifest` · `ContentBuildProfile` ·
> `IContentRegistry` + `ContentRegistry` · `ContentPackLoader` · `IContentAssetLoader` +
> `ContentAssetLoader` · `AstralChorusLifetimeScope` · validator Editor rule 1–4. 58 test EditMode.
> Sprint này cũng dựng assembly runtime đầu tiên của AstralChorus — trước đó chỉ có `.Logic`.
>
> **Validator rule 5 (ledger chặn tái dùng `entityId`) bị CẮT** (user chốt): ít generic nhất, false
> positive bắt buộc người xác nhận, và hiện có 0 definition để mà tái dùng ID. Mở lại khi có content thật.


Framework **không có gì** cho: kiểu definition nền, manifest của pack, phân giải `packId:entityId`, kiểm
thiếu phụ thuộc, và hợp đồng *tắt-pack-không-vỡ-save*. `UIViewRegistry` chỉ quản khoá **view**, không phải
dữ liệu game.

Toàn bộ `astral-chorus-content-architecture.md` mô tả một hệ thống framework hiện **không có**. Đây là
phần lớn thứ hai sau lưới.

### 1.4 Không có autosave, không có hook vòng đời ứng dụng

```
grep -r "OnApplicationPause|OnApplicationQuit|OnApplicationFocus" Runtime/  ->  0 kết quả
grep -r "AutoSave" Runtime/                                                ->  0 kết quả
```

Trên mobile app bị giết **không báo trước**. Game gacha offline mất một lần 10-pull là mất vĩnh viễn —
không có server để đối chiếu. Cần:

- **Save khi pause** (`OnApplicationPause(true)` là điểm duy nhất chắc chắn chạy trên Android/iOS)
- **Bộ lập lịch save có debounce/coalesce** — `ISaveService` ghi **nguyên khối cả file JSON**, nên save
  mỗi lần đổi một field là ghi lại toàn bộ tiến độ
- **Chốt chặn "đang save"** lúc thoát, để không cắt ngang một lần ghi

Rủi ro #5 trong GDD đã nêu. Xác nhận: đúng, và nặng hơn mô tả vì ràng buộc ghi-nguyên-khối.

### 1.5 Không có UX phục hồi save hỏng

`ISaveService` **ném exception** khi file hỏng (thiếu file mới trả `null`). Tầng lưu trữ đã có
`SaveBackupRecovery` và `SaveRecoveredEventArgs`, nhưng **không có view/luồng nào** cho *"save của bạn hỏng,
đây là phần khôi phục được, tiếp tục hay chơi lại?"*.

Không có màn hình này thì hành vi mặc định là **crash lúc khởi động**. Rủi ro #11 GDD.

### 1.6 Không có âm thanh — bất kỳ thứ gì

```
grep -r "IAudio|AudioSource" Runtime/  ->  0 kết quả
```

Animation pull không có tiếng thì không ship được. Cần: BGM crossfade theo game state, bus SFX, lưu âm
lượng (dùng `ISaveService` sẵn có), ducking khi Ultimate.

### 1.7 Không có object pool

Pool **đã bị gỡ khỏi framework** (memory `uiframework-removed-systems`). Số sát thương bay lên, VFX trúng
đòn, hạt của animation pull — đều cần. AircraftStriker đã phải tự dựng lại pool phía game; đó là bằng
chứng nó nên nằm ở framework.

### 1.8 `INotificationService` không có API hoãn

API hiện tại đúng ba phương thức: `Notify` · `Dismiss` · `DismissAll`.

GDD §14 đã chỉ ra va chạm: layer `Notification` = **275**, `Overlay` = **300** ⇒ toast phần thưởng render
**bên dưới** animation pull. Luật cần là *"hoãn toast khi có Overlay đang hiện, xả hàng đợi khi đóng"* —
hiện phải tự làm **ở từng chỗ gọi**, nghĩa là chắc chắn sẽ quên ở một chỗ nào đó.

Cần: `Suspend(reason)` / `Resume(reason)` có đếm tham chiếu, hoặc để chính service theo dõi layer Overlay.

---

## 2. Lỗ chặn nhưng **KHÔNG nên nằm trong UIFramework**

Đề xuất: **package thứ hai**, ví dụ `com.sinkii09.turnbattle` + `com.sinkii09.dialogue`. Vẫn là "framework
cung cấp", chỉ không phải package UI.

### 2.1 Không có gì cho chiến đấu turn-based

Framework thuần UI. Thiếu toàn bộ: bộ lập lịch lượt theo SPD, **giải quyết lựa chọn đồng thời** (Aria bắt
buộc AI chốt trước khi thấy input người chơi), engine buff/debuff có thời hạn, pipeline công thức sát
thương, và **event đổi phase bắn đúng một lần** (GDD §7 nói rõ: poll thì mất sạch cơ hội animation/thoại).

GDD trỏ tới `BossController` của AircraftStriker làm tiền lệ — đó là **code game**, không phải framework.

### 2.2 Không có hệ hội thoại

GDD §12, rủi ro #4. Cần: định dạng kịch bản, typewriter, đổi chân dung/biểu cảm, tên người nói, auto/skip,
và sổ cái dòng đã đọc (`SeenDialogueIds` đã có chỗ trong save).

### 2.3 Không có asmdef `Logic` tách khỏi engine

`UIFramework.ColorStackSort.Logic.asmdef` tồn tại nhưng ở **tầng game**, không phải framework. Cả sim kinh
tế (rủi ro #7) lẫn toàn bộ toán chiến đấu đều cần `noEngineReferences: true` để test không cần Unity.
Đây là **mẫu asmdef + template**, không phải code — rẻ, nên làm sớm.

---

## 3. Cố ý KHÔNG làm

- **Localization** — GDD §15 đã đưa ra ngoài phạm vi Phase 1. Đừng xây.
- **Mã hoá save** — GDD §15, quyết định có ý thức.
- **Bất cứ thứ gì cần server** — cloud save, tài khoản, PvP.

---

## 4. Thứ tự đề xuất

Sắp theo **cái gì chặn cái gì**, không theo độ khó.

| # | Hạng mục | Vì sao ở vị trí này |
|---|---|---|
| 1 | **§2.3 mẫu asmdef `Logic`** | Rẻ nhất, mở khoá sim kinh tế — mà sim là việc đầu tiên của Phase 1 |
| 2 | **§1.2 `IUILoader`** | **Breaking change**. Làm càng muộn càng đắt. Chặn §1.3 |
| 3 | **§1.4 autosave + hook vòng đời** | Red zone (persistence). Không có nó thì mọi playtest đều mất dữ liệu |
| 4 | ~~**§1.3 registry nội dung**~~ | **XONG 2026-09-20** — ở tầng game, không phải framework |
| 5 | **§1.1 lưới + filter + snap** | Hạng mục to nhất; bắt đầu sớm nhưng không chặn cái khác |
| 6 | **§1.5 UX save hỏng** | Nhỏ, nhưng không có thì crash lúc khởi động là hành vi mặc định |
| 7 | **§1.6 audio**, **§1.7 pool**, **§1.8 hoãn toast** | Độc lập nhau, làm song song được |
| 8 | **§2.1 combat**, **§2.2 dialogue** | Package riêng, khối lượng lớn nhất, nhưng không chặn §1 |

---

## Câu hỏi chưa giải quyết

1. ~~**§1.2 nới `where T : Component` hay thêm `IAssetLoader` riêng?**~~ **Đã chốt v3.3.0:** thêm
   `IAssetLoader` riêng, `LoadAssetAsync` (không phải overload — ràng buộc generic không thuộc chữ ký
   method trong C#). §1.3 là consumer thật đầu tiên của nó.
2. **§2 có thực sự tách package không?** Nếu giữ trong `com.sinkii09.uiframework` thì mọi project khác
   (AircraftStriker, ColorStackSort, MemoryGame) phải gánh theo engine chiến đấu chúng không dùng.
3. **§1.1 tự viết hay tìm package có sẵn?** Lưới ảo hoá có filter là bài toán đã được giải nhiều lần.
   Chưa khảo sát.
4. **§1.7 pool đã bị gỡ một lần** — cần biết **vì sao gỡ** trước khi đưa lại, nếu không sẽ lặp lại đúng
   lý do đó.
