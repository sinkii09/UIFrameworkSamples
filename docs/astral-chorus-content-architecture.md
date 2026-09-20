# AstralChorus — Kiến trúc content modular

Tài liệu engineering. Thiết kế game ở [astral-chorus-gdd.md](astral-chorus-gdd.md).

**Yêu cầu:** content **ngoài content chính** phải thêm/xoá được **không đụng codebase**, **block được khi
build**, và **chịu được việc thiếu tài nguyên** của chính nó.

---

## 0. Nguyên tắc

> **Core không bao giờ được phép biết tên một pack cụ thể.** Mọi vi phạm đều dẫn tới cùng một hậu quả:
> tắt pack là vỡ build hoặc vỡ save.

Suy ra:

1. Pack là **dữ liệu**, không phải **hành vi**.
2. Core trỏ tới content bằng **string ID**, không bằng object reference.
3. Mọi tra cứu content **fail mềm** — trả `false`, không throw, không null-ref.

---

## 1. Ranh giới pack

```
Assets/UIFramework/Features/AstralChorus/
├─ Scripts/
│  ├─ UIFramework.AstralChorus.asmdef          ← MỘT assembly cho toàn bộ core
│  ├─ Content/                                  ← registry, definition, loader, profile
│  ├─ Logic/  UIFramework.AstralChorus.Logic.asmdef   ← noEngineReferences: true
│  ├─ Bootstrap/ States/ ViewModels/ Views/ Gameplay/
│  └─ Editor/
├─ Resources/
│  └─ ContentPacks/                             ← chỉ chứa manifest asset
└─ Packs/
   ├─ core/            ← content chính
   ├─ tide-banner/
   └─ ember-banner/
```

Mỗi pack là **một folder** chứa đúng **một** `ContentPackManifest`, cộng `Definitions/` và `Art/`.

> **SỬA 2026-09-20 — manifest phải nằm TRONG pack:** `Packs/<packId>/Resources/ContentPacks/<packId>.asset`.
> Bản đầu đặt nó ở một `Resources/ContentPacks/` dùng chung, **ngoài** `Packs/`. Unity gộp mọi thư mục
> `Resources/` nên khoá nạp không đổi, nhưng một manifest dùng chung thì `Resources/` kéo manifest và
> manifest kéo `Entries` ⇒ **definition của MỌI pack vào build bất kể profile bật hay tắt**, tức là
> vô hiệu hoá chính cơ chế block. Nó cũng làm validator rule 4 báo lỗi chính manifest. Đặt trong pack
> thì xoá folder pack là bytes biến mất thật.

**Pack data-only KHÔNG có asmdef.** Asmdef mỗi pack nhân số compile unit mà chẳng mua được gì — core vốn đã
bị cấm tham chiếu type của pack, nên ranh giới assembly không bảo vệ thêm gì. Nó chỉ mua được một chỗ móc
`defineConstraints`, mà repo này đã tỏ ra không tin cậy cơ chế đó (define `ADDRESSABLES` bị gỡ ở d87eceb
chính vì symbol đặt tay không phải bảo đảm). Thêm nữa **không asmdef game nào trong repo đang dùng
`versionDefines`** — không phải convention sẵn có để dựa vào.

### Hai loại pack

Bản đầu của tài liệu này coi pack-có-code là "escape hatch hiếm khi dùng". **Sai.** Khi content modular
chủ yếu là **chế độ chơi** (leo tháp, world boss, võ đài — GDD mục 13), pack-có-code là **đường chính**.
Hai loại pack có hồ sơ rủi ro khác hẳn nhau nên phải tách tên, tách luật:

| | **Data pack** | **Mode pack** |
|---|---|---|
| Chứa | nhân vật, banner, chapter | một chế độ chơi |
| asmdef | **không** | **một**, tại `Packs/<packId>/Scripts/`, reference `UIFramework.AstralChorus` — **không bao giờ ngược lại** |
| Hỏng thì hỏng kiểu gì | thiếu → ô "?" | **chạy sai / ném exception** |
| Save | không sở hữu gì | một **blob mờ** trong key của core (§5) |
| Validate | ID, reference | ID, reference, **+ smoke test vào/ra được mode** |

Nguyên tắc §0 không đổi cho cả hai: **core không bao giờ biết tên một pack cụ thể.**

### `core` là pack đặc biệt

`Packs/core/` đi qua **đúng cơ chế pack** — đường dẫn được thử nghiệm mỗi lần chạy game, nên không bao giờ
mục rữa. Nhưng `core` **không được phép xuất hiện trong `EnabledPackIds`**: loader luôn nạp nó vô điều kiện.
Nếu để nó vào profile thì một lỗi đánh máy trong profile có thể **xoá sạch content chính**.

---

## 2. Định danh & phân giải

ID dạng **`packId:entityId`**, validate `^[a-z0-9_-]+(:[a-z0-9_-]+)+$`, tách ở dấu `:` **đầu tiên**.

> **SỬA 2026-09-20:** regex cũ là `^[a-z0-9_-]+:[a-z0-9_-]+$` — hai đoạn — trong khi chính tài liệu này
> gọi `"ascent:floor:042"` là hợp lệ ở §7. Hai câu đó không thể cùng đúng. Luật nới được chọn để **một**
> validator phủ cả content id lẫn milestone id. Giá phải trả: `"a:b:c"` viết nhầm từ `"a:b"` không bị bắt.
> Cài bằng vòng lặp ký tự chứ không phải `Regex` (không cấp phát, không caveat IL2CPP, `IsValid(null)` không ném).

`string` thuần ở cả save lẫn registry. **Không GUID** — prefab GUID ở repo này hay churn
(`AircraftStrikerSetupWizard` xoá-rồi-tạo-lại prefab làm đổi GUID, phá binding Addressables). **Không enum**
— pattern `array-index == enum value` của AircraftStriker chính là thứ kiến trúc này thay thế.

