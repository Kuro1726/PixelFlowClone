# Audit PixelFlowCloneUnity/Assets

Mình đã đọc `.claude/commands/uw-cmd-review.md`, `.claude/skills/uw-code-review/SKILL.md`, các standards trong `docs/`, rồi quét static toàn bộ 90 file C# dưới `PixelFlowCloneUnity/Assets/`. Không sửa code.

**Findings**

1. Medium - Hot-path không alloc rõ ràng trong `Update()`, nhưng có work không cần thiết mỗi frame/physics frame.
Không thấy lifecycle `Update()` thực sự trong runtime; các hot path là `LateUpdate()` / `FixedUpdate()`. Không phát hiện LINQ, `new`, string concat hay collection allocation trực tiếp trong các hàm này. Nhưng có vài chi phí vẫn đáng xử lý:

- [CollectorUnit.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorUnit.cs:217) gọi debug ray preview trong `FixedUpdate()` khi `OnConveyor`. Phần draw thật nằm trong `#if UNITY_EDITOR`, nhưng caller vẫn tính toán singleton/config/rigidbody/grid trong player build trước khi đi vào method rỗng. Bọc cả call hoặc method caller bằng `#if UNITY_EDITOR`.
- [GameplayInstruction.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/GameplayInstruction.cs:325) gọi `Camera.main` trong `LateUpdate()` tại line 337. Nên cache camera và refresh khi context/camera đổi.
- [LevelManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/LevelManager.cs:88) cũng gọi `Camera.main` trong `LateUpdate()` tại line 91. Nên cache camera hoặc chỉ refit khi resize/safe area thay đổi.

2. Medium - GC/event spike theo gameplay action, không nằm trong `Update()` nhưng sẽ đập vào feel.
Các đoạn này tạo coroutine / yield instruction / object runtime theo sự kiện consume, shot, audio, victory:

- [CollectorShotVfx.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorShotVfx.cs:98) `StartCoroutine(AnimateShot(...))` cho mỗi shot.
- [GridManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/GridManager.cs:169) `StartCoroutine(FinalizeConsumedBlockAfterDelay(...))`, và [GridManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/GridManager.cs:185) tạo `new WaitForSeconds(delay)`.
- [AudioManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/AudioManager.cs:104) start coroutine trả audio source về pool, và [AudioManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/AudioManager.cs:183) tạo `new WaitForSecondsRealtime(...)`.
- [VictoryPopup.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/UI/Popups/VictoryPopup.cs:405) spawn 14 coin và 10 sparkle mỗi lần win, kèm nhiều coroutine/yield ở [VictoryPopup.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/UI/Popups/VictoryPopup.cs:415), [VictoryPopup.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/UI/Popups/VictoryPopup.cs:438), [VictoryPopup.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/UI/Popups/VictoryPopup.cs:482). Đây là spike lúc victory; nên pool coin/sparkle và cache reusable waits nếu duration cố định.

3. Medium - Conveyor HUD update bị double path, gây rebuild text thừa.
[GameplayHUD.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/UI/GameplayHUD.cs:66) tự subscribe `GameEvents.OnConveyorCountChanged`, trong khi [UIManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/UIManager.cs:35) cũng subscribe event đó và gọi `_hud.SetConveyorCount(...)` ở [UIManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/UIManager.cs:234). Kết quả cùng một thay đổi count có thể update HUD hai lần. [GameplayHUD.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/UI/GameplayHUD.cs:281) dùng `string.Format("{0}/{1}", ...)`, nên mỗi path cũng tạo text mới. Nên chọn một owner duy nhất cho việc bind HUD.

4. High - `CollectorUnit` đang ôm quá nhiều trách nhiệm.
[CollectorUnit.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorUnit.cs:1) hiện xử lý input tap, state, movement, lane/raycast consume, điều phối Queue/Conveyor/Grid/Pool, capacity label, facing, reject shake, shot shake, exit animation. Các vùng đáng tách:

- Tap/input và trạng thái ở [CollectorUnit.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorUnit.cs:86).
- Consume/manager orchestration ở [CollectorUnit.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorUnit.cs:556).
- Queue/conveyor dispatch ở [CollectorUnit.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorUnit.cs:680).
- Direct singleton use như [CollectorUnit.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorUnit.cs:81), [CollectorUnit.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorUnit.cs:838), [CollectorUnit.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorUnit.cs:839).

Đây là điểm nóng kiến trúc lớn nhất vì mọi gameplay flow đang đi xuyên qua một MonoBehaviour.

5. High - Có dependency cycle giữa queue và conveyor, rồi `CollectorUnit` gọi trực tiếp cả hai.
[QueueManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/QueueManager.cs:89) gọi `ConveyorPathManager.Instance`, trong khi [ConveyorPathManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/ConveyorPathManager.cs:100) gọi `QueueManager.Instance`. `CollectorUnit` lại trực tiếp điều phối cả queue/conveyor/grid. Đây là spaghetti coupling: thay đổi rule queue dễ làm vỡ conveyor, và ngược lại. Nên đưa flow sang coordinator/service hoặc event contract rõ ràng.

