# Behavior Tree với Unity Behavior — Guide

Unity 6000.4.0f1 — package `com.unity.behavior`

Viết cho người **đã biết lý thuyết behavior tree** (Selector / Sequence / Decorator / Status) nhưng
chưa dùng trong Unity. Bỏ qua phần BT 101, đi thẳng vào API và tooling của package.

Phần thực hành click-through: [behavior-tree-lab-walkthrough.md](behavior-tree-lab-walkthrough.md)

> **Chưa chốt (cần verify trong Editor sau khi cài package):** 5 điểm ở mục 13. Docs của Unity tự mâu
> thuẫn ở những chỗ đó nên guide này không đoán bừa. Version cụ thể cũng sẽ pin lại theo con số
> Package Manager thực resolve, không theo con số đọc từ changelog.

---

## 1. Ánh xạ thuật ngữ — đọc mục này trước mọi thứ khác

Thứ làm người biết BT mất thời gian nhất với package này **không phải khái niệm, mà là từ vựng**.
Unity đặt tên khác gần như toàn bộ:

| Bạn biết | Unity Behavior gọi | Ghi chú |
|---|---|---|
| Selector / Fallback | **`Try In Order`** | Không tồn tại node nào tên "Selector" |
| Sequence | `Sequence` | Giống |
| Decorator | **Modifier** | `Cooldown`, `Inverter`, `Random`, `Repeat`, `Succeeder`, `Time Out` |
| Parallel | `Run In Parallel` | Có `Parallel Mode` chọn trong Inspector |
| Condition / điều kiện | **`Conditional Guard`** | **Là Action node** (category Blackboard), không phải decorator |
| Blackboard | Blackboard | Giống |
| Tick / Status | `Status.Success` / `Failure` / `Running` | Giống |

### Ba khác biệt về cấu trúc, không chỉ tên gọi

**a) Nó không phải cây.** Unity gọi là *behavior graph*, và nhánh **được phép merge lại**. Đó chính là
lý do tồn tại `Wait For All` / `Wait For Any` — hai node vô nghĩa trong một cái cây thuần, vì cây thì
không có chỗ nào để hai nhánh gặp nhau.

**b) Graph chạy xong là dừng, không tự lặp.** Không bọc `Repeat` ở gốc thì agent hành động đúng một
lượt rồi đứng im — **và không có lỗi nào báo cho bạn biết**. Docs nói thẳng: *"you can use a Repeat
node to loop indefinite behavior"*. Đây là lỗi số một của người mới.

**c) Có nhiều entry point.** Ngoài `On Start`, một graph có thể có thêm nhánh bắt đầu bằng
`Start On Event Message`. Nhánh đó nằm im cho tới khi nhận được event tương ứng.

---

## 2. Kiến trúc — ai giữ state, ai tick

Bốn thứ rời nhau, đừng gộp trong đầu:

| Thành phần | Dạng | Vai trò |
|---|---|---|
| **Behavior Graph** | `.asset` | Bản thiết kế. Không giữ state runtime. Nhiều agent dùng chung một graph |
| **Blackboard** | `.asset` hoặc khai báo trong graph | Khai báo biến. Có thể làm *shared blackboard* để nhiều graph đọc chung |
| **`BehaviorGraphAgent`** | `MonoBehaviour` | Thứ thật sự chạy. Mỗi agent tự **instance hoá** graph + blackboard riêng, và tick trong `Update()` |
| **Event Channel** | `ScriptableObject` | Kênh truyền message giữa các nhánh/các graph. Decoupling |

Điểm dễ hiểu sai: **graph asset là read-only lúc chạy**. Khi `BehaviorGraphAgent.Init()` chạy, nó tạo
một instance riêng. Hai enemy dùng chung `Enemy.asset` không giẫm chân nhau.

---

## 3. Node catalog

### Action nodes (thực thi logic)

| Nhóm | Node |
|---|---|
| Animation | `Set Animator Boolean` / `Float` / `Integer` / `Trigger` |
| Blackboard | `Set Variable Value`, **`Conditional Guard`** |
| Debug | `Log Message`, `Log Variable`, `Log Variable Change` |
| Delay | `Wait (Seconds)`, **`Wait (Range) (Seconds)`**, `Wait (Frames)` |
| Find | `Find Closest With Tag`, `Find With Tag` |
| GameObject | `Instantiate Object`, `Destroy Object`, `Attach Object`, `Set Object Active State`, `Set Object List Active State`, `Don't Destroy On Load` |
| Navigation | `Navigate To Location`, `Navigate To Target`, `Patrol` |
| Physics | `Add Force`, `Add Torque`, `Set Velocity`, `Check Collisions In Radius`, `Wait For Collision` / `Collision 2D` / `Trigger` / `Trigger 2D` |
| Resource | `Play Audio`, `Play Particle System` |
| Scene | `Load Scene`, `Unload Scene` |
| Transform | `Set Position`, `Set Position To Target`, `Set Rotation`, `Set Scale`, `Translate`, `Rotate`, `Scale`, `Look At` |