> **`entityId` là append-only. Không bao giờ được tái sử dụng, kể cả khi entity cũ đã bị xoá.** ID là khoá
> vào save của người chơi. Tái dùng `tide:aria_01` cho một nhân vật khác sẽ **âm thầm gán lại** một nhân vật
> level 60 đã max node sang một nhân vật hoàn toàn khác — một đường vòng qua mặt preserve-and-hide mà không
> có tín hiệu nào. Validator quy tắc 5 chặn việc này.

### Type — tất cả nằm ở core

| Type | File | Vai trò |
|---|---|---|
| `ContentDefinition` | `Content/ContentDefinition.cs` | abstract SO: `Id`, `DisplayNameKey`, `IconAddress` |
| `CharacterDefinition` | `Content/Definitions/` | `: ContentDefinition` |
| `BannerDefinition` | `Content/Definitions/` | `: ContentDefinition` + `UnlockCondition` |
| `ChapterDefinition` | `Content/Definitions/` | `: ContentDefinition` + `DialogueAddress` |
| `GearTemplateDefinition` | `Content/Definitions/` | `: ContentDefinition` — main stat theo slot, pool substat, bonus 2/3 món. **Template là data; instance là save (cuối §5)** |
| `ContentPackManifest` | `Content/ContentPackManifest.cs` | `PackId` + `ContentDefinition[] Entries` |
| `ContentBuildProfile` | `Content/ContentBuildProfile.cs` | `string[] EnabledPackIds` |
| `IContentRegistry` | `Content/IContentRegistry.cs` | tra cứu |
| `ContentPackLoader` | `Content/ContentPackLoader.cs` | dựng registry lúc boot |
| `IContentAssetLoader` | `Content/IContentAssetLoader.cs` | nạp art fail-mềm |
| `ContentFallbacks` | `Content/ContentFallbacks.cs` | SO giữ placeholder của core |

Mọi subclass definition ở core ⇒ **pack chỉ là file `.asset`**, không một dòng code.

`BannerDefinition.UnlockCondition` giữ **luật** mở khoá (clear chapter nào, hạ boss nào — bằng ID chuỗi);
`GachaStateSaveData.UnlockedBannerIds` chỉ giữ **kết quả**. Đừng lẫn hai thứ.

### Hợp đồng registry

```csharp
public interface IContentRegistry
{
    bool TryGet<T>(string id, out T definition) where T : ContentDefinition;
    bool IsPackAvailable(string packId);
    IReadOnlyList<T> All<T>() where T : ContentDefinition;
}
```

Miss → `false` + **một** `Debug.LogWarning` có throttle. **Không bao giờ throw.** Mọi call site bắt buộc
`if (TryGet(...)) { ... } else { bỏ qua }`. Registry dựng **một lần lúc boot** rồi **đóng băng**.

---

## 3. Block khi build

`Resources/AstralChorusContentBuildProfile.asset` — `string[] EnabledPackIds`.

> **Profile lưu ID, tuyệt đối không lưu object reference.** Một `ContentPackManifest[]` trong profile sẽ kéo
> pack đã tắt quay lại build qua asset graph, vô hiệu hoá chính cơ chế block. **Đây là toàn bộ mẹo.**

Luồng: `ContentPackLoader` đọc profile → `Resources.LoadAll<ContentPackManifest>("ContentPacks")` → nạp
`core` vô điều kiện + lọc phần còn lại theo `EnabledPackIds` → dựng dictionary → đóng băng.

Profile gate ở **runtime** ⇒ **Editor Play Mode khớp chính xác build**. Đây là lý do chọn nó thay vì loại trừ
theo folder (phá Editor của chính dev) hay `defineConstraints` (cần asmdef mỗi pack).

**Bytes vẫn nằm trong build.** Chấp nhận có ý thức: 14 nhân vật portrait tĩnh thì vài MB thừa không đáng
đánh đổi lấy việc bật Addressables.

**Phase 2 — loại bỏ bytes.** Chuyển manifest sang Addressables có label, thêm
`Editor/ContentPackBuildPreprocessor.cs` (`IPreprocessBuildWithReport`) đặt
`BundledAssetGroupSchema.IncludeInBuild = false` cho group của pack đã tắt. Preprocessor **chỉ lật một bool**
— không tạo, xoá hay regenerate group/prefab, nên không rebind `m_GUID`. Khôi phục phải trong `try/finally`,
nếu không build hỏng giữa chừng để lại `AddressableAssetSettings.asset` bẩn và dev commit nhầm.

> Phase 2 có hai nợ phải trả trước: Addressables **không được khai báo trong `manifest.json`** (nó chỉ resolve
> gián tiếp qua framework), và loader Addressables của framework còn **3 gap chưa đóng**. Đây là một phase
> riêng, không phải một buổi chiều.

---

## 4. Thiếu tài nguyên

Definition trỏ art bằng **string address**, **không bao giờ** bằng field `Sprite`/`AssetReference` cắt ngang
ranh giới core → pack.

```csharp
public interface IContentAssetLoader
{
    // null = không có. KHÔNG BAO GIỜ throw, TRỪ khi bị huỷ.
    UniTask<T> TryLoadAsync<T>(string address, CancellationToken ct = default) where T : UnityEngine.Object;
    UniTask UnloadAsync(string address, CancellationToken ct = default);
}
```

> **SỬA 2026-09-20 — chữ ký là ASYNC.** Bản đầu viết `bool TryLoad<T>(..., out T)` đồng bộ. Đồng bộ chỉ
> đúng với Resources; dưới Addressables (Phase 2) nó là **lời nói dối**, và sửa về sau là đổi một API
> công khai cho mọi call site. Kèm theo: huỷ (`OperationCanceledException`) **phải bay lên**, không được
> nuốt thành `null` — một màn hình đóng giữa chừng mà bị báo "thiếu asset" sẽ đi vẽ placeholder lên
> một view đã chết. Có `UnloadAsync` vì không có thì mọi asset nạp qua đây rò vĩnh viễn dưới Addressables.

