# AstralChorus — Game Design Document

**Status:** thiết kế Phase 1, chưa implement · **Platform:** Mobile portrait 1080x1920
**Pacing:** turn-based · **Framework:** com.sinkii09.uiframework v3.2.0
**Kiến trúc content:** [astral-chorus-content-architecture.md](astral-chorus-content-architecture.md)

---

## 1. Premise

Bạn là người **triệu hồi** — thu thập các Chorister, chiến đấu theo lượt, mở dần những banner mới bằng
tiến độ chứ không bằng tiền.

Game **offline hoàn toàn**. Không server, không tài khoản, không IAP. Toàn bộ state nằm trong máy người chơi.

Ba format chiến đấu, cùng một engine:

- **Aria** — 1v1. Đấu tay đôi, ăn nhau ở **đọc ý** đối thủ.
- **Gauntlet** — Bo3. Ba hiệp Aria nối tiếp; HP mang sang, hoán đổi giữa các hiệp.
- **Chorus** — 3v3. Đội hình, vị trí, và **cộng hưởng** giữa các nhân vật.

Tên game là ẩn dụ có nghĩa thật: Aria là độc tấu, Chorus là hợp xướng. Ba format không khác nhau ở *luật*,
mà khác ở **lớp tài nguyên** đang hoạt động — mục 5 và 6.

## 2. Vòng lặp tổng

```
Thu thập nhân vật  →  Build đội theo synergy  →  Clear content  →  Mở banner mới  →  Thu thập...
```

Mắt xích quan trọng nhất là **"mở banner mới"**. Banner không xoay vòng theo thời gian như gacha thương mại
— game offline không có lý do gì để ép người chơi vội. Banner **mở khoá bằng tiến độ và mở vĩnh viễn**.

Hệ quả: banner là **phần thưởng của campaign**, không phải cửa hàng. Và vì banner là một tập con roster
theo chủ đề, nó khớp thẳng với đơn vị content modular:

> **Một content pack = một banner + roster subset + chapter + encounter.**
> Không phải trùng hợp — đây là lý do kiến trúc pack khả thi. Xem tài liệu kiến trúc.

## 3. Vì sao gacha offline vẫn có sức hút

Bài toán trung tâm, phải giải trước mọi thứ khác.

> Gacha lấy dopamine từ **sự khan hiếm**. Trong game offline không bán gì, khan hiếm là **tuỳ tiện**:
> farm thoải mái thì pull vô nghĩa, siết chặt thì thành chặn cửa vô lý vì người chơi không có đường mua
> để thoát ra.

**Lời giải: tách "sở hữu" khỏi "hoàn thiện", và đặt bức tường ở giữa hai cái đó.**

> **Ngân sách do tác giả rải — một mình nó — đã bảo đảm sở hữu đủ 14 nhân vật.**
> **Farm chỉ đẩy nhanh và đào sâu. Farm không bao giờ là điều kiện để CÓ một nhân vật.**

- **Astral Shard** đến từ hai nguồn, và cả hai đều **phát đúng một lần**:
  **(a)** content campaign chơi một lần — **180**, đủ để sở hữu trọn roster;
  **(b)** **first-clear từng tầng The Ascent** — cày được, nhưng cày bằng *độ sâu* chứ không bằng *thời gian*.
- **Resonance Dust** farm tự do, tiêu cho nâng cấp và shop.
- **Pity** vẫn có nghĩa đúng: **sàn công bằng trên phần ngân sách bảo đảm**.

Vì sao "cày bằng độ sâu" mới là mấu chốt: đánh lại một tầng đã qua **không ra Shard**. Muốn thêm Shard thì
phải **đi sâu hơn**, mà đi sâu hơn cần roster mạnh hơn và rộng hơn. Vòng lặp tự khoá vào nhau:

```
pull  →  roster mạnh hơn  →  Ascent sâu hơn  →  thêm Shard  →  pull
```

Không có chỗ nào để idle-farm, không cần stamina, không cần đồng hồ. Người chơi cày **bằng kỹ năng và
chiều sâu đội hình**, đúng thứ game này muốn thưởng.

Khác biệt cố ý lớn nhất so với gacha thương mại:

> **Sở hữu đủ roster không được phụ thuộc may rủi.** Gacha thương mại thiết kế để không bao giờ đủ. Ở đây,
> clear hết Phase 1 content là có đủ 14 nhân vật.

Mức bảo đảm, nói chính xác — vì "chắc chắn" mà không kèm cơ chế chỉ là lời hứa suông:

| Bậc | Mức bảo đảm | Cơ chế |
|---|---|---|
| ★5 | **Bảo đảm bằng cơ chế** | Hard pity 40 cho sàn 4 lần trúng; new-character-first ⇒ đủ 3 từ lần trúng thứ 3 |
| ★4 | **Bảo đảm bằng cơ chế** | Soft pity 10 cho sàn 18 lần trúng; soft pity trả **đúng ★4** khi ★4 chưa đủ (mục 9) |
| ★3 | **Phân biệt bằng cơ chế, số lần trúng thì thống kê** | New-character-first áp dụng cho mọi bậc ⇒ không bao giờ trùng khi chưa đủ. Chỉ *số lần trúng* là xác suất: tỉ lệ nền 80%, cần 6 lần ⇒ xác suất thiếu ~1e-8 |

## 4. Nhân vật

14 nhân vật Phase 1. Năm chỉ số: **HP · ATK · DEF · SPD · CRIT**.

### Hệ và khắc hệ

```
Solar → Umbra → Lunar → Tempest → Solar
```

| Quan hệ | Hệ số |
|---|---|
| Khắc (thuận chiều mũi tên) | **×1.25** |
| Trung tính | ×1.0 |
| Bị khắc (ngược chiều) | **×0.8** |

### Vai trò

| Vai trò | Chức năng |
|---|---|
| **Vanguard** | Tiền tuyến, chịu đòn, taunt |
| **Striker** | Sát thương chính |
| **Weaver** | Hồi máu, buff |
| **Disruptor** | Debuff, khống chế, phá Guard |

### Bậc hiếm

| Bậc | Số lượng | Kit | Vai trò thiết kế |
|---|---|---|---|
| **★3** | 6 | 1 active + 1 passive | **Chuyên một việc, giỏi nhất việc đó** |
| **★4** | 5 | 2 active + 1 passive + 1 synergy tag | Linh hoạt, xương sống đội hình |
| **★5** | 3 | 2 active + Ultimate + 2 passive + 2 tag | Định hình lối chơi |

> Bậc hiếm quyết định **độ phức tạp kit và số synergy tag**, **không chỉ là chỉ số cao hơn.** Nếu ★3 chỉ là
> "★5 chỉ số thấp" thì chúng thành rác ngay khi người chơi có ★5, và roster thực tế co lại còn 3 nhân vật.
> Cách giữ ★3 sống: cho chúng **cực đoan ở một việc** — taunt mạnh nhất game, cleanse duy nhất không tốn
> lượt, buff SPD lớn nhất. ★5 linh hoạt hơn nhưng không giỏi bằng ở đúng việc đó.

## 5. Aria — 1v1, và Gauntlet — Bo3

Mỗi lượt, hai bên **chọn đồng thời** một *nhóm hành động*, rồi chọn skill bên trong nhóm.