> **Cảnh báo cho project này:** nhóm **Navigation** (`Patrol`, `Navigate To Target`) chạy trên
> **NavMesh**. Project là 2D URP, không có NavMesh ⇒ ba node đó vô dụng. Phần di chuyển phải tự viết
> custom action. Đây là lý do lab có `MoveToward2DAction` chứ không phải vì thích tự làm.

### Event nodes

`On Start` · `Send Event Message` · `Start On Event Message` · `Wait for Event Message`

### Flow nodes

- **Modifier:** `Cooldown`, `Inverter`, `Random`, `Repeat`, `Succeeder`, `Time Out`
- **Sequencer:** `Sequence`, `Try In Order`, `Conditional Branch`, `Switch`, `Switch Flag`,
  `Run In Parallel`, `Wait For All`, `Wait For Any`, `Abort`, `Restart`

### Subgraph nodes

`Run Subgraph` · `Run Subgraph Dynamically`

---

## 4. Viết custom node

Đây là phần bạn sẽ dùng nhiều nhất, vì node dựng sẵn không bao giờ đủ.

```csharp
using System;
using Unity.Behavior;
using Unity.Properties;
using UnityEngine;
using Action = Unity.Behavior.Action;   // tránh đụng System.Action

[Serializable, GeneratePropertyBag]
[NodeDescription(
    name:     "Move Toward 2D",
    story:    "[Agent] moves toward [Target] at [Speed]",
    category: "Action",
    id:       "739a0711c5da6e4ca560ae951d045b61")]
public partial class MoveToward2DAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Agent;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float>      Speed = new(3f);

    protected override Status OnStart()
    {
        // Fail sớm còn hơn Running vĩnh viễn — node treo là lỗi khó tìm nhất trong BT
        if (Agent?.Value == null || Target?.Value == null) return Status.Failure;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var self   = Agent.Value.transform;
        var target = Target.Value;
        if (target == null) return Status.Failure;   // target bị Destroy giữa chừng

        self.position = Vector3.MoveTowards(
            self.position, target.transform.position, Speed.Value * Time.deltaTime);

        return Vector3.Distance(self.position, target.transform.position) < 0.1f
            ? Status.Success
            : Status.Running;
    }

    protected override void OnEnd() { }
}
```

### Từng mảnh nghĩa là gì

**`partial`** — bắt buộc. `[GeneratePropertyBag]` là source generator, nó sinh phần còn lại của class.
Bỏ `partial` là compile error khó hiểu.

**`story:`** — không phải comment. Chuỗi này **sinh ra giao diện node**: mỗi `[Tên]` trong story thành
một link field kéo-thả trong GUI, khớp theo tên với field `BlackboardVariable` cùng tên trong class.
Sửa story là sửa luôn hình dạng node.

**`id:`** — GUID **phải duy nhất**. Copy-paste một node cũ rồi quên đổi `id` là cách nhanh nhất để hai
node ăn cắp dữ liệu của nhau.

**`[SerializeReference] BlackboardVariable<T>`** — không phải `T` trần. Bọc trong `BlackboardVariable<T>`
để node đọc/ghi được biến dùng chung thay vì giữ bản sao riêng. Đọc giá trị qua `.Value`.

**Vòng đời:**

| Hàm | Khi nào chạy | Trả về |
|---|---|---|
| `OnStart()` | đúng 1 lần khi node được kích hoạt | `Success` / `Failure` → kết thúc luôn; `Running` → sang `OnUpdate` |
| `OnUpdate()` | mỗi tick, chừng nào còn `Running` | |
| `OnEnd()` | khi node kết thúc (kể cả bị abort) | `void` — chỗ dọn dẹp |

**`Status.Running`** là cách node giữ quyền điều khiển qua nhiều frame. Node trả `Running` mãi mà không
có đường thoát = agent đứng hình, và BT **không** cảnh báo. Luôn tự hỏi: node này thoát `Running` bằng
điều kiện nào?

---

## 5. Blackboard

- Biến khai báo trên blackboard của graph, hoặc trên một **Blackboard asset** dùng chung nhiều graph
- Từ 1.0.15, blackboard hiện ra và **override được ngay trên Inspector của `BehaviorGraphAgent`** —
  tiện cho việc cho mỗi enemy một `Speed` khác nhau mà vẫn dùng chung graph
- Có hỗ trợ cast giữa các kiểu biến

Quy tắc thực dụng: **mỗi biến nên có đúng một node chịu trách nhiệm ghi.** Trong lab,
`ScanForTargetAction` là nguồn sự thật duy nhất của `Target` — thấy thì ghi, không thấy thì **set null**.
Nếu nhiều node cùng ghi một biến, việc truy ra ai làm hỏng nó gần như bất khả thi.