Miss → `false` → call site dùng placeholder từ `ContentFallbacks`. Pack có definition nhưng thiếu art render
ô **"?"**, không throw.

**UI entry point dựng bằng duyệt registry**, không bao giờ từ danh sách hardcode:

- Lưới sưu tầm ← `registry.All<CharacterDefinition>()`
- Danh sách banner ← `registry.All<BannerDefinition>()`
- Bản đồ chapter ← `registry.All<ChapterDefinition>()`

Pack thiếu ⇒ tự khắc ra 0 dòng, không cần dòng code xử lý đặc biệt nào. Vài nút cố định bind vào
`IsPackAvailable(packId)` rồi `SetActive(false)`.

---

## 5. Save

Bảy key, **tất cả thuộc core**. Pack không sở hữu key nào — kể cả mode pack.

| Key | POCO | Giữ gì |
|---|---|---|
| `AstralChorusPlayerContent` | `PlayerContentSaveData` | Nhân vật sở hữu |
| `AstralChorusProgress` | `PlayerProgressSaveData` | Currency, Fragment, vật phẩm, **sổ cái phần thưởng**, tiến độ |
| `AstralChorusGachaState` | `GachaStateSaveData` | Pity, banner đã mở |
| `AstralChorusLoadout` | `PlayerLoadoutSaveData` | Đội hình + vị trí Front/Back |
| `AstralChorusModeState` | `ModeStateSaveData` | Tiến độ từng chế độ chơi, dạng **blob mờ** |
| `AstralChorusGear` | `GearSaveData` | Kho trang bị — **instance có roll** (xem cuối §5) |
| `AstralChorusSettings` | `SettingsSaveData` | Âm lượng, tốc độ text, auto/skip |

### Sổ cái phần thưởng — không có nó thì economy sụp

```csharp
public sealed class PlayerProgressSaveData
{
    public const string SaveKey = "AstralChorusProgress";

    public int AstralShards      { get; set; }
    public int ResonanceDust     { get; set; }
    public int ResonanceCatalyst { get; set; }   // consumable của luật Resonance (GDD §11)

    public Dictionary<string, int> Fragments { get; set; } = new();   // "packId:entityId" -> count
    public Dictionary<string, int> Materials { get; set; } = new();   // vật liệu NÂNG TRANG BỊ (GDD §11),
                                                                      // KHÔNG còn dùng cho lên cấp nhân vật

    // Sổ cái EXACTLY-ONCE. HashSet chứ không List: exactly-once giờ là TOÀN BỘ lập luận an toàn
    // của kinh tế, nên kiểu dữ liệu phải tự cấm trùng thay vì trông vào hàm cấp phát nhớ kiểm.
    // (HashSet<string> round-trip sạch qua Newtonsoft, không dính đa hình.)
    //   "chapter:c01:clear" · "chapter:c01:3star" · "boss:c01:first"
    //   "affinity:<packId>:<entityId>:60"   <- mốc affinity dùng CHUNG sổ cái này
    public HashSet<string> ClaimedRewardIds { get; set; } = new();

    // Họ mốc ĐƠN ĐIỆU gộp về mốc cao nhất: "ascent:floor" -> 42 nghĩa là đã lĩnh tầng 1..42.
    // Xem ghi chú tăng trưởng bên dưới.
    public Dictionary<string, int> ClaimedHighWater { get; set; } = new();

    public Dictionary<string, int> ChapterStars    { get; set; } = new();
    public Dictionary<string, int> BattlesFought   { get; set; } = new();  // cho affinity
    public List<string>            SeenDialogueIds { get; set; } = new();

    public string CurrentChapterId { get; set; }
    public string CurrentNodeId    { get; set; }
}
```

> **Tăng trưởng của sổ cái là vấn đề thật, cùng lý do với trần kho trang bị.** Mốc Ascent là *mỗi tầng một
> id*, nên sổ cái trở thành cấu trúc lớn nhanh nhất trong save — mà `ISaveService` **ghi nguyên khối một
> file JSON**, đúng lập luận đã dùng để bắt buộc trần kho gear. Lối ra không phải là đặt trần (không được
> phép quên một mốc đã lĩnh) mà là **nén**: mốc của Ascent **đơn điệu** — lên được tầng 42 nghĩa là đã qua
> 1..41 — nên cả họ mốc đó gộp về **một con số cao nhất** trong `ClaimedHighWater` thay vì 42 chuỗi.
> `ClaimedRewardIds` chỉ giữ những mốc **rời rạc thật sự** (clear chapter, hạ boss, affinity).

> **`ClaimedRewardIds` là thứ duy nhất giữ cho nguyên tắc "Astral Shard không farm được" đúng.** Không có nó,
> người chơi chơi lại một chapter là nhận lại 10 Shard, và toàn bộ lịch phần thưởng ở GDD mục 3 sụp đổ.
> Cấp phát Shard **phải** đi qua một hàm duy nhất kiểm tra sổ cái trước khi cộng.

### Nhân vật sở hữu — preserve-and-hide

```csharp
public sealed class PlayerContentSaveData
{
    // Pin cứng. KHÔNG dùng key mặc định typeof(T).Name — đổi tên class là mồ côi toàn bộ save.
    public const string SaveKey = "AstralChorusPlayerContent";

    // key = "packId:entityId". Dictionary<string,T> round-trip sạch, không dính đa hình.
    public Dictionary<string, OwnedEntry> Owned { get; set; } = new();
}

public sealed class OwnedEntry
{
    public int  Level         { get; set; }
    public int  Copies        { get; set; }
    public int  UnlockedNodes { get; set; }
    public long AcquiredUtc   { get; set; }
}
```

> **Quy tắc: preserve-and-hide.** Content mồ côi giữ **nguyên văn** trong save, **lọc lúc đọc**. Không bao
> giờ purge — tắt một pack trong đúng một bản hotfix sẽ **xoá vĩnh viễn** những nhân vật người chơi đã tốn
> cả campaign để sưu tầm.

