# 待解决 Bug 清单

> 记录开发过程中发现但尚未解决的问题，集中管理，后续统一处理。

---

## BUG-001：枪械左右手切换时 Y 轴上下漂移

**状态**：🟡 待解决

**出现位置**：`WeaponViewModel.cs`（枪械视角层）

**问题描述**：
左右手持枪切换（`Hand Side` 从 `Right` 切到 `Left`）时，枪械在 Y 轴（垂直方向）发生上下漂移，而非仅在 X 轴（水平方向）镜像。正确行为应是：切换时枪械仅水平镜像，Y 坐标保持不变。

**当前表现**：
- 枪口朝向已正确（右手朝左上、左手朝右上，均对准准星）✅
- 但切换手时，枪的垂直位置会上下移动 ❌

**背景与已排查方向**：
- v2~v6 多个版本均存在此问题，根因与旋转支点（pivot）的设置有关。
- 部件统一朝右、pivot 恒定后，水平镜像正常，但垂直方向仍有偏移，疑似 pivot 的 Y 值与实际握把位置未完全对齐，或 `anchoredPosition` 定位点（pivot 点）在切换时产生 Y 偏移。
- 当前版本：v6（`_backups/WeaponViewModel_v5_枪口对准_左右手有Y偏移.cs` 为上一版备份）。

**影响**：
- 影响观感，不阻塞核心玩法（瞄准、射击）。
- 后续接入真实枪械贴图时需一并修正。

**下一步建议**：
- 通过 Debug 输出 `handScreen`、`weaponRoot.anchoredPosition`、`pivot` 的运行时值，对比左右手切换差异，定位 Y 偏移来源。
- 或改用"左手时握把 Y 坐标减去握把局部偏移"的方式显式补偿。

**发现日期**：2026-08-08

---

## BUG-002：启动时玩家初始视角相对生成长方体中心向左偏移

**状态**：🟡 待解决

**出现位置**：`TargetManager.cs`（靶球管理器，启动自动定位）

**问题描述**：
启动游戏时，自动定位逻辑将生成长方体放到相机正前方（`Auto Place In Front Of Camera`），并尝试让玩家正对区域中心（`Auto Rotate Player To Face Area`），但玩家初始视角仍相对长方体中心**向左偏移约 20°**，无法正对区域中心。

**当前表现**：
- 球能生成在视野前方，启动无需转身 ✅
- 但初始朝向偏左约 20°，玩家需要手动右转才能正对区域中心 ❌

**背景与已排查方向**：
- 场景结构：`Player`（位置 -1.43, 0, -11.43，旋转 0,0,0）+ 子物体 `PlayerCamera`（位置 0,0,0，旋转 0,0,0）。
- autoPlace 将区域放在相机 forward 方向，并设置 `playerRoot.rotation = LookRotation(区域中心-玩家位置)`。
- 但启动后仍有 20° 偏移，疑似：PlayerController 的视角旋转在 `Awake`/`Update` 中与 autoRotate 冲突，或 `PlayerCamera` 的实际朝向与 Player 根物体朝向不一致（相机可能挂在 PlayerCamera 子物体上，其自身旋转虽为 0，但父物体 Player 的旋转被 PlayerController 或其它逻辑覆盖）。
- `PlayerController` 在 `Update` 中持续根据鼠标设置相机 pitch，但 yaw 是绕 Player 根物体旋转——启动瞬间 autoRotate 设置的旋转可能被后续 Update 覆盖或叠加。

**影响**：
- 影响开局体验（需小幅转动视角），不阻塞核心玩法。
- 后续做正式开场动画/初始状态时可一并修正。

**下一步建议**：
- 在 `Start` 中先 autoRotate，再让 `PlayerController` 在启动后第一帧重置 yaw 基准，避免冲突。
- 或将"玩家初始朝向"改为场景中手动摆放 Player 的旋转（去掉 autoRotate），让玩家在编辑器中摆好初始视角。
- 排查 `PlayerController` 是否在 `Awake` 或首帧 `Update` 覆盖了旋转。

**发现日期**：2026-08-08

---
