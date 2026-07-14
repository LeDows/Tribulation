# Unity 性能防回归手册

本文记录 2026-07-14 对 `ProfilerCaptures` 的分析结果和项目级约束。涉及敌人生成、弹体、经验球、暂停、HUD、血条、物理或 URP 的改动，都应先阅读本文。

## 1. 本次问题留下的证据

采集来自 Unity 6000.3.2f1 Editor，共约 1112 帧、3.9 GB。该采集启用了高粒度/Deep Profile，平均每帧约 6.3 万个采样点，因此 CPU 绝对耗时被采样本身明显放大；这些数字适合定位调用链，不应当作发布版帧时。

仍然可以确认以下根因：

| 症状 | Profiler 证据 | 根因 |
| --- | --- | --- |
| 首个敌人生成卡顿 | `WaveSpawner.SpawnEnemy` 约 42 ms，其中 `Resources.Load` 约 40.5 ms、文件读取约 38.9 ms | 在战斗生成路径同步加载 Prefab |
| 中后期某次生成突刺 | `Object.Instantiate` 约 150.5 ms，`TransformHandle.SetHierarchyCapacity` 约 149.9 ms | RunRoot 层级没有预留容量，动态对象持续扩容 |
| 经验球和敌方弹体极端尖峰 | `Renderer.GetMaterial_Injected` 在采样中出现约 2256 ms、936 ms、885 ms | `renderer.material` 为每个实例克隆材质 |
| 暂停后仍有 CPU 调度 | 暂停帧仍调度 Enemy、Projectile、Orb、Spawner、Weapon、HUD 等回调 | `Time.timeScale = 0` 不会停止普通生命周期回调 |
| GPU 帧时过高 | 稳定段 GPU P95 约 36 ms；主光阴影、DrawProcedural、SSAO/Bloom 等占用明显 | URP 功能超过当前简单场景需要，动态小物体也启用了重型 Renderer 选项 |
| GC 和 UI 开销持续发生 | HUD 每帧格式化字符串并重建武器视图；每个敌人都有独立世界空间 Canvas | UI 刷新频率和脏写次数过高 |

## 2. 动态生成与对象生命周期

### 必须遵守

- 运行时 Prefab 的 `Resources.Load` 只能通过 `RuntimePrefabCatalog` 的缓存路径使用。`ConfigCenter` 加载配置文本不受此条限制；战斗中的生成函数仍不得直接同步加载资源。
- Enemy、Projectile、ExperienceOrb 必须使用 `InstantiatePooled` / `ReleasePooled`，不能恢复为反复 `Instantiate` / `Destroy`。
- 对象池目标数量来自 `maps.xml` / `SpawnConfig`，并通过分帧预热准备。RunRoot 创建时必须根据 Prefab 子层级数量预留 `hierarchyCapacity`。
- 新增动态高频对象时，同步补齐：预加载、池化键、预热配置、层级容量估算、释放路径和回归测试。
- 获取和释放必须成对。复用前必须重置位置、旋转、缩放、生命期、命中记录、回调、冷却、DOT、血量、Canvas 状态和其他单次生命数据。
- 复用敌人时，跨生命期的命中身份必须包含 `EnemyHealth.SpawnVersion`，不能只用组件引用判断“已经命中”。
- 不要在 Update、攻击或生成热路径使用 `FindObjectsByType<EnemyHealth>`。使用 `EnemyHealth.ActiveEnemies`，且伤害可能移除当前敌人时采用反向遍历。
- 临时目标集合应复用 List/Dictionary，避免每次攻击创建数组、列表或闭包。

### 材质规则

- 动态实例禁止访问 `renderer.material`。
- 仅修改颜色时使用 `RuntimePrefabCatalog.SetRendererColor` 的 `MaterialPropertyBlock` 路径。
- 如果未来需要多个材质属性，先确认 MPB、SRP Batcher 和 GPU Instancing 的实际批次表现，再决定使用共享材质变体还是 MPB。

### 物理规则

- 会移动的 3D Collider 不能作为“移动静态碰撞体”。敌人应保留无重力 Kinematic Rigidbody。
- 修改 Rigidbody 约束时必须确认位掩码和 Inspector 结果；当前敌人只冻结 Y 位置以及 X/Z 旋转，不能冻结 X/Z 移动。
- 不要为了性能擅自修改碰撞规则或伤害判定。Layer 和碰撞矩阵优化必须先验证实际交互集合。

## 3. 暂停不是停止回调

`Time.timeScale = 0` 只停止缩放时间和物理步进，不会自动停止：

- `Update` / `LateUpdate`
- UI 刷新和字符串格式化
- Renderer、Canvas 和相机渲染
- 某些已经排队的触发回调

因此：

- 所有游戏模拟入口，包括 Update、FixedUpdate、LateUpdate 和相关 Trigger，必须先检查 `GameManager.IsSimulationRunning`。
- 突破、属性分配、GameOver 和主菜单使用较低暂停帧率；恢复运行时必须还原进入暂停前的目标帧率，不能永久硬编码为 60 FPS。
- 暂停 UI 需要的 `unscaledTime` 只能用于 UI 自身，不能继续推进武器、敌人、弹体、掉落物或 DOT。
- 新增运行时组件时，应在代码审查中明确回答：“突破界面打开时，这个回调是否仍会执行？”