---

## 6. Events & Event Channels

Event Channel là một `ScriptableObject` mang message có tham số.

Tạo: chuột phải trong graph editor → `Add` → `Event` → `Send Event Message` → bấm icon link ở ô
**Event Channel** → `Create Event Channel` → đặt tên + chọn kiểu tham số → `Create`. Channel xuất hiện
trên Blackboard.

Bốn node: `On Start`, `Send Event Message` (phát), `Start On Event Message` (nhận, **tạo entry point
mới**), `Wait for Event Message` (nhận, chặn nhánh hiện tại cho tới khi có message).

**Khi nào dùng event thay vì poll blackboard:** khi sự kiện là *tức thời và một lần*, không phải trạng
thái kéo dài. "Vừa phát hiện địch" là event. "Đang có địch" là biến blackboard. Nhầm hai thứ này là lý
do BT hay bị mất sự kiện hoặc xử lý lặp.

> **Bẫy:** `Send Event Message` mà không có node nào nhận thì **không báo lỗi gì cả** — event bắn vào hư
> không. Wire receiver trước, rồi mới wire sender.

---

## 7. Subgraph

Graph con tái sử dụng, gọi bằng node `Run Subgraph` (hoặc `Run Subgraph Dynamically` để chọn graph lúc
runtime). Vai trò như hàm trong code.

> **Bẫy:** biến của subgraph **phải được expose lên blackboard** thì mới map được tại node gọi. Không
> map thì subgraph chạy trên **giá trị default** — im lặng, không exception, và bạn sẽ debug nhầm chỗ.

---

## 8. Conditional abort / ObserverAbort

Vấn đề kinh điển của BT: agent đang chạy một nhánh dài (đi tới điểm A), giữa chừng điều kiện thay đổi
(địch xuất hiện), nhưng nhánh đang `Running` nên không ai kiểm tra lại → agent đi tiếp như chưa có gì.

BT cổ điển giải bằng *conditional abort* trên decorator. Unity Behavior bổ sung **ObserverAbort** từ
1.0.15, kèm node `ConditionalGuardModifier`.

> Mục này chưa viết chi tiết được: tên node thật trong GUI và chỗ bật ObserverAbort cần xác nhận trong
> Editor (xem mục 13, điểm 2). Sẽ bổ sung sau khi cài package.

---

## 9. Runtime API

```csharp
var agent = enemy.GetComponent<BehaviorGraphAgent>();

// Ghi TRƯỚC Init() -> thành agent-level override, KHÔNG bị mất
agent.SetVariableValue("Speed", 5f);

agent.Init();       // tạo instance riêng của graph + blackboard, áp mọi override
agent.Start();      // bắt đầu chạy

// Ghi SAU Init() -> ghi thẳng vào graph instance
agent.SetVariableValue("Target", player);

if (agent.GetVariable<GameObject>("Target", out var target))
    Debug.Log(target.Value);
```

| Thành viên | Chữ ký | Ghi chú |
|---|---|---|
| `Graph` | `BehaviorGraph` get/set | Gán graph mới lúc runtime → tự init ở `Update` kế tiếp |
| `BlackboardReference` | get | **Trả `null` + warning nếu đọc trước khi init** |
| `Init()` | `void` | Tự chạy trong `Awake()` nếu graph gán sẵn ở Inspector |
| `Start()` / `End()` / `Restart()` | `void` | |
| `SetVariableValue<T>` | `(string, T)` → `bool` · `(SerializableGUID, T)` → `bool` | Trả `bool`, **nên check** |
| `GetVariable<T>` | `(string, out BlackboardVariable<T>)` → `bool` | Có cả bản untyped và bản theo GUID |
| `GetVariableID` | `(string, out SerializableGUID)` → `bool` | Lấy GUID để tránh tra theo string trong hot path |

> **Gotcha quan trọng nhất của mục này:** rất nhiều người tưởng set biến trước `Init()` là mất. **Không
> mất** — nó thành agent-level override. Thứ *thật sự* null trước init là `BlackboardReference`.

---

## 10. Debug

Vào Play Mode rồi mở graph asset: editor **highlight node đang chạy theo thời gian thực**. Đây là công
cụ debug chính — nhìn node nào đang sáng `Running` là biết agent kẹt ở đâu.

Kèm theo: `Log Message`, `Log Variable`, `Log Variable Change` (action nodes) để in ra Console mà không
phải viết custom node.

---

## 11. Gotchas & khi nào KHÔNG dùng BT

**Đừng biến BT thành visual scripting.** Cám dỗ lớn nhất là làm một node cho mỗi lời gọi Unity rồi nối
lại thành chương trình. Kết quả là một cái graph khổng lồ khó đọc hơn code nó thay thế. Mỗi node nên
gói một **hành vi trừu tượng, đủ lớn** ("tìm chỗ nấp", "áp sát mục tiêu"), không phải một câu lệnh.