| | thắng | thua |
|---|---|---|
| **Strike** | vs Break | vs Guard |
| **Guard** | vs Strike | vs Break |
| **Break** | vs Guard | vs Strike |

Kết quả vòng đoán là **hệ số**, không phải thắng/thua tuyệt đối:

| Kết quả đọc | Sát thương | Momentum |
|---|---|---|
| Đọc đúng | 100% | **+1** |
| Hoà (cùng nhóm) | 75% | +0 |
| Đọc sai | 50% | +0 |

**Momentum** tối đa **5**, Ultimate tốn **3**, chỉ mất khi dùng. **Cả hai bên đều tích Momentum theo cùng
luật.**

Đây không phải oẳn tù tì thuần: **skill nào trong nhóm** và **chỉ số** vẫn quyết định con số cuối. Lớp đoán
ý chỉ nhân hệ số.

### AI phải mix — nếu không Aria là bài toán đã giải

> Trọng số **cố định** cộng tell **trung thực** tạo ra một **best response thuần**: sau 2–3 trận người chơi
> chỉ việc counter tell mỗi lượt. Aria khi đó không phải tung đồng xu mà tệ hơn — nó là một bảng tra cứu.

Đặc tả đủ để implement:

| Tham số | Giá trị |
|---|---|
| Trọng số nền | đều, `1/3` mỗi nhóm |
| `p` — xác suất tell nói thật | **0.80 / 0.65 / 0.50** theo phase 1/2/3 |
| Tell khi nói dối | hiện **một trong hai nhóm còn lại, chọn đều** |
| Bảng đếm | 3×3: `P(nhóm người chơi dùng | nhóm AI dùng ở lượt trước)` |
| Độ dịch trọng số `Δ` | **0.15**, cộng vào nhóm counter của phản ứng có tần suất cao nhất, trừ đều từ hai nhóm kia |
| **Sàn trọng số** | **0.15 mỗi nhóm, clamp cứng** |
| Phạm vi bảng đếm | **trong một trận**, reset mỗi lần vào trận, **không lưu vào save** |

> **Sàn trọng số là thứ giữ cho AI không tự biến thành bảng tra cứu.** Không clamp thì `Δ` tích luỹ đẩy AI
> về gần tất định, và người chơi lại khai thác được — đúng cái bẫy đang cố tránh, chỉ đổi chiều.

AI **chốt lựa chọn trước khi thấy input người chơi** (thật sự đồng thời). Tell tính từ lựa chọn đã chốt, nên
tell không bao giờ là lời hứa — chỉ là bằng chứng có nhiễu.

### Gauntlet — Bo3, cấu trúc trận bọc quanh Aria

Gauntlet **không phải format thứ ba**. Nó là **cấu trúc trận**: mang 3 nhân vật, mỗi hiệp là một trận
Aria 1v1, **thắng 2 hiệp là thắng trận**.

| Luật | Giá trị |
|---|---|
| Đội hình | **3 nhân vật**, không có dự bị ngoài 3 |
| Mỗi hiệp | một trận **Aria** đầy đủ (mục 5 ở trên) |
| Thắng trận | **2/3 hiệp** |
| **HP** | **Mang sang hiệp sau.** Nhân vật đã đánh giữ nguyên HP còn lại |
| **Momentum** | **Reset về 0 mỗi hiệp** |
| **Gục** | HP về 0 ⇒ **loại khỏi trận**, không cử lại được |
| **Thua trận sớm** | Hết người còn đứng ⇒ **thua ngay**, bất kể tỉ số hiệp |
| Hoán đổi | **Tự do giữa các hiệp.** Được cử lại nhân vật đã đánh (với HP đã mất), miễn là chưa gục |
| **Entrance** | Nhân vật lần đầu ra sân vào với **+1 Momentum — CHỈ khi hiệp trước bên bạn THUA** |
| **Hiệp 3** | Cả hai bên bắt đầu với **Momentum 1** |

Bốn luật cuối là chỗ trận đấu có chiều sâu, và mỗi luật đóng đúng một lỗ:

> **Vì sao Entrance phải gắn với việc THUA hiệp trước.** Bản đầu cho +1 cho mọi nhân vật lần đầu ra sân —
> và luật đó **không bao giờ ràng buộc**: với đúng 3 người và tối đa 3 hiệp thì luôn còn người chưa ra sân,
> nên A→B→C là tối ưu hiển nhiên và cả hai bên ăn +1 mỗi hiệp. Nó là **hằng số, không phải tài nguyên**.
> Gắn vào việc thua thì nó thành **cơ chế bắt kịp**, và tạo ra quyết định thật ở bên đang thắng: cử người
> mới nguyên máu nhưng không có thưởng, hay cử lại người vừa thắng với số HP đã sứt?

> **Vì sao gục phải loại hẳn.** Không có luật này thì ghế dự bị **không tốn gì** — HP được giữ chứ không
> bị tiêu, nên xoay vòng là miễn phí. Cho gục là loại thì ba nhân vật thành một **quỹ hữu hạn**, và mỗi
> lần cử ai ra là một lần tiêu vào quỹ đó.

> **Vì sao hiệp 3 bắt đầu ở Momentum 1.** Momentum reset mỗi hiệp mà Ultimate tốn 3, nên hiệp quyết định —
> vốn ngắn vì hai bên đều sứt mẻ — gần như **không bao giờ chạm tới Ultimate**. Đúng ngược với cao trào mà
> cấu trúc Bo3 sinh ra để tạo. Một con số sửa được.

**Lớp thông tin bất đối xứng.** Hiệp 1 hai bên chọn mù. Từ hiệp 2, bạn **đã thấy một nhân vật của đối
thủ** — biết hệ, biết vai trò, đoán được kit. Đây là tầng đọc ý thứ hai, chồng lên tầng đọc ý trong từng
lượt của Aria: một bên là đoán *nhóm hành động lượt này*, một bên là đoán *nhân vật hiệp sau*.

**Vì sao Momentum reset mà HP thì không.** Hai tài nguyên mang hai ý nghĩa khác nhau: HP là **hậu quả
tích luỹ** của cả trận, nên phải mang theo, nếu không thắng hiệp 1 chẳng có giá trị gì. Momentum là
**nhịp của một cuộc đấu tay đôi cụ thể**, nên nó chết theo cuộc đấu đó — cho nó mang sang là tạo
snowball không gỡ được, đúng thứ cấu trúc Bo3 sinh ra để tránh.

Gauntlet **cần đúng 3 nhân vật dùng được**, và vì HP mang sang nên không thể dựa vào một carry duy nhất.
Đây là format ép chiều sâu roster mạnh nhất trong ba cái.

## 6. Chorus — 3v3

Cùng engine, cùng dữ liệu nhân vật. Khác ở **vị trí** và **Link**.

### Vị trí

Đội hình một hàng dọc: **Front (1 slot) · Back (2 slot)**.

- Đòn đánh thường luôn nhắm **Front**, trừ khi skill ghi rõ khác.
- Front: **+20% DEF**, nhưng là mục tiêu mặc định.
- Sát thương lan (splash) vào Back: **×0.75**.
- Front chết → unit Back kế tiếp **đôn lên**.

### Link