## 4. 渲染和 UI 约束

### 先核对真实引用

- 不能根据文件名猜测正在使用的 Render Pipeline Asset、Renderer Data 或 Volume Profile。
- 必须沿 Scene、Camera、URP Asset 的 GUID/序列化引用确认真实资产。
- 当前 PC 路径使用：
  - `Assets/Settings/PC_RPAsset.asset`
  - `Assets/Settings/PC_Renderer.asset`
  - `Assets/Settings/SampleSceneProfile.asset`
- `DefaultVolumeProfile.asset` 不是当前 Start 场景的活动 Volume；不要改错相似资产。

### 当前简单场景的默认基线

- 普通 Forward；没有明确需求时不要启用 Forward+。
- SSAO、Opaque Texture、Depth Texture、HDR、GPU Resident Drawer 和 Bloom 默认关闭；开启前必须指出使用者并提供前后 Profiler 数据。
- 主光阴影保持 1024、2 Cascades、约 30 米和低质量软阴影，除非视觉验收明确要求提高。
- Enemy 和 Ground 可以接收阴影，但大量动态 Enemy、Projectile、ExperienceOrb 和 Ground Tile 不应投射无收益阴影。
- Projectile、ExperienceOrb、Enemy、Ground 默认关闭 Motion Vectors、Light Probes、Reflection Probes 和 Ray Tracing 参与。

### Canvas 与 HUD

- 敌人满血时 World Space Canvas 必须关闭；血量变化由伤害事件更新，不能让所有敌人每帧写 fillAmount。
- 只有受伤且已启用的血条才执行朝向相机逻辑。
- HUD 高频数据最多约 10 Hz 刷新，只有值变化时才写 Text。静态或事件型字段应由事件触发。
- 不要在 HUD 每帧调用 `GetComponent`、创建武器视图集合、拼接大量字符串或无条件改写文本。

## 5. 不能用隐藏玩法规则换性能

以下措施会改变玩法，未经明确确认不得加入：

- 强制合并经验球或把远处经验移动到另一颗球的位置
- 设置经验球活跃数量上限并丢弃掉落
- 自动拾取、超时消失或改变经验奖励
- 限制玩家通过突破获得的弹体数量
- 降低敌人数、生成频率、武器伤害或攻击次数

当前经验球池只减少已拾取对象的创建/销毁，不限制同时活跃的经验球。若长局 Profiler 证明未拾取经验球仍是瓶颈，应先向用户说明“视觉批处理、逻辑合并、自动回收”等方案各自对玩法的影响，再选择实现。

## 6. 性能改动完成标准

每次相关改动至少完成以下检查：

1. 搜索回归热点：
   - `.material`
   - `FindObjectsByType<EnemyHealth>`
   - 高频路径中的 `Resources.Load`
   - Enemy、Projectile、ExperienceOrb 的直接 `Instantiate` / `Destroy`
2. 检查池化对象的获取、释放和复用字段重置是否成对。
3. 检查所有新增生命周期回调和 Trigger 是否有运行状态门禁。
4. 检查 Scene/URP/Volume 的真实 GUID 引用，不只看资产名称。
5. 对 XML、Prefab 和渲染资产运行差异检查，确认没有覆盖无关序列化内容。
6. 在条件允许时运行 Unity 导入/编译和完整 EditMode 测试；当前基线为 31 项测试全部通过。无法运行时必须说明原因。
7. 在条件允许时使用 Development Build、关闭 Deep Profile，预热后采集 300-600 个代表帧；无法重新采样时必须明确说明尚未完成效果验证。采样至少记录：
   - CPU Median / P95
   - GPU Median / P95
   - GC Alloc / frame
   - 首次和持续 Spawn 的最大耗时
   - Pause 状态的 CPU/GPU 与回调数量
8. 不要用 Editor + Deep Profile 的绝对帧时宣称发布版性能改善；它只能用于定位调用链。

## 7. 修改前优先检查的文件

- `Assets/Scripts/Core/RuntimePrefabCatalog.cs`
- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/Enemies/WaveSpawner.cs`
- `Assets/Scripts/Enemies/EnemyHealth.cs`
- `Assets/Scripts/Combat/Projectile.cs`
- `Assets/Scripts/Combat/AutoWeapon.cs`
- `Assets/Scripts/Pickups/ExperienceOrb.cs`
- `Assets/Scripts/UI/GameUiController.cs`
- `Assets/Scripts/Enemies/EnemyHealthBar.cs`
- `Assets/Resources/Prefabs/Enemy.prefab`
- `Assets/Resources/Prefabs/Projectile.prefab`
- `Assets/Resources/Prefabs/ExperienceOrb.prefab`
- `Assets/Resources/Prefabs/Ground.prefab`
- `Assets/Settings/PC_RPAsset.asset`
- `Assets/Settings/PC_Renderer.asset`
- `Assets/Settings/SampleSceneProfile.asset`

修改这些路径时，应保留现有池化、暂停门禁、轻量 Renderer 和事件驱动 UI 的设计意图；若必须替换，应先用新的 Profiler 证据说明原因。