### Ba bất biến

**(1) Pack không bao giờ sở hữu save key, và không bao giờ đăng ký migration.**

`SaveMigrationRegistry` **suy ra** version hiện tại **từ chuỗi migration**, không phải từ khai báo. Pack đăng
ký migration cho key của nó rồi bị gỡ ⇒ version suy ra **tụt xuống dưới** version đã đóng dấu trong file →
`SaveSchemaVersionException` → **hỏng load vĩnh viễn, không cứu được.**

**(2) Schema không được phụ thuộc tập pack đang bật.**

Không POCO subclass theo pack. Ngoài việc phá bất biến (1), nó **âm thầm degrade về base type** vì
`JsonSaveService` **không bật `TypeNameHandling`** — field của lớp con mất sạch mà không báo lỗi.

Không cần túi `Extra` mở rộng: pack là **data-only, không có code**, nên chỉ core mới ghi được vào đó — tức
core phải biết field riêng của pack, **vi phạm thẳng nguyên tắc §0**. Và thêm field vào một POCO vốn đã
**tương thích ngược sẵn** với Newtonsoft (JSON cũ thiếu field → nhận giá trị mặc định), nên việc mở rộng
schema về sau **không cần migration**, miễn là chỉ thêm chứ không đổi tên hay đổi kiểu.

**(3) `LoadAsync` phải nhận nguyên instance deserialize, không copy field ra ngoài.**

Copy sang object khác nghĩa là entry mồ côi không được copy theo, và `SaveAsync` kế tiếp **ghi đè mất chúng**
— purge do tai nạn, đúng thứ preserve-and-hide đang tránh.

Hệ quả bắt buộc: **mọi thao tác ghi đi qua `_data.Owned[id]`**. `OwnedCharacter` trả từ `GetOwned()` là
**read-only view**, không phải thứ ghi ngược lại.

### Pity — toàn cục, ở core

```csharp
public sealed class GachaStateSaveData
{
    public const string SaveKey = "AstralChorusGachaState";

    public int PullsSinceRare   { get; set; }   // soft pity ★4+
    public int PullsSinceAstral { get; set; }   // hard pity ★5

    public List<string> UnlockedBannerIds { get; set; } = new();

    // Focus (GDD §9) — bannerId -> characterId, cả hai dạng "packId:entityId".
    // Phải lưu: Focus là lựa chọn dài hạn của người chơi, mất khi tắt game là mất tiến độ ý định.
    public Dictionary<string, string> FocusByBanner { get; set; } = new();
}
```

**Focus trỏ vào chỗ không hợp lệ** (nhân vật thuộc pack đã tắt, hoặc bậc đó đã sở hữu đủ): **không xoá
entry, chỉ bỏ qua khi roll** — quay về phân bố đều. Xoá là phá preserve-and-hide: bật pack lại thì Focus
cũ phải còn nguyên.

> **Bẫy chết người:** pity khoá theo **banner ID** thì disable pack chứa banner đó làm pity **đóng băng im
> lặng** — mất tiến độ mà không có thông báo, không phát hiện được ngoài việc đọc file save. Pity **phải ở
> core, đếm toàn cục**. Vì banner mở vĩnh viễn và không xoay vòng (GDD mục 2), toàn cục cũng là thiết kế
> đúng chứ không chỉ là né bug.

### Tiến độ chế độ chơi — blob mờ

Mode pack **có tiến độ riêng** (tầng cao nhất của The Ascent, HP còn lại của Standing Colossus, bậc thang
của Echo Duel). Nhưng bất biến (1) cấm pack sở hữu save key. Cách thoát:

```csharp
public sealed class ModeStateSaveData
{
    public const string SaveKey = "AstralChorusModeState";

    // modeId ("packId:entityId") -> JSON do CHÍNH mode serialize. Core KHÔNG parse nội dung.
    public Dictionary<string, string> Blobs { get; set; } = new();
}
```

> **Vì sao blob mờ đúng ở đây, trong khi túi `Extra` đã bị bác bỏ.** `Extra` sai vì data pack **không có
> code** — chỉ core ghi được vào đó, nên core phải biết field riêng của pack, vi phạm §0. Mode pack thì
> **có code**: nó tự serialize và tự deserialize blob của mình, core chỉ cầm một `string`. Phân biệt không
> phải là nguỵ biện — nó nằm đúng ở chỗ "ai là người ghi".

Ba hệ quả, tất cả đều là lợi:

1. **Nguy cơ migration biến mất.** Mode tự version bên trong JSON của nó (một field `v` chẳng hạn). Schema
   của `ModeStateSaveData` mãi mãi là `Dictionary<string,string>`, không bao giờ đổi, nên **không có chuỗi
   migration nào để gãy** khi gỡ mode. Đây chính là cái bẫy `SaveSchemaVersionException` ở bất biến (1),
   và blob mờ đi vòng qua nó hoàn toàn.
2. **Preserve-and-hide miễn phí.** Tắt mode → blob nằm im, không ai đọc. Bật lại → mode tự đọc lại blob
   của chính nó. Core không cần biết gì.
3. **Blob hỏng không giết save.** Mode parse blob của mình trong `try/catch`; hỏng thì mode đó reset về
   trạng thái đầu và ghi log — **chỉ mất tiến độ một mode**, không phải cả file. Core không bao giờ parse
   nên không bao giờ throw vì nó.

Giá phải trả, nói thẳng: **core không thể thanh tra tiến độ mode.** Không có màn hình tổng hợp "bạn đã
leo tới tầng bao nhiêu" dựng từ core, trừ khi mode tự xuất ra một tóm tắt. Chấp nhận — đổi lại là mode
gỡ ra lắp vào không bao giờ làm hỏng save.

