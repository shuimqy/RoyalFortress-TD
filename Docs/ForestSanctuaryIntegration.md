# 图二：圣林加护分层场景

更新：`Assets/Scenes/ForestSanctuaryGameplay.unity` 已接入主菜单，使用现有金币、拖卡建塔、暂停与地图导航系统，含八个建塔位、三条路线、八波敌人与脉冲伤害/减速。300 初始金币；旧点击菜单在运行时隐藏。下面保留纯分层演出场景的说明。

已接入 Unity 6000.3.27f1，沿用原亮度、PPU64、Point 过滤和无压缩精灵。包含干净底图、8 组 32 张透明动画帧、可复用预制体、演出控制脚本，以及与现有波次控制器连接的适配器。

## 打开与运行

1. 打开 `Assets/Scenes/ForestSanctuary.unity`，点击 **Play**。
2. 贤者施法、勇者跪立、水晶悬浮/内光和两段能量流自动循环。
3. 左上角点击「自动演示八波」，观看充能、净化脉冲和完成仪式；也可逐次点击「模拟守住一波」。
4. 第 2、4、6 波守住后可释放一次脉冲，最多储存一次。点击「释放净化脉冲」或地图中央水晶均可释放；仪式进度不减少。
5. 第 8 波守住且目标存活后，水晶开裂、碎片转移、勇者起身、贤者收手，最终保持加护完成。重复触发被拒绝。
6. 支持暂停、逐帧、重置和仪式中断预览。滚轮缩放，中键/右键拖动，Home 恢复全图。

本场景用于分层演出接入与检查。按钮模拟已守住波次，地图没有配置第二关敌人路线、建塔或真实伤害。八个石台保留为定位点，后续确认路线后继续制作战斗场景。

## 文件

| 位置 | 内容 |
|---|---|
| `Assets/Art/ForestSanctuary/` | 32 张动画帧、1536×1024 底图和源文件映射 |
| `Assets/Prefabs/ForestSanctuary/ForestSanctuaryPresentation.prefab` | 完整分层预制体，含锚点、脉冲和点击区域 |
| `Assets/Materials/ForestSanctuary/OriginalColours.mat` | 原亮度 Sprite Unlit 材质 |
| `Assets/Scripts/ForestSanctuary/ForestSanctuaryPresentation.cs` | 播放、仪式进度、脉冲储备及一次性结局 |
| `Assets/Scripts/ForestSanctuary/ForestSanctuaryEncounter.cs` | 现有 `WaveGameController` 的事件适配器 |

人物以脚底为 Pivot `(0.5,0.075)`，水晶与特效使用中心 Pivot。贤者、勇者地面点分别为地图像素 `(704,447)`、`(883,461)`；水晶中心为 `(796,364)`。能量起止点跟随独立 Socket，勇者胸前 Socket 随起身姿势调整；四组转移光点和胸前标记复用现有水晶精灵。

## 战斗场景连接

预制体上已有 `ForestSanctuaryEncounter`，当前 `Game` 留空。后续配置好 **八波**的 `WaveGameController` 后，将它拖入 `Game`。适配器会关闭模拟按钮，订阅 `StateChanged`，只在出怪结束、场上敌人清空后同步 `CompletedWaves`。波次开始、仅出怪结束或仍有敌人均不推进仪式；目标生命为零会中断。

也可直接调用 `NotifyWaveCleared(波次编号, 当前生命)`，必须按 1～8 顺序且每波只能接受一次；目标损失生命调用 `ReportHomeLives`。

- `TryReleasePulse()` 消耗一枚储备，播放脉冲，并触发 `onPurificationPulse`。伤害、减速与命中范围在战斗层连接此事件；本轮仅接入视觉与储备规则。
- `onBlessingCompleted` 在 4.8 秒完成演出结束时触发一次，用于后续结算/战役解锁。
- 重开调用 `ResetPresentation()` 或重载场景；正式战斗不应调用演示用的模拟按钮。

## 验证

实际 Unity Play 模式检查记录位于工程旁 `output/unity-forest-sanctuary/verification.json`，同目录提供循环、脉冲、开裂、碎片、起身与中断截图。

已检查多帧自动播放、暂停/逐帧、人物固定位置、能量 Socket、精灵导入设置、充能上限、脉冲不扣进度、一次性结局与失败分支。另用现有敌人和波次控制器跑完八波临时测试，每波两次延迟出怪，验证必须等后续出怪结束且敌人清空才推进；实际漏怪使目标生命归零时也能中断仪式。测试对象均在检查后清理，没有保存为地图路线。

原图、提示词、切帧脚本和离线预览保存在工程旁 `output/animation-pilot/forest-sanctuary-v1/`。本轮未构建独立程序。