Gauge **dùng chung cả đội**, 0–100.

| Sự kiện | Link |
|---|---|
| Unit hành động **cùng hệ hoặc cùng tag** với unit vừa hành động trước đó | **+20** |
| Unit hành động không khớp | +10 |

Đầy 100 → **Chain Attack**: cả 3 unit hành động liên tiếp ngay, mỗi đòn **+30% sát thương**, Link về 0.

> Đây là cơ chế **biến việc sưu tầm thành động lực cơ học**. Đội 3 nhân vật cùng hệ lên Chain sau 5 lượt;
> đội lộn xộn cần 10 lượt. Muốn đội đồng bộ thì phải **có đủ nhân vật để chọn** — đó chính là lý do người
> chơi muốn pull.

## 7. Boss & phase

Boss có **3 phase** theo ngưỡng máu **75% / 50%** — giống tiền lệ `BossController` của AircraftStriker.

Mỗi phase đổi:

| | Đổi gì |
|---|---|
| Chung | Bộ skill và trọng số hành động |
| **Aria** | **`p` của tell** (0.80 → 0.65 → 0.50) — phase sau **bluff nhiều hơn**, không phải ngẫu nhiên hơn |
| **Chorus** | Mục tiêu ưu tiên — phase cuối bỏ qua Front, nhắm thẳng Back |

> **Phase transition phải là event bắn đúng một lần**, không phải trạng thái poll mỗi lượt. Poll thì biết
> "đang ở phase 2" nhưng không biết "vừa chuyển sang phase 2" — mất sạch cơ hội cho animation, lời thoại,
> và reset Link. `BossController` hiện tại làm đúng (`TakeDamage` phát hiện đổi phase rồi bắn callback một
> lần); giữ nguyên ngữ nghĩa đó.

## 8. Công thức & thứ tự lượt

```
raw     = ATK × SkillPower
mitig   = raw × 100 / (100 + DEF)
final   = mitig × affinity × readMod × posMod × crit
```

| Hệ số | Giá trị |
|---|---|
| `affinity` | 1.25 / 1.0 / 0.8 (mục 4) |
| `readMod` | Aria **và Gauntlet** (mỗi hiệp là một trận Aria): 1.0 / 0.75 / 0.5 — mục 5. Chorus: luôn 1.0 |
| `posMod` | Chorus: splash vào Back ×0.75. Còn lại 1.0 |
| `crit` | ×1.5, xác suất bằng CRIT |

**Thứ tự lượt:** SPD cao đi trước. Đồng SPD → **%HP hiện tại cao hơn** đi trước. Vẫn đồng → bên **đi sau ở
lượt trước** đi trước.

> Tie-break **hoàn toàn tất định, không RNG**. Trong một game mà lớp chơi chính là đoán ý, tie-break ngẫu
> nhiên phá chính thứ người chơi đang cố dự đoán.

**Level 1–60.** Chi phí `L → L+1` = `20 + 8L` Dust. Tổng `Σ(20+8L), L=1..59` = **15,340 Dust/nhân vật**
⇒ **~215k Dust** để max level cả 14 nhân vật.

## 9. Gacha

**1 pull = 1 Astral Shard.** Không có gói 10-pull giảm giá — không bán gì thì giảm giá vô nghĩa.

| Bậc | Tỉ lệ |
|---|---|
| ★5 | **2%** |
| ★4 | **18%** |
| ★3 | **80%** |

### Pity

| Loại | Quy tắc |
|---|---|
| **Soft** | Mỗi **10** pull không ra ★4+ → pull thứ 10 chắc chắn ★4+. **Khi roster ★4 chưa đủ, pull này trả đúng ★4** (không trả ★5) |
| **Hard** | Mỗi **40** pull không ra ★5 → pull thứ 40 **chắc chắn ★5** |

Mệnh đề "trả đúng ★4" không thừa: nếu soft pity được phép trả ★5, sàn cho ★4 về **0** và bảng bảo đảm ở
mục 3 sai.

> **Và mệnh đề đó HẾT HIỆU LỰC khi đã đủ ★4.** Nó sinh ra để giữ sàn sở hữu ★4; đủ rồi thì nó không còn
> giữ gì nữa, nên từ đó pull bảo đảm trả ★5 theo **tỉ lệ tương đối** của hai bậc: `0.02/(0.02+0.18)` =
> **10%**. Vì new-character-first lấp đủ 5 ★4 trong vài lần trúng đầu tiên, nên **gần như cả ván chơi
> nằm ở chế độ đã-hết-hiệu-lực** — đây không phải chi tiết phụ, nó dịch số ★5 trung bình ở 180 pull từ
> 6.08 lên **6.31** và kéo tỉ lệ người chơi không max nổi ★5 nào từ 49% xuống **47%**.

Hard pity 40 chứ không phải 90 như gacha thương mại, vì currency ở đây hữu hạn.

### Bảo đảm nhân vật mới

> **Chừng nào người chơi chưa sở hữu đủ mọi nhân vật của một bậc, pull ra bậc đó LUÔN cho một nhân vật
> CHƯA sở hữu.** Trùng lặp chỉ xuất hiện sau khi bậc đó đã đủ.

Không có quy tắc này, "sàn 4 ★5" chỉ là 4 **lần trúng**, không phải 4 **nhân vật khác nhau** — xác suất gom
đủ 3 ★5 riêng biệt chỉ khoảng **75%**. Có nó, sàn 4 lần trúng **bảo đảm** đủ cả 3, và cơ chế "selector" cho
người chơi chọn mục tiêu trở nên không cần thiết.

### Focus — cơ chế lệch rate

Người chơi chỉ định **một nhân vật Focus** trên banner đang pull. Trong bậc của nhân vật đó, trọng số của
họ **×3**.

| Thành phần bậc trong banner | Focus nhận |
|---|---|
| 2 nhân vật | `3/(3+1)` = **75%** |
| 3 nhân vật | `3/(3+1+1)` = **60%** |
| 1 nhân vật | không đổi — hard pity vốn đã bảo đảm |

Đổi Focus **miễn phí và tức thì**. Đây là game offline; bắt trả giá để đổi mục tiêu chỉ là ma sát, không
phải thiết kế.

> **Focus không bao giờ đè lên new-character-first.** Chừng nào bậc đó còn nhân vật chưa sở hữu, Focus chỉ
> lệch **trong nhóm chưa sở hữu**. Nghĩa là Focus **không thể** dùng để cày trùng trong khi bạn còn thiếu
> người — bảo đảm sở hữu ở mục 3 giữ nguyên, không bị cơ chế mới đục thủng.

**Focus phát huy nhất sau khi đã đủ roster.** Lúc đó mọi pull đều là trùng, và Focus quyết định **Fragment
chảy về ai** — tức là bạn chọn ai được max trước. Đây chính là chỗ farm Shard có nghĩa: cày The Ascent để
hoàn thiện đúng nhân vật bạn thích, chứ không phải để lấp danh sách.

### Trùng lặp

Mỗi bản trùng → **2 Echo Fragment** của đúng nhân vật đó. Khi đã đủ một bậc, pull ra bậc đó **tự động quy
đổi thẳng** thành Fragment, không hiện màn hình "bạn nhận được nhân vật đã có".

## 10. Economy