> **Một ngoại lệ quan trọng: kỷ lục tầng KHÔNG nằm trong blob.** The Ascent phát Shard theo first-clear
> từng tầng, và Shard là tài sản của core. Nên mốc `"ascent:floor:042"` nằm trong sổ cái của **core**,
> không phải trong blob của mode — cụ thể là trong `ClaimedHighWater` chứ không phải `ClaimedRewardIds`,
> vì họ mốc này **đơn điệu** và được nén về một con số cao nhất (xem phần sổ cái ở trên). Blob chỉ giữ thứ **chỉ mode quan tâm** — đội hình lần chạy
> trước, tầng đang dở, seed.
>
> Ranh giới: **cái gì đổi ra được tài sản của core thì core phải giữ sổ.** Nếu để kỷ lục tầng trong blob,
> gỡ mode rồi lắp lại sẽ cho người chơi **lĩnh lại toàn bộ Shard của mọi tầng** — một lỗ farm Shard vô
> hạn mở ra từ đúng chỗ kiến trúc tưởng là an toàn.

### Đọc

```csharp
// Inner-join Owned với registry. Mồ côi vẫn nằm trong _data, chỉ không lọt ra ngoài.
public IEnumerable<OwnedCharacter> GetOwned()
{
    foreach (var (id, entry) in _data.Owned)
        if (_registry.TryGet<CharacterDefinition>(id, out var def))
            yield return new OwnedCharacter(def, entry);   // read-only view
}

// "N nhân vật đang ẩn do content pack bị tắt" (GDD §13).
public int HiddenCount()
    => _data.Owned.Count(kv => !_registry.TryGet<CharacterDefinition>(kv.Key, out _));
```

Không cần danh sách `KnownPacks` riêng: mọi id trong `Owned` đều đến từ một pack đã từng nạp thành công, nên
một danh sách như vậy **luôn chứa sẵn mọi pack cần hỏi** và không lọc thêm được gì. Phép lọc duy nhất có ý
nghĩa là "hiện tại registry có biết id này không" — đúng thứ `HiddenCount` đang làm.

Bật lại pack ⇒ mọi thứ quay về nguyên vẹn, **không cần một dòng migration nào**.

### Đội hình và cài đặt

```csharp
public sealed class PlayerLoadoutSaveData
{
    public const string SaveKey = "AstralChorusLoadout";

    // teamId -> id nhân vật theo thứ tự vị trí. Index 0 = Front, 1..2 = Back (GDD §6).
    // Slot trống = null; id trỏ vào pack đã tắt vẫn GIỮ NGUYÊN, lọc lúc dựng đội.
    public Dictionary<string, string[]> Teams { get; set; } = new();
    public string ActiveTeamId { get; set; }
}
```

> Đội hình cũng theo **preserve-and-hide**: pack bị tắt thì slot đó coi như trống khi vào trận, nhưng id
> **không bị xoá khỏi save**. Bật pack lại là đội hình cũ trở lại y nguyên. Xoá id ở đây sẽ phá đúng lời hứa
> ở GDD §13 theo một đường mà người chơi chỉ phát hiện ra sau khi đã mất đội hình.

`SettingsSaveData` (`AstralChorusSettings`): âm lượng BGM/SFX, tốc độ text, auto/skip mặc định. Tách khỏi
tiến độ vì nó là thứ **duy nhất nên reset được độc lập** khi người chơi muốn làm lại cài đặt.

### Trang bị — thứ duy nhất được SINH RA, không được viết sẵn

Trang bị (GDD §11, *thiết kế xong, chưa xếp phase*) phá một giả định chạy xuyên toàn bộ kiến trúc này: mọi
content khác là **data do tác giả viết**, nên nó sống trong pack và được tra bằng ID. Một món trang bị thì
được **roll lúc runtime** — nó không thể là `ContentDefinition`.

Ranh giới, và nó sắc:

| | Là gì | Ở đâu |
|---|---|---|
| **Template / bộ** | main stat theo slot, pool substat, bonus 2/3 món | **Pack.** `GearTemplateDefinition : ContentDefinition` |
| **Instance** | roll cụ thể của một món cụ thể | **Save của core.** Không bao giờ ở pack |

```csharp
public sealed class GearSaveData
{
    public const string SaveKey = "AstralChorusGear";
    public List<GearInstance> Items { get; set; } = new();
}

public sealed class GearInstance
{
    public string        Id         { get; set; }   // guid của instance
    public string        TemplateId { get; set; }   // "packId:entityId" -> definition trong pack
    public int           Level      { get; set; }   // 0..9
    public string        MainStat   { get; set; }
    public List<SubStat> SubStats   { get; set; } = new();
    public string        EquippedBy { get; set; }   // "packId:entityId" của nhân vật, hoặc null
}

// Value tính bằng ĐIỂM CƠ BẢN (basis point, 1/100 %). CRIT 250 = +2.50%; SPD 1200 = +12.00%.
// Dùng int + basis point thay vì float: JSON round-trip tất định, không trôi số lẻ giữa các lần save.
public sealed class SubStat { public string Stat { get; set; } public int Value { get; set; } }
```

**`EquippedBy` nằm trên gear, không nhân bản sang `OwnedEntry`.** Một nguồn sự thật duy nhất — hai bảng
cùng ghi "ai đang đeo gì" là hai bảng chắc chắn sẽ lệch nhau, và lệch theo cách không ai phát hiện cho tới
khi người chơi thấy một món được đeo bởi hai nhân vật.

> **Nhưng `EquippedBy` một mình KHÔNG đủ.** Slot nằm trên *template*, không trên instance, nên ràng buộc
> "mỗi nhân vật đeo tối đa một món mỗi slot" **không biểu diễn được trong schema** — nó chỉ sống nhờ kỷ
> luật của code. Trên một file save **không mã hoá, không kiểm tra** (GDD §15), hai món Instrument cùng gán
> cho một nhân vật sẽ **cộng đôi main stat ATK** mà không có gì báo.
>
> Bắt buộc: **chuẩn hoá lúc load** — nhóm theo `(EquippedBy, slot của template)`, giữ món đầu, tháo phần
> còn lại và ghi log. Rẻ, chạy một lần, và biến một lỗi âm thầm thành một dòng log.