**BT hợp với:** AI có nhiều hành vi cạnh tranh, ưu tiên rõ ràng, cần designer chỉnh mà không cần
compile.

**BT không hợp với:** luồng tuyến tính (cutscene, tutorial), logic UI, state machine ít trạng thái và
chuyển tiếp rõ ràng. Với những thứ đó, một FSM hoặc code thẳng ngắn và dễ debug hơn nhiều.

**Chi phí tick.** Mỗi agent tick graph mỗi frame. Trăm agent = trăm lần duyệt cây. Với bullet-hell hàng
trăm enemy, cân nhắc giữ BT cho boss/elite và dùng logic đơn giản cho đám đông.

**Package còn ở 1.0.x.** Đã có ghi nhận bug từ cộng đồng (ví dụ nhúng biến trực tiếp vào subgraph thay
vì expose). Đọc changelog trước khi nâng version.

---

## 12. Đối chiếu với codebase này

[`BossController.cs`](../Assets/UIFramework/Features/AircraftStriker/Scripts/Gameplay/BossController.cs)
là ví dụ gần nhất trong repo với thứ BT sinh ra để thay thế:

```csharp
BulletPatternConfig[] patterns = CurrentPhase switch
{
    0 => _wave.BossPhase1Patterns,
    1 => _wave.BossPhase2Patterns,
    _ => _wave.BossPhase3Patterns
};
```

Một coroutine `FireRoutine()` + `switch` theo `CurrentPhase` (ngưỡng máu 0.75 / 0.50). Dịch sang graph
thì thành `Try In Order` với ba nhánh phase, mỗi nhánh là `Sequence` các pattern bắn.

**Nhưng đừng port vội — và đây mới là bài học đáng giá nhất của mục này.** Phase transition trong
`BossController` là **event-driven**: `TakeDamage()` phát hiện đổi phase và bắn callback **đúng một
lần**. Một BT ngây thơ poll máu mỗi tick sẽ **mất ngữ nghĩa single-fire đó** — nó biết "đang ở phase 2"
nhưng không biết "vừa mới chuyển sang phase 2". Muốn giữ, phải cố tình dựng lại bằng Event Channel.

Bài học tổng quát: **BT giỏi mô tả *trạng thái đang là gì*, kém mô tả *khoảnh khắc chuyển đổi*.** Chỗ
nào logic của bạn quan tâm tới khoảnh khắc, đó là chỗ BT cần được bổ sung bằng event, hoặc đơn giản là
không nên dùng BT.

---

## 13. Những điểm chưa chốt

Docs của Unity tự mâu thuẫn ở 5 chỗ. Verify trong Editor sau khi cài package rồi cập nhật lại mục này:

1. **`Abort` hay `Fail`?** Changelog 1.0.15 ghi đã đổi tên `Abort` → `Fail`; trang node-types của
   1.0.16 vẫn liệt kê `Abort`.
2. **`ConditionalGuardModifier` vs `Conditional Guard`** — changelog 1.0.15 giới thiệu node modifier
   mới, node-types liệt kê `Conditional Guard` là Action. Hai thứ khác nhau, cần xác nhận label GUI và
   chỗ bật ObserverAbort (ảnh hưởng mục 8).
3. **`On Start` có toggle Repeat không?** Danh sách modifier có nhắc "OnStart" nhưng docs không mô tả.
4. **`Wait For All` / `Wait For Any`** là Sequencer hay nhóm Join riêng — hai trang docs nói lệch nhau.
5. **`Repeat` xử lý Failure của child thế nào** — dừng hay lặp tiếp? Ảnh hưởng trực tiếp tới việc graph
   có sống sót qua một vòng fail hay không.

---

## Nguồn

- [About Unity Behavior](https://docs.unity3d.com/Packages/com.unity.behavior@1.0/manual/index.html)
- [Behavior graph node types](https://docs.unity3d.com/Packages/com.unity.behavior@1.0/manual/node-types.html)
- [Use event nodes](https://docs.unity3d.com/Packages/com.unity.behavior@1.0/manual/event-nodes.html)
- [Behavior graphs](https://docs.unity3d.com/Packages/com.unity.behavior@1.0/manual/behavior-graph.html)
- [Create a custom node](https://docs.unity3d.com/Packages/com.unity.behavior@1.0/manual/create-custom-node.html)
- [BehaviorGraphAgent API](https://docs.unity3d.com/Packages/com.unity.behavior@1.0/api/Unity.Behavior.BehaviorGraphAgent.html)
- [Changelog](https://docs.unity3d.com/Packages/com.unity.behavior@1.0/changelog/CHANGELOG.html)