| Currency | Nguồn | Tiêu vào | Farm được? |
|---|---|---|---|
| **Astral Shard** | Campaign + first-clear từng tầng Ascent | Pull | **Có — bằng độ sâu, không bằng thời gian** |
| **Resonance Dust** | Node lặp lại, thưởng trận | Level, Catalyst, nâng trang bị | **CÓ, không trần** |

### Ngân sách Astral Shard — Phase 1

**Phần bảo đảm — campaign, cố định:**

| Nguồn | Đơn giá | Số lượng | Tổng |
|---|---|---|---|
| Clear chapter (node chính) | 10 | 6 | 60 |
| Hoàn thành chapter 3 sao | 5 | 6 | 30 |
| Lần đầu hạ boss | 15 | 6 | 90 |
| **Sàn bảo đảm** | | | **180** |

**Phần cày được — The Ascent:**

| | |
|---|---|
| First-clear mỗi tầng | **6 Shard** (`ASCENT_SHARD_PER_FLOOR`) |
| Số tầng Phase 1 | **40 tầng** (`ASCENT_FLOORS`) |
| **Tổng Ascent** | **240 Shard** — cộng với 180 campaign ⇒ **trần 420** |
| **Đánh lại tầng đã qua** | **0 Shard** |

*Cả hai con số do sim chốt (rủi ro 7). 240 là lượng vừa đủ để người leo hết tháp max trọn roster —
không dư, xem mốc trần ở mục 11.*

> **Cày bằng độ sâu, không bằng thời gian.** Không có Shard cho việc chạy lại. Muốn thêm Shard phải đi sâu
> hơn — mà Ascent không hồi HP/Link giữa các tầng, nên đi sâu hơn đòi **roster rộng và mạnh hơn**. Đây là
> thứ thay thế stamina mà không cần đồng hồ: người chơi bị chặn bởi **sức mạnh đội hình**, không bởi giờ giấc.

> **Cả hai nguồn đều phát đúng một lần**, bảo đảm bằng sổ cái `ClaimedRewardIds`. First-clear một tầng cũng
> exactly-once y hệt clear một chapter — nên **toàn bộ lập luận an toàn cũ sống sót nguyên vẹn** qua việc
> mở cho farm. Không có sổ cái thì chạy lại là nhận lại, và mục 3 sụp đổ.

### Thu nhập Resonance Dust

Node lặp lại: **~600 Dust / lượt chạy (~3 phút)**. Campaign rải thêm ~40k Dust.

> **Con số 600 này là của core, không phải của content pack.** Mốc trần 22,2 giờ đứng hoàn toàn trên nó,
> nên một mode pack tự khai "lượt chạy của tôi trả 5.000 Dust" sẽ làm mốc đó vô nghĩa **mà không ai sửa
> một dòng thiết kế nào** — đúng cùng một lỗ với Catalyst ở dưới, chỉ khác tài nguyên. Mode **báo độ dài
> lượt chạy**, core tra bảng rồi trả Dust. Xem tài liệu kiến trúc: API mà mode chạm được không có phương
> thức phát Dust tuỳ ý. Dust **không có trần** —
đây là chủ ý: nó là nơi người chơi tiêu thời gian mà không ảnh hưởng tới roster.

### Ràng buộc sống còn

> **Resonance Dust không bao giờ được quy đổi sang Astral Shard.** Không shop, không sự kiện, không ngoại lệ.

Shop (tiêu Dust) bán: **Resonance Catalyst**. **Không bán nhân vật, không bán Fragment, không bán Shard.**

> **Vật liệu đã đổi vai.** Bản trước cho lên cấp tốn "Dust + vật liệu", mà vật liệu lại mua bằng Dust —
> tức là tiêu Dust hai lần qua thêm một lần bấm nút, không tạo ra quyết định nào. Từ nay:
> **lên cấp chỉ tốn Dust**, còn **vật liệu chỉ dùng để nâng cấp trang bị** (mục 11) và **chỉ rơi từ mode**,
> mỗi mode một loại khác nhau. Vật liệu vì thế mới có nguồn thật và đích thật.

### Chế độ chơi được phát cái gì — và tuyệt đối không được phát cái gì

Mode (mục 13) **lặp lại được vô hạn**. Đây là chỗ mọi ràng buộc ở trên có thể chết mà không ai nhận ra:

> **Phần thưởng LẶP LẠI của mode chỉ gồm Resonance Dust và vật liệu nâng trang bị.**
> **Không bao giờ có Astral Shard. Không bao giờ có Echo Fragment. Không bao giờ có Resonance Catalyst.**

> **Vì sao Catalyst bị đẩy ra khỏi phần thưởng lặp lại — sim bắt được.** Bản trước cho phép mode rơi
> Catalyst lặp lại. Sim: **không có mode rơi**, đi từ giá 600 lên 1.500 Dust làm thời gian cày đổi
> **5,7 giờ** (18,4 → 24,1). Cho mode rơi **0,25 Catalyst/lượt**, cùng khoảng giá đó chỉ còn đổi
> **0,1 giờ** (14,75 → 14,85) — **sập 57 lần**, vì ràng buộc Dust chặn trước. Shop chết, và cái duy nhất còn gate việc quy đổi là thời gian ngồi chạy mode — đúng thứ "farm
> bằng thời gian" mà mục 3 cấm. Catalyst vì thế chỉ được phép nằm ở **mốc first-clear của mode**, đi qua
> sổ cái như mọi thứ exactly-once khác.

Phân biệt sống còn: **lặp lại** khác **first-clear**. The Ascent *được phép* phát Shard ở mốc first-clear
từng tầng, vì mốc đó **chỉ xảy ra một lần** và đi qua sổ cái. Nhưng chạy lại tầng 12 lần thứ hai mươi thì
**không ra gì ngoài Dust**.

Nếu để phần thưởng lặp lại chứa Shard — dù chỉ 1 mỗi lượt — thì Shard thành **farm bằng thời gian**, và
mục 3 sụp: người chơi ngồi đủ lâu là có tất cả, pull mất hết sức nặng. Tương tự với Fragment: nguồn
Fragment lặp lại làm toàn bộ mục 11 thành vô nghĩa và ★5 max được trong một buổi tối.

Ranh giới này **được thực thi bằng kiểu dữ liệu, không bằng kỷ luật** — xem tài liệu kiến trúc: API phần
thưởng mà mode chạm được **không có** phương thức phát Shard; nó chỉ **báo mốc**, còn core tra sổ cái rồi
mới cấp.

Điều này **không làm mode nhạt**. Dust gate toàn bộ level 1→60 (**215k Dust** cho 14 nhân vật) **và**
gate luôn việc mua Catalyst — tức là mode chính là nơi người chơi **hoàn thiện** nhân vật, chỉ không
phải nơi họ **kiếm thêm** nhân vật. Phân vai rõ: **campaign là lịch phần thưởng, mode là tầng grind.**