> **Trần kho (~200 món) là BẮT BUỘC, và nó là ràng buộc của tầng persistence chứ không phải lựa chọn
> thiết kế.** `ISaveService` ghi **nguyên khối một file JSON**, không ghi từng phần. Một `List<GearInstance>`
> không trần nghĩa là **mọi lần save đều serialize lại toàn bộ kho** — trên mobile, với người chơi đã farm
> vài nghìn món, đó là độ trễ thấy được ở mỗi lần autosave. Trần kho phải đi kèm **auto-salvage theo
> filter**, nếu không nó chỉ là một bức tường chặn người chơi.

Instance mồ côi (template thuộc pack đã tắt) theo đúng **preserve-and-hide** như nhân vật: giữ nguyên trong
save, lọc lúc đọc bằng `_registry.TryGet`, `EquippedBy` giữ nguyên để bật pack lại là về chỗ cũ.

> **Auto-salvage phải bảo vệ theo `EquippedBy != null`, kiểm TRƯỚC khi lọc qua registry.** Nếu "được bảo
> vệ" nghĩa là *đang đeo bởi một nhân vật nhìn thấy được*, thì món đang đeo bởi nhân vật bị ẩn vì pack tắt
> sẽ bị rã **vĩnh viễn** — mất mát không hoàn tác được, đi qua đúng cửa hông mà §5 đã bịt cho đội hình.
> Trần kho là ràng buộc kỹ thuật; nó không được phép trở thành một đường xoá dữ liệu.

### File save hỏng

`ISaveService`: thiếu file → trả `null`; **file hỏng → ném exception**. Offline thì save là **tài sản duy
nhất** của người chơi, nên không được nuốt lỗi và cũng không được tự reset.

Thứ tự: thử `ReadBackupAsync` (`LocalFileStorageBackend` giữ `.bak` xoay vòng) → thành công thì tiếp tục và
báo qua `OnSaveRecoveredAsObservable` → thất bại thì **dừng ở màn hình "dữ liệu hỏng"** với hai lựa chọn rõ
ràng (thử lại / bắt đầu mới, có xác nhận). **Không bao giờ tự động tạo save mới đè lên.**

---

## 6. Hướng phụ thuộc

Core → pack **chỉ** qua `Resources.LoadAll<ContentPackManifest>("ContentPacks")`, lọc theo profile. Core
không nêu tên pack nào.

**Không quét reflection theo type.** Type manifest ở core và được `ContentPackLoader` hard-reference nên
IL2CPP stripping không đụng tới — khác hẳn `UIViewRegistry.AutoRegister()` của framework (quét reflection và
tự nó cảnh báo cần `link.xml`/`[Preserve]`).

Ba việc bắt buộc, dễ quên:

- **Mọi service được inject phải có đúng một constructor, hoặc đánh dấu `[Inject]` lên cái đúng.** VContainer
  chọn **constructor nhiều tham số nhất**, kể cả một ctor phụ thêm vào để test. Repo này đã bị đúng lỗi đó
  một lần, với hàng trăm test xanh trong khi DI ném ở runtime.

- **`UIViewFactory` cấp child container riêng cho mỗi view** ⇒ `AstralChorusLifetimeScope` phải gọi
  `SetScopeContainer(IObjectResolver)` sau `base.Awake()` và `ResetScopeContainer(Container)` trong
  `OnDestroy`. Không gọi thì **ViewModel không thấy service nào của game**.
- **`[Preserve]` cho mọi `UIView<>`/POCO save của core**, không chỉ của pack — auto-registration quét
  reflection nên IL2CPP có thể strip chúng.

### Mode pack đăng ký thế nào

Mode pack expose **một** `ContentPackInstaller : ScriptableObject` (abstract ở core), nạp theo address.
Type đến được qua asset đã serialize nên **sống sót IL2CPP stripping**. Installer đăng ký vào
`IPackModeRegistry` **mutable, sau khi load async** — **không** vào `IContainerBuilder`, để graph
VContainer giữ nguyên tĩnh và `Configure` giữ nguyên đồng bộ.

```csharp
public interface IGameMode                     // mode pack implement
{
    string   Id       { get; }                 // "packId:entityId"
    BattleFormat Format { get; }               // Aria | Gauntlet | Chorus — CORE sở hữu enum này
    UniTask  EnterAsync(CancellationToken ct);
    string   SerializeState();                 // -> blob mờ (§5)
    void     RestoreState(string blob);        // tự try/catch bên trong
}
```

**`BattleFormat` là enum của core và đóng ở Phase 1** (ba giá trị). Đây là toàn bộ giao diện giữa core và
mode: core biết cách *đánh một trận*, mode quyết định *một chuỗi trận có nghĩa gì*. Mode không được tự
định nghĩa format mới — nếu một mode cần luật đánh mới, đó là thay đổi **core**, không phải thêm pack.

> Đây là chỗ dễ trượt nhất về sau. Một mode "chỉ cần khác một chút" ở luật resolve sẽ cám dỗ việc cho pack
> tự chạy vòng lặp trận đấu của nó. Làm vậy là **có hai engine chiến đấu** trong cùng một game, phân kỳ
> âm thầm, và mọi cân bằng ở GDD mục 8 chỉ còn đúng cho một nửa content.

Menu chế độ chơi dựng bằng **duyệt `IPackModeRegistry`** — mode bị tắt tự khắc không xuất hiện, không cần
một dòng code xử lý đặc biệt nào.

---

## 7. Editor validator

`Editor/ContentPackValidator.cs`:

1. ID trùng giữa các pack
2. ID sai định dạng `^[a-z0-9_-]+:[a-z0-9_-]+$`
3. `EnabledPackIds` trỏ tới pack không tồn tại, **hoặc chứa `core`**
4. **Asset phía core tham chiếu trực tiếp asset trong `Packs/`** — vi phạm nghiêm trọng nhất vì nó **âm thầm
   kéo content đã tắt quay lại build**. Phải quét reference thật, không chỉ kiểm ID
5. **`entityId` bị tái sử dụng** — so với một file ID đã cấp phát, append-only, commit vào repo
6. **Mode pack không vào/ra được** — smoke test: `EnterAsync` rồi thoát, `SerializeState` →
   `RestoreState` round-trip. Data pack hỏng thì ra ô "?"; **mode pack hỏng thì treo game**, nên nó cần
   một gate mà data pack không cần
7. **Mốc được báo mà không được khai** — đối chiếu mọi chuỗi truyền vào `ReportMilestone` trong code của
   pack với bảng phần thưởng pack đó khai. Chính sách runtime là "cấp 0, ghi log", nên một tầng quên khai
   sẽ **im lặng không trả gì**; đây là chỗ duy nhất bắt được nó trước khi người chơi gặp
8. **Tổng Shard vượt `MAX_PACK_SHARD`, hoặc tổng Catalyst vượt `MAX_PACK_CATALYST`** — trùng với kiểm lúc
   nạp, nhưng bắt sớm hơn ở Editor. Kiểm **cả hai**: chỉ kiểm Shard là để ngỏ đúng chỗ rò mà việc gỡ
   `GrantCatalyst` vừa bịt

### Ràng buộc kinh tế: làm cho nó không diễn đạt được, đừng chỉ kiểm tra

GDD mục 10 cho phép **The Ascent** phát Shard ở **first-clear từng tầng**, nhưng cấm mọi phần thưởng
**lặp lại** chứa Shard hay Fragment. **Đừng thực thi ranh giới đó bằng validator** — một quy tắc đọc bằng
mắt sẽ bị vi phạm vào tháng thứ sáu bởi một người không đọc tài liệu này.

Thay vào đó: API phần thưởng mà mode nhìn thấy **không có phương thức nào để phát Shard**. Mode chỉ **báo
đã đạt một mốc**; core tra sổ cái rồi mới quyết định cấp gì.

> **`GrantCatalyst` đã bị GỠ (2026-09-20, sim bắt).** Bản trước có nó. Sim: cho mode rơi **0,25 Catalyst
> mỗi lượt** thì ảnh hưởng của giá shop lên thời gian cày **sập 57 lần** — từ 5,7 giờ xuống 0,1 giờ trên
> cùng khoảng giá 600–1.500 Dust — vì ràng buộc Dust chặn trước. Catalyst là phần thưởng **exactly-once**
> y hệt Shard, nên nó đi đường của Shard: qua `ReportMilestone` và sổ cái, không qua một phương thức cấp
> trực tiếp. Cùng một lập luận, cùng một cách thực thi: **làm cho vi phạm không biên dịch được.**

```csharp
public interface IModeRewardSink               // thứ DUY NHẤT mode chạm được
{
    // Mode KHÔNG tự định giá một lượt chạy. Nó báo lượt chạy dài/khó cỡ nào; core tra bảng rồi trả Dust.
    void ReportRunComplete(RunLength length);

    void GrantMaterial(string materialId, int amount);   // chỉ nuôi trang bị — chưa xếp phase

    // Mode KHÔNG phát Shard. Nó chỉ báo mốc — core tra sổ cái, mốc đã lĩnh thì bỏ qua.
    void ReportMilestone(string milestoneId);   // "ascent:floor:042"

    // Cố ý KHÔNG có GrantShard / GrantFragment / GrantCatalyst / GrantDust.
}

// Phần thưởng của một mốc là một KIỂU ĐÓNG, không phải danh sách phần thưởng tự do.
// Đây là chỗ "làm cho vi phạm không diễn đạt được" áp vào dữ liệu, không chỉ vào API.
public readonly struct MilestoneReward
{
    public int Shard    { get; init; }
    public int Catalyst { get; init; }
    // Không có trường Fragment, không có trường Dust, không có túi key-value mở rộng.
}
```

Ba tính chất rơi ra từ đúng một chữ ký hàm:

1. **Chạy lại tầng 42 lần thứ hai mươi không ra Shard** — `ReportMilestone` gọi bao nhiêu lần cũng được,
   sổ cái chỉ cho qua lần đầu. Mode **không cần biết** nó đã lĩnh hay chưa, nên không thể quên kiểm tra.
2. Vi phạm không phải chuyện bị bắt lúc review — nó **không compile**.

#### Ai định giá một mốc — và vì sao bản đầu của mục này SAI

Bản đầu viết: *"bảng `milestoneId → phần thưởng` nằm ở core, nên pack không thể tự định giá"*. **Sai, và
sai theo cách tự mâu thuẫn.** `milestoneId` là `packId:entityId` — tách ở dấu `:` **ĐẦU TIÊN**, phần
còn lại thuộc về pack và được phép chứa thêm `:` (nên `"ascent:floor:042"` hợp lệ: pack `ascent`, entity
`floor:042`) — do **chính mode pack** đặt ra. Core mà
liệt kê được chúng thì core **biết tên pack** — vi phạm thẳng §0, tức là hàng rào đứng trên chính cái nó
định bảo vệ. Còn để pack tự khai giá thì thủng hàng rào kinh tế. Cả hai lối thoát hiển nhiên đều hỏng.

Lối ra: **pack khai phần thưởng như dữ liệu; core không kiểm từng mốc mà kiểm TỔNG, bằng một hằng số không
nhắc tên pack nào.**