6. Medium - `LevelManager` là God manager cấp scene.
[LevelManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/LevelManager.cs:1) đang gom progression, PlayerPrefs, scene loading/loading UI, pool cleanup, apply level, camera/background fitting. Ví dụ play routine ở [LevelManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/LevelManager.cs:186), scene return ở [LevelManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/LevelManager.cs:263), apply gameplay ở [LevelManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/LevelManager.cs:370), camera fitting ở [LevelManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/LevelManager.cs:417). Nên tách `LevelProgressionService`, `LevelSceneFlow`, `LevelApplier`, `CameraBoundsFitter`.

7. Medium - UI popup/HUD class trộn presentation, factory, navigation, animation.
`VictoryPopup`, `PausePopup`, `DefeatPopup`, `MainMenuScreen`, `GameplayHUD` đều vừa dựng runtime UI, vừa bind event, vừa chạy animation, đôi khi còn tạo sprite/texture procedural. Ví dụ [VictoryPopup.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/UI/Popups/VictoryPopup.cs:690) có `BuildRuntimeUi()`. Nên tách builder/factory/editor migration khỏi runtime presenter, nhất là với prefab UI đã tồn tại.

8. Medium - `GameplayContext` là service locator/spawner khá rộng, và có dấu hiệu thiếu init.
[GameplayContext.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Core/GameplayContext.cs:1) resolve reference, ensure persistent managers, ensure HUD/popup/vfx/background, register UI. Đáng chú ý: có method `EnsureConsumeVfx()` nhưng `Awake()` không gọi, trong khi các ensure khác được gọi. Cần xác nhận đây là bug wiring hay dead code.

9. Medium - Vi phạm rule logging không thương lượng.
Không tìm thấy `GameDebug` implementation/use trong runtime, nhưng có nhiều raw `Debug.Log*`, ví dụ [CollectorUnit.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Gameplay/CollectorUnit.cs:81), [QueueManager.cs](../PixelFlowCloneUnity/Assets/PixelFlowClone/Scripts/Managers/QueueManager.cs:118), và nhiều file manager/UI khác. Theo rules trong `.claude/skills/uw-code-review/SKILL.md`, đây là issue vì log/interpolated string có thể còn trong release.


**Refactor progress**

- [x] Viec 1 - Tach collector flow va pha vong phu thuoc Queue/Conveyor. Da them `CollectorFlowCoordinator`, chuyen orchestration khoi `CollectorUnit`, va bo direct singleton calls trong `CollectorUnit`.
- [x] Viec 2 - Don cac allocation/event spike theo consume/shot/audio.
- [x] Viec 3 - Tach UI builder khoi runtime presenter va chon mot owner cho HUD binding.

**Top 3 việc nên refactor ngay**

1. Tách collector flow và phá vòng phụ thuộc Queue/Conveyor.
Tạo một `CollectorFlowCoordinator` hoặc service tương đương để đứng giữa `CollectorUnit`, `QueueManager`, `ConveyorPathManager`, `GridManager`. `CollectorUnit` nên chỉ giữ view/state/movement và phát intent như tapped/arrived/can consume, còn manager orchestration chuyển ra ngoài. Đây là refactor có ROI cao nhất vì giảm rủi ro bug lan truyền.

2. Dọn các allocation/event spike theo consume/shot/audio.
Thay coroutine-per-shot/block/audio bằng scheduler/tick nhỏ hoặc pooled active state. Pool shot/victory visual objects, cache wait instruction nếu còn dùng coroutine với duration cố định. Phần này tác động trực tiếp tới feel khi combo/consume liên tục.

3. Tách UI builder khỏi runtime presenter, đồng thời chọn một owner cho HUD binding.
Popup class chỉ nên bind/show/hide/animate; build runtime UI, procedural texture/sprite, prefab migration nên nằm ở builder/editor/factory riêng. Riêng conveyor HUD, bỏ một trong hai event path `GameplayHUD` hoặc `UIManager` để tránh duplicate update.

**Quick wins ít rủi ro**

- Bọc debug ray preview trong `#if UNITY_EDITOR` từ caller ở `CollectorUnit`.
- Cache camera trong `GameplayInstruction` và `LevelManager`.
- Thêm `GameDebug` wrapper rồi migrate raw `Debug.Log*` theo từng cụm.
- Kiểm tra `GameplayContext.EnsureConsumeVfx()` có cần gọi trong `Awake()` không.

**Verification**

- Static audit: đã quét 90 file C# dưới `PixelFlowCloneUnity/Assets/`.
- Test mapping: 4 scenario trong `docs/GDD.md` được map bởi test hiện có cho Gameplay Instruction Tap Pulse và Collector Shot Shake.
- Game feel: 2 feature trong `docs/GFD.md` đang có implementation/test tương ứng.
- Build/tests: chưa chạy được trong sandbox ở lượt audit trước; báo cáo này dựa trên static review.
- Lưu ý repo: `docs/ProjectConfig.yaml` được AGENTS yêu cầu nhưng không tồn tại trong workspace khi audit.