Ngoại lệ duy nhất được phép: **mốc first-clear do tác giả đặt bên trong một mode** (ví dụ "lần đầu lên
tầng 50") có thể phát **Shard hoặc Catalyst**, vì nó **phát đúng một lần** qua sổ cái `ClaimedRewardIds`.

> **Cả hai loại đều phải vào sổ, và vì hai lý do khác nhau.** Shard trôi ngoài sổ làm hỏng **ngân sách
> pull** (mục 11). Catalyst trôi ngoài sổ làm hỏng **con số 22,2 giờ** — mỗi Catalyst phát không qua shop
> là 1.200 Dust người chơi không phải cày, nên đủ nhiều mốc là mốc trần tự dịch mà không ai chỉnh gì.
> Đây chính là chỗ rò vừa bịt ở phần thưởng lặp lại, chỉ đổi sang cửa mốc. Trần cho **cả hai** nằm ở core,
> xem tài liệu kiến trúc — `MAX_PACK_SHARD` và `MAX_PACK_CATALYST`.

**Giá chốt: 1 Catalyst = 1.200 Dust** — sim xác nhận, và xác nhận luôn rằng **giá không phải chỗ có vấn đề**.

> **Catalyst là một Dust sink, KHÔNG phải một cái van.** Dust không có trần nên Catalyst cũng không. Thứ
> thật sự giới hạn việc quy đổi là **số Fragment**, mà Fragment bị chặn bởi ngân sách pull. Đừng thiết kế
> như thể Catalyst đang cân bằng cái gì.

> **Giá Catalyst và tốc độ thu Dust là MỘT bài toán, và sim đã giải nó.** Ở mốc trần (mục 11):
> **76 Catalyst = 91k Dust**, cộng **215k** để max level cả 14 nhân vật, trừ **40k** campaign rải sẵn
> ⇒ **266k Dust phải cày** = **444 lượt chạy** ở mức 600 Dust/lượt ≈ **22,2 giờ**.
>
> Nghi phạm đúng là **tốc độ thu Dust**, không phải giá. Ở mức 300 Dust/lượt cũ, cùng một mục tiêu tốn
> **44,4 giờ**; gấp đôi tốc độ đưa nó về 22,2 giờ, còn **hạ giá Catalyst từ 1.200 xuống 900 chỉ tiết kiệm
> 1,9 giờ**. Giá Catalyst là một cái nút gần như không có tác dụng — đừng vặn nó khi cần chỉnh thời lượng.

## 11. Tiến triển nhân vật

- **Level 1–60** — **chỉ tốn Dust** (công thức mục 8). Vật liệu không còn nằm trong chi phí lên cấp.
- **4 skill node** mỗi nhân vật, mở bằng Echo Fragment giá **1 / 2 / 3 / 4** → **10 Fragment để max một
  nhân vật**.

### Resonance — quy đổi Fragment

> **Fragment cùng một bậc (gộp từ BẤT KỲ nhân vật nào của bậc đó) + 1 Catalyst → 1 Fragment của MỘT nhân
> vật bạn CHỌN. Giá khác nhau theo hướng đi:**
>
> | Hướng | Giá |
> |---|---|
> | **Cùng bậc** — ★4 → ★4 | **5 : 1** |
> | **Lên một bậc** — ★3 → ★4, ★4 → ★5 | **10 : 1** |
>
> Không bao giờ đi xuống bậc, không bao giờ nhảy hai bậc.

Ba việc trong một luật:

1. **Đầu vào gộp theo bậc, đầu ra có mục tiêu.** Fragment lưu theo từng nhân vật (`Dictionary<string,int>`
   khoá `packId:entityId`), nên nếu không nói rõ đầu ra thuộc về ai thì luật vô nghĩa.
2. **Chọn AI được hoàn thiện — không phải xoá phương sai.** Đổi cùng bậc để người chơi dồn fragment về
   đúng nhân vật họ muốn, thay vì phải chấp nhận người nào đang rẻ nhất.

> **Lý do cũ của luật này SAI, và sim chứng minh được.** Bản trước viết: *"29 bản trùng ★4 chia cho 5 nhân
> vật là ~5.8 mỗi con, độ lệch chuẩn ~4 — không có đường chuyển ngang thì khoảng một nửa số ★4 hụt fragment"*.
> Sim (30.000 người chơi, cả hai mốc neo): quy đổi **cùng bậc kích hoạt 0,0% số lần chạy**, và **100%**
> người chơi max trọn ★3 lẫn ★4 mà **không cần một lần đổi ngang nào**. Lý do: dòng fragment từ bậc dưới
> **đã có mục tiêu**, nên nó lấp chỗ hụt 1:1 trước khi phương sai kịp thành vấn đề. Thử ép lệch bằng Focus
> ×3 suốt 420 pull cũng chỉ đẩy lên **0,1%**.
>
> Giữ luật lại **không phải** vì nó vá phương sai — nó không vá gì cả. Giữ vì nó là thứ duy nhất cho người
> chơi **đổi mục tiêu**: sim tối đa hoá *số lượng* nhân vật max được, người chơi thì muốn max **đúng người
> họ thích**. Đó là hai bài toán khác nhau, và chỉ bài toán thứ hai cần đường đi ngang.
3. **Cho ★3 thừa mứa một lối ra** lên ★4 rồi ★5 — nhưng là một lối ra **đắt**.

> **Vì sao hai giá chứ không phải một — sim bắt được.** Ở giá đồng nhất 5:1, riêng campaign đã sinh ~276
> Fragment ★3, và chuỗi ★3→★4→★5 biến 25 Fragment ★3 thành 1 Fragment ★5. Ống dẫn đó rộng tới mức
> **100% người chơi max trọn 14 nhân vật ở 260 Shard** — trong khi campaign một mình đã cho 180. Tức là
> toàn bộ vai trò kinh tế của The Ascent chỉ còn đáng **80 Shard**, và "khoảng cách giữa sở hữu và làm
> chủ" — trụ cột sinh ra cả cái tháp — gần như không tồn tại.
>
> Tách giá chữa đúng chỗ rò mà không đụng vào chỗ lành: **lý do 2 là xoá phương sai trong cùng một bậc,
> nên giữ 5:1**; **lý do 3 là xả hàng thừa sang bậc trên, và chỉ nó mới là chỗ rò**. Ở 10:1, trần dịch
> lên **420 Shard** ⇒ tháp có **240 Shard** để phát, tức là có một đường cong thật để leo.

### Fragment đi tới đâu

Từ khi Shard cày được (mục 10), **không còn một tổng số pull duy nhất** để tính. Dưới đây là **hai mốc
neo**; kết quả thật của một người chơi nằm đâu đó ở giữa, tuỳ họ leo Ascent tới đâu.

Cả hai bảng đọc theo **chuỗi**: mỗi dòng đã trừ phần bị dòng dưới tiêu. Cột cuối là phần **thật sự giữ
được sau toàn bộ quy đổi** — ghi "dư 220 ★3" cạnh "thiếu 5 ★5" là đếm trùng, vì 220 đó chính là thứ bị
tiêu để bù cho ★5.

Số lần trúng lấy từ **sim Monte-Carlo 30.000 người chơi**, không lấy từ `ngân sách / E[chu kỳ]`.

> **Vì sao không dùng công thức.** `E[chu kỳ ★5] = (1−0.98⁴⁰)/0.02 = 27.715` là **tiệm cận của lý thuyết
> đổi mới**, không phải kỳ vọng ở một chân trời hữu hạn: `180/27.715 = 6.50` nhưng sim ra **6.31**. Ngược
> lại, ★4 **nhiều hơn** công thức thuần tỉ lệ vì mỗi lần hard pity trả ★5 thì nó **reset luôn bộ đếm
> soft pity**, bơm thêm ★4+ vào ngoài quá trình đổi mới. Hai sai số ngược chiều nhau nên nhìn tổng thì
> tưởng khớp. Bảng dưới dùng số của sim.

Cột **Từ bậc dưới** tách riêng để phép tính kiểm được — bản trước gộp nó vào "Tổng" và đó chính là chỗ
người đọc không lần lại được con số.

#### Mốc sàn — 180 pull (chỉ campaign, không leo tháp)

**6.31 ★5 · 35.35 ★4 · 138.34 ★3**

| Bậc | Trùng ×2 | Affinity | Từ bậc dưới | Tổng | Cần max | Đổi lên bậc trên | **Còn lại** |
|---|---|---|---|---|---|---|---|
| ★3 | 265 | +12 | — | 277 | 60 | −210 → **+21 ★4** | **7** |
| ★4 | 61 | +10 | +21 | **92** | 50 | −40 → **+4 ★5** | **2** |
| ★5 | 7 | +6 | +4 | **17** | 30 | — | **thiếu 13** |

⇒ **Sở hữu đủ 14 nhân vật** · max hết ★3 và ★4 · ★5 đạt **17/30**. Catalyst tiêu: **25**.

Đây là **sàn bảo đảm**, và chữ "bảo đảm" ở đây có số đỡ: trong 30.000 lượt sim, **100%** người chơi max
trọn cả 6 ★3 **và** cả 5 ★4 (p01 cũng là 6/6 và 5/5). 11 nhân vật max hết thì thừa sức dựng một đội 3
người hoàn thiện cho cả Chorus lẫn Gauntlet.

> **Nhưng ★5 thì sàn này KHÔNG hứa gì, và mức độ nặng hơn tôi viết lần đầu.** Sim: campaign-only max được
> **1 ★5** ở trung vị, và **47% — gần đúng một nửa — max được 0 ★5** (bản trước ghi "~10%", sai; con số đó
> suy từ phân vị của *tổng* nhân vật max, không phải của riêng ★5). Đây là **hệ quả cố ý** của giá 10:1 —
> muốn tháp có đường cong thật thì điểm kết thúc của riêng campaign phải thấp xuống. ★5 max là **phần
> thưởng của việc leo**, không phải thứ campaign phát kèm. Xem rủi ro 13.

#### Mốc trần — 420 pull (180 campaign + 240 từ 40 tầng Ascent)

**15.26 ★5 · 82.40 ★4 · 322.34 ★3**

| Bậc | Trùng ×2 | Affinity | Từ bậc dưới | Tổng | Cần max | Đổi lên bậc trên | **Còn lại** |
|---|---|---|---|---|---|---|---|
| ★3 | 633 | +12 | — | 645 | 60 | −580 → **+58 ★4** | **5** |
| ★4 | 155 | +10 | +58 | **223** | 50 | −170 → **+17 ★5** | **3** |
| ★5 | 25 | +6 | +17 | **48** | 30 | — | **dư 18** |

⇒ **100% người chơi max trọn roster, kể cả cả ba ★5.** Catalyst tiêu: **75** (p90 = **76**).

> Đây là **thiết kế, không còn là minh hoạ**: 420 là con số sim tìm ra cho "leo hết tháp thì max được hết",
> và 240 Shard của tháp chia thành **40 tầng × 6 Shard**. Biên an toàn đo được: **p01 vẫn max cả 3 ★5** —
> tức là kể cả 1% xui nhất cũng tới đích.
>
> **Đừng đọc "dư 18" như một đệm 18 Fragment.** Nó là fragment đã nằm trên những nhân vật đã max, và dịch
> sang nhân vật khác tốn **5:1**, nên sức vá thật của nó chỉ khoảng **3 Fragment**. Ở đây không sao vì biên
> chưa bao giờ phải dùng tới; nhưng nếu sau này chỉnh số làm p01 tụt xuống dưới 3 ★5 thì **18 đó không cứu được**.

**Vai trò của Ascent trong kinh tế:** nó là **khoảng cách giữa "sở hữu" và "làm chủ"**. Campaign cho bạn
đủ nhân vật; Ascent cho bạn max chúng. Ghép với **Focus** (mục 9) — thứ quyết định Fragment chảy về ai —
thì việc cày có mục tiêu rõ ràng: leo sâu hơn để hoàn thiện **đúng nhân vật bạn thích**, chứ không phải
để lấp cho đầy danh sách.

### Hệ trang bị — thiết kế đầy đủ, **chưa xếp phase**

> Mục này mô tả một hệ đã thiết kế xong nhưng **chưa cam kết vào phase nào** — cùng dạng với "Phase 2 —
> the trade route" của GildedLedger. Đừng đọc nó như hạng mục Phase 1 bị bỏ sót.

Ba trục sức mạnh hiện có — level, skill node, đội hình — để lại hai lỗ thật:

1. **Endgame có điểm dừng.** Max hết skill node thì The Ascent không còn gì để cho.
2. **Ba mode chưa có bản sắc.** Chạy Ascent hay Colossus phần thưởng như nhau, nên không có lý do chọn.

Trang bị vá đúng hai chỗ đó.

#### Ba slot

| Slot | Main stat | Vai trò |
|---|---|---|
| **Instrument** | ATK — cố định | tấn công |
| **Mantle** | HP — cố định | phòng thủ |
| **Sigil** | **roll** từ pool: CRIT · SPD · DEF · hiệu ứng | **chỗ build choice thật sự nằm** |

Ba slot chứ không phải năm sáu: roster chỉ 14 nhân vật, mỗi slot thêm vào là nhân khối lượng farm lên.

#### Roll và nâng cấp — cố ý nhẹ hơn game thương mại

- **Tối đa 3 substat**, roll từ pool có khoảng giá trị.
- **Level 0→9.** Mỗi 3 level thêm hoặc nâng một substat.
- Món ★5 có main stat cao hơn và bắt đầu với nhiều substat hơn ★4/★3.

> Genshin +20 với 4 substat tồn tại để nuôi một vòng grind vô hạn **bán được**. Bỏ phần bán đi thì chiều
> sâu đó chỉ còn lại phần khổ. 0→9 giữ được cảm giác "món này roll đẹp" mà không cần 200 giờ.

#### Set — đây là chỗ mode có bản sắc

Ba slot ⇒ bonus ở mốc **2 món** và **3 món**. Mỗi mode rơi bộ riêng:

| Mode | Hướng bộ | Vì sao khớp với mode đó |
|---|---|---|
| **The Ascent** | Trụ hạng — hồi máu mỗi lượt, giảm sát thương khi HP thấp | Ascent không hồi HP giữa các tầng ⇒ bộ này biến việc leo sâu thành khả thi |
| **Standing Colossus** | Sát thương — cộng thêm khi đánh mục tiêu HP cao, dồn dần theo lượt | Colossus là một túi máu khổng lồ ⇒ thưởng cho việc đánh dài |
| **Echo Duel** | Đọc ý — thêm Momentum khi đọc đúng, thưởng khi thắng hiệp | Duel chạy trên Aria/Gauntlet ⇒ bộ này ăn thẳng vào lớp đoán ý |

Đây là câu trả lời cho "vì sao chạy mode này thay vì mode kia" — thứ mà bản trước không có.

#### Không thêm currency nào

| Việc | Tốn |
|---|---|
| Nâng cấp trang bị | **Dust + vật liệu** |
| Rã trang bị | trả lại **vật liệu** |

Vật liệu vốn là khâu thừa (mục 10) nay có nguồn thật — **rơi từ mode, mỗi mode một loại** — và đích thật.
Không phát sinh resource mới.

> **Trang bị không bao giờ rơi ra Astral Shard hay Echo Fragment, và không bao giờ đổi được sang chúng.**
> Cùng một lằn ranh xuyên suốt: **exactly-once vs lặp lại**. Trang bị là phần thưởng lặp lại, nên nó nằm
> hẳn ở phía Dust/vật liệu.

#### Điều khiến trang bị khác MỌI thứ khác trong game

Đây là **thứ đầu tiên được SINH RA chứ không được viết sẵn**. Mọi content khác — nhân vật, banner, chapter,
mode — đều là data do tác giả viết. Một món trang bị thì được roll lúc runtime.

Hệ quả: **template/bộ là data** (ship trong content pack như mọi thứ khác), còn **instance là save state**
(của core). Chi tiết POCO và trần kho ở [tài liệu kiến trúc](astral-chorus-content-architecture.md).

## 12. Story & dialogue

6 chapter Phase 1. Mỗi chapter ~10 scene hội thoại + 4–6 trận.

| Có | Không có |
|---|---|
| Portrait trái/phải, tên, text, đánh máy | **Nhánh rẽ / lựa chọn** |
| Biến thể biểu cảm theo key (thiếu → fallback default) | **Voice / audio** |
| Auto / skip | Cutscene động, Live2D |

Kịch bản là **TextAsset JSON, một file một chapter**, trỏ bằng string address. **Không phải
ScriptableObject** — khối lượng viết lớn, sửa hội thoại trong Inspector là cực hình, file text thì diff
được, sửa hàng loạt được, và **giữ cho content pack là data thuần**.

### Affinity — phạm vi Phase 1

**3 mốc** theo số trận tham chiến (**10 / 30 / 60**):

| Mốc | Thưởng |
|---|---|
| 10 trận | Đoạn thoại + 500 Dust |
| 30 trận | Đoạn thoại + 1500 Dust |
| 60 trận | Đoạn thoại + **2 Echo Fragment** của chính nhân vật đó |

> Chỉ mốc cuối cho Fragment, và chỉ 2. Nếu cả ba mốc đều cho Fragment thì tổng là 84 Fragment toàn roster
> (★5 nhận 18) — đủ để **max ★5 ngay trong Phase 1**, mâu thuẫn thẳng với mục 11. Affinity ở đây là
> **lý do để dùng nhân vật mình thích**, không phải một nguồn kinh tế thứ hai.

Mốc affinity cũng phát đúng một lần — dùng chung sổ cái `ClaimedRewardIds`.

### Cảnh báo khối lượng

> **Framework hiện không có gì cho hội thoại.** Subsystem lớn thứ hai sau combat, và là hạng mục **duy nhất
> chưa định lượng được bằng con số chắc chắn**.
>
> Ước tính thô: 6 chapter × 10 scene × ~8 dòng ≈ **480 dòng thoại**, cộng 14 bio và 42 đoạn affinity.
> Đây là **khối lượng viết, không phải khối lượng code** — không rút ngắn được bằng kỹ thuật.

## 13. Content pack & chế độ chơi — góc nhìn người chơi

Người chơi không bao giờ thấy chữ "pack". Họ thấy: **một banner mới mở khoá, một chương truyện mới, và
một chế độ chơi mới xuất hiện trong menu.**

Pack bị tắt: banner không xuất hiện, chương không có trong danh sách, nhân vật không ra từ gacha, chế độ
không hiện trong menu. Nếu người chơi **đã sở hữu** nhân vật của pack bị tắt, nhân vật đó **giữ nguyên
trong save**, chỉ ẩn khỏi UI kèm dòng "N nhân vật đang ẩn". Bật lại → quay về nguyên vẹn cùng level và node.

### Format ≠ Mode

Phân biệt này là trục của toàn bộ kiến trúc modular — nhầm nó là hỏng cả hai bên:

| | Là gì | Ở đâu |
|---|---|---|
| **Format** | Một trận **resolve như thế nào** — Aria (1v1), Gauntlet (Bo3), Chorus (3v3) | **Core.** Ba cái, đóng, không mở rộng ở Phase 1 |
| **Mode** | Một chuỗi trận **có nghĩa gì** — luật tiến triển, điều kiện thua, bảng thưởng | **Pack.** Mở, thêm/bớt tự do |

Mode **khai báo nó dùng format nào**. Đó là toàn bộ giao diện giữa hai bên: core cung cấp ba cách đánh
một trận, mode quyết định đánh bao nhiêu trận, theo thứ tự nào, thua thì sao, thắng được gì.

### Chế độ chơi Phase 1

Gacha thương mại có võ đài, guild war, world boss, leo tháp. Offline thì **PvP và guild war không dịch
được** — chúng cần người chơi khác, không phải cần server. Ba cái còn lại dịch được, và đây là bản dịch:

| Mode | Format | Luật | Vì sao hợp offline |
|---|---|---|---|
| **The Ascent** (leo tháp) | Gauntlet | N tầng liên tiếp. **HP và Link KHÔNG hồi giữa các tầng.** Không dùng lại nhân vật đã ngã | Thuần tất định, không cần đối thủ. **Ép chiều sâu roster mạnh nhất** — không thể leo bằng 3 nhân vật |
| **Standing Colossus** (world boss) | Chorus | Một boss có HP **khổng lồ, lưu lại giữa các lần đánh**. Mỗi lượt chơi bào một phần. Thưởng theo mốc sát thương tích luỹ | World boss offline chỉ là **một bộ đếm HP bền vững** — bỏ bảng xếp hạng đi thì phần còn lại chạy hoàn toàn cục bộ |
| **Echo Duel** (võ đài) | Aria hoặc Gauntlet | Đối thủ là **đội archetype do tác giả dựng**, cộng với **đội dựng từ chính roster người chơi** ở tier cao. Bậc thang cố định, không có rating động | Không cần người chơi khác. Đội dựng từ roster của chính bạn là thứ thay thế PvP gần nhất mà offline làm được |

**Guild war bị cắt, không thay thế.** Bản chất của nó là phối hợp giữa nhiều người; mọi bản "offline hoá"
đều biến nó thành một mode PvE khác đội lốt, tức là thêm việc mà không thêm trải nghiệm mới.

> **Không có mode nào theo lịch.** Không daily, không weekly, không sự kiện giới hạn thời gian. Game offline
> không có lý do gì để khoá content sau đồng hồ — cùng lý do banner không xoay vòng (mục 2). Mode mở bằng
> **tiến độ**, rồi mở vĩnh viễn và **lặp lại được vô hạn**.

## 14. Màn hình → layer UIFramework

| Màn hình | Layer | Ghi chú |
|---|---|---|
| Main menu | `Screen` | |
| **Lưới sưu tầm** | `Screen` | **Phải tự xây** — `RecyclerView` không có multi-column |
| Chi tiết nhân vật | `Screen` | Chỉ số dùng `TooltipStatLine` |
| Chọn banner | `Screen` | Duyệt registry, không hardcode |
| Animation pull | `Overlay` | DOTween + UIParticle + UIEffect |
| Kết quả pull | `Popup` | |
| Team builder | `Screen` | |
| Bản đồ chapter | `Screen` | |
| Scene hội thoại | `Screen` | |
| HUD trận đấu | `HUD` | |
| Kết quả trận | `Popup` | |
| Pause | `Popup` | |
| Toast phần thưởng | `Notification` | `INotificationService` key-merging |
| Tooltip chỉ số | `Tooltip` | |
| Chuyển cảnh | `Overlay` | `ITransitionOverlay` |

> **Va chạm layer:** `Notification` là 275, `Overlay` là 300 ⇒ toast render **bên dưới** animation pull và
> transition overlay. Quy tắc: **hoãn mọi toast khi một Overlay đang hiện**, xả hàng đợi khi overlay đóng.
> Nếu không, phần thưởng quan trọng nhất của game là thứ duy nhất người chơi không nhìn thấy.

## 15. Out of scope — Phase 1

Multiplayer/PvP **và guild war** (cần người chơi khác, không dịch được sang offline — mục 13) ·
**Format chiến đấu thứ tư** (Aria / Gauntlet / Chorus là tập đóng ở Phase 1) · **Mode theo lịch** —
daily, weekly, sự kiện giới hạn thời gian ·
IAP, quảng cáo, analytics · Cloud save, tài khoản · Nhánh rẽ hội thoại · Voice acting ·
Live2D/Spine (**Phase 2** — Phase 1 dùng portrait tĩnh AI generate) · Banner giới hạn thời gian, sự kiện
theo lịch · Loại bỏ content pack khỏi bytes của build (**Phase 2**, cần Addressables) · Localization ·
Mã hoá/chống sửa save.

> **Đã bỏ khỏi danh sách này:** "max toàn bộ ★5". Từ khi Ascent cho cày Shard, max trọn ★5 là **phần
> thưởng của việc leo tháp**, không còn là thứ bị hoãn sang phase sau. Nó vẫn không đạt được nếu chỉ chơi
> campaign — xem hai mốc neo ở mục 11.

Save **không mã hoá** là **quyết định có ý thức**: game offline, không có gì để bảo vệ khỏi chính người chơi,
và mã hoá chỉ làm khó debug.

### Thiết kế xong nhưng CHƯA xếp phase — không phải out of scope, cũng không phải Phase 1

**Hệ trang bị** (mục 11). Nó nằm ở đây, trong một ô riêng, vì cả hai nhãn kia đều sai: gọi là out of scope
thì phủ nhận một thiết kế đã hoàn chỉnh, mà để trống thì người đọc audit phạm vi sẽ kết luận đó là hạng
mục Phase 1 bị bỏ sót.

> **Hoãn trang bị có một cái giá phải nói thẳng.** Mục 11 nêu "ba mode chưa có bản sắc" là một lỗ có thật,
> và **bộ đồ rơi theo mode là câu trả lời duy nhất** mà thiết kế này đưa ra cho nó. Nên nếu Phase 1 ship
> mà không có trang bị, nó ship **kèm theo một lỗ phân hoá đã biết mà chưa vá** — ba mode sẽ khác nhau ở
> luật chơi nhưng giống nhau ở lý do để chạy. Đây là đánh đổi có ý thức, không phải chuyện bỏ quên.

## 16. Assumptions & rủi ro

| # | Giả định / rủi ro | Trạng thái |
|---|---|---|
| 1 | **Repo không có một tí character art nào.** Spine runtime 4.3 đã cài nhưng **không tồn tại** `.skel`/`.atlas.txt`/`_SkeletonData.asset` nào | **Prerequisite phase**. Phase 1 dùng portrait tĩnh AI generate |
| 2 | Art AI khó giữ **consistency** giữa các biểu cảm của cùng một nhân vật | Chốt seed/prompt per nhân vật, sinh trọn bộ biểu cảm một lượt |
| 3 | **Lưới sưu tầm phải tự viết** — `RecyclerView` chỉ 1 trục, không grid, không snapping | Hạng mục kỹ thuật lớn nhất ngoài combat |
| 4 | **Không có dialogue system** | Mục 12 |
| 5 | Không có autosave / save-on-quit trong framework | Phải tự xây |
| 6 | Cân bằng Aria phụ thuộc hoàn toàn vào **AI có mix và có sàn trọng số** | Mục 5 |
| 7 | Toàn bộ số ở mục 9–11 **vẫn chưa playtest** | **Sim đã chạy 2026-09-20** (30k người chơi, fragment mô hình theo từng nhân vật): bốn ô trống đã lấp — 6 Shard/tầng · 40 tầng · 1.200 Dust/Catalyst · 600 Dust/lượt. Sim còn **bác bỏ** hai chỗ trong bản cũ (xem mục 11). **Đã port sang `UIFramework.AstralChorus.Logic`** (`noEngineReferences: true`, 45 test, chạy được ngoài Unity qua `tools/astral-chorus-economy-sim`). Bản Python giữ lại làm oracle đóng băng |
| 8 | Giá Catalyst và tốc độ thu Dust là một bài toán ghép đôi | **Đã giải.** Trần mới = **22,2 giờ cày** (444 lượt) ở 600 Dust/lượt, giá 1.200. Nghi phạm đúng là tốc độ Dust: hạ giá xuống 900 chỉ tiết kiệm 1,9 giờ |
| 9 | **Focus có thể làm nhạt cảm giác pull** nếu lệch quá mạnh | Sim đo: **60,2%** trên bậc 3 nhân vật, khớp giải tích 3/5. Banner 2 người vẫn là 75% — gần tất định. Chấp nhận có ý thức: game offline, targeting là tính năng chứ không phải rò rỉ doanh thu. Ngưỡng hạ xuống ×2 vẫn để ngỏ, chờ playtest chứ không chờ sim |
| 10 | Phase 1 một save profile, key và service **slot-independent** | Thêm slot sau không phải migrate |
| 11 | File save hỏng thì `ISaveService` **ném exception** (thiếu file thì trả null) | Phải có UX phục hồi — xem tài liệu kiến trúc |
| 12 | **Chi phí Dust để nâng trang bị KHÔNG nằm trong ngân sách 266k** ở mục 10 | Cố ý — trang bị chưa xếp phase (mục 15). Khi nào xếp phase thì phải tính lại toàn bộ ngân sách Dust, không phải cộng thêm vào |
| 13 | **Sàn campaign-only max được 1 ★5 ở trung vị, 47% max được 0** — hệ quả của giá quy đổi 10:1 | Cố ý (mục 11), nhưng **chưa playtest**. Nếu người chơi đọc "không max nổi ★5 nào" là *thất bại* chứ không phải *lời mời leo tháp*, thì đòn bẩy là **affinity ★5** (hiện +6), không phải giá quy đổi — chỉnh giá sẽ kéo sập lại trần 420 |