| | Ai giữ | Nội dung |
|---|---|---|
| Phần thưởng từng mốc | **Pack**, dạng data trong definition | `"ascent:floor:042"` → 3 Shard, 1 Catalyst |
| **Trần mỗi pack** | **Core**, hai hằng số toàn cục | `MAX_PACK_SHARD` · `MAX_PACK_CATALYST` — áp cho *mọi* pack, không cho pack cụ thể nào |
| Sổ cái đã lĩnh | **Core** | `ClaimedRewardIds` |

`ContentPackLoader` cộng tổng **Shard và tổng Catalyst** mà một pack khai; vượt trần nào thì **từ chối nạp
pack đó và báo lỗi rõ** (không clamp im lặng — clamp im lặng nghĩa là content ship ra với ngân sách khác
thiết kế mà không ai biết). Core vẫn không biết tên pack nào: nó áp **một luật**, không phải một danh sách.

> **Vì sao phải là HAI trần chứ không một.** Bản đầu chỉ có `MAX_PACK_SHARD`, từ hồi Catalyst còn được
> phát qua `GrantCatalyst`. Gỡ `GrantCatalyst` rồi mà để nguyên một trần thì Catalyst chỉ đơn giản đi vòng
> qua cửa mốc: một pack khai 500 mốc mỗi mốc 1 Catalyst là **600k Dust người chơi không phải cày**, và mốc
> trần 22,2 giờ ở GDD mục 10 tự dịch mà không ai sửa một dòng thiết kế nào. Hai tài nguyên exactly-once ⇒
> hai trần. Trần là **luật về tổng**, nên thêm một cái không làm core biết thêm gì về pack.

> **Và trần phải cộng dồn TOÀN CỤC, không chỉ theo từng pack.** Một trần per-pack với N pack cho phép đúc
> N lần trần đó — mà `MAX_PACK_SHARD` buộc phải ≥ 240 vì riêng The Ascent đã khai chừng ấy. `ContentPackLoader`
> vì thế giữ **một tổng đang chạy qua mọi pack đã nạp**, và đối chiếu với ngân sách toàn cục ở GDD mục 10.
> Nó vẫn không cần biết tên pack nào — cộng thì không cần biết đang cộng của ai.

> **Trường hợp `core` vượt trần.** `core` nạp vô điều kiện (§1), nên "từ chối nạp" là vô nghĩa với nó:
> từ chối core là không có game. Với core, vượt trần là **lỗi lúc build** — validator §7 chặn, không phải
> runtime. Đây là lý do luật này cần cả hai chỗ kiểm chứ không chỉ một.

> **Ba cửa, không phải một.** Ba thứ ở trên đóng ba đường khác nhau mà cùng một tài nguyên có thể chui qua:
> `GrantCatalyst` (cửa lặp lại) · `MilestoneReward` là kiểu đóng (cửa dữ liệu — không có trường Fragment
> thì pack không khai được Fragment, khỏi cần trần thứ ba) · `GrantDust` biến mất khỏi sink (cửa Dust, thứ
> quyết định con số 22,2 giờ). Bịt một cửa mà quên hai cửa kia chỉ là đổi cửa.

**Mốc lạ — `ReportMilestone` với id không pack nào khai:**

> **Log warning, cấp 0, tiếp tục chạy.** Không throw, không cấp mặc định. Cấp mặc định là một vòi Shard vô
> hạn mở ra từ một lỗi đánh máy; throw thì một id sai làm chết cả mode. Nhưng "cấp 0" cũng có mặt xấu phải
> biết: **tầng mới thêm mà quên khai phần thưởng sẽ im lặng không trả gì**. Validator (§7) phải đối chiếu
> mọi `ReportMilestone` trong code pack với bảng khai của nó — đây là chỗ bug đó bị bắt, không phải lúc chạy.

---

## 8. Không làm (YAGNI)

DLC tải về · save key hoặc migration theo pack · đồ thị phụ thuộc/version range giữa pack · định danh GUID ·
ký/checksum pack · hot-reload registry · asmdef cho pack data-only · codegen wizard · túi `Extra` mở rộng ·
cây definition đa hình sâu hơn ba subclass đang thực sự cần.

---

## 9. Rủi ro

| # | Rủi ro | Giảm thiểu |
|---|---|---|
| 1 | Ai đó thêm field `Sprite`/`AssetReference` ở asset core trỏ vào pack ⇒ **vô hiệu hoá block** | Validator 7.4 phải quét reference. Rủi ro số một vì nó **im lặng** |
| 2 | `entityId` bị tái dùng ⇒ gán nhầm nhân vật đã max sang nhân vật khác | Validator 7.5 + file ID append-only trong repo |
| 3 | Cấp Shard không qua `ClaimedRewardIds` ⇒ economy sụp | Một hàm cấp phát duy nhất; không ai được cộng `AstralShards` trực tiếp |
| 4 | `ISaveSlotContext` bị đăng ký `Scoped` thay vì `Singleton` | **Runtime không bắt được.** Review tay khi đăng ký DI |
| 5 | (Phase 2) build hỏng giữa chừng để lại `AddressableAssetSettings.asset` bẩn | `try/finally`; validator assert trạng thái sạch |
| 6 | Registry đóng băng lúc boot ⇒ đổi profile phải restart | Chấp nhận. Hot-reload nằm trong danh sách không làm |
| 7 | **Mode pack chạy code ⇒ nó có thể treo hoặc throw**, khác hẳn data pack vốn chỉ có thể thiếu | Smoke test bắt buộc (§7.6); `EnterAsync` bọc try/catch ở phía core, lỗi thì thoát về menu chứ không chết app |
| 8 | Một mode "cần luật đánh hơi khác" ⇒ pack tự chạy vòng lặp trận của nó ⇒ **hai engine chiến đấu phân kỳ âm thầm** | `BattleFormat` là enum đóng của core. Luật đánh mới = thay đổi core, không phải thêm pack |
| 9 | Mode lặp vô hạn phát Shard/Fragment ⇒ sụp toàn bộ kinh tế | `IModeRewardSink` **không có** phương thức để làm việc đó — không compile được, không cần ai nhớ |
