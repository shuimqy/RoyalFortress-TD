# 图三：亡者之门分层场景

可玩入口为 `Assets/Scenes/GateOfTheDeadGameplay.unity`：新版枯骨墓域底图已接回现有十个塔位、七波与三路，沿用金币、拖卡建塔和暂停系统。三处补充火盆及火焰复制了美术对照场景中保存的位置，并分别放在 `Necropolis Environment Props / Purple Brazier 3～5` 下，可以移动整个组合。

美术对照场景 `Assets/Scenes/GateOfTheDeadArtV2.unity` 保留，Play 模式右上角可切换「新版底图 / 原版对照」。以下说明继续对应原版分层演出场景。

使用项目版本 Unity 6000.3.27f1。按当前确定的**原亮度**接入，所有精灵使用 Unlit 材质和白色 Tint。

## 打开与运行

1. 打开 `Assets/Scenes/GateOfTheDead.unity`。
2. 点击 Play：两个人物施法、传送门、对波、五处紫火、两处橙火、上下符文和四块悬石自动循环。
3. 左上角可以暂停、重置、拖动对波推进，或播放一次封印演出。
4. 滚轮缩放，鼠标中键或右键拖动；Home 恢复全图。

## 文件

- `Assets/Art/GateOfDead/`：108 张动画帧、底图、独立静态层和传送门椭圆遮挡贴图。原始尺寸、PPU64、Point、无压缩、无 mipmap。
- `Assets/Art/GateOfDead/ImportManifest.json`：实际导入的源文件与 Assets 路径对应表。
- `Assets/Prefabs/GateOfDead/GateOfDeadPresentation.prefab`：已注册位置、锚点和材质的完整分层预制体。
- `Assets/Materials/GateOfDead/OriginalColours.mat`：原亮度 Unlit 材质。
- `Assets/Scripts/GateOfDead/GateOfDeadPresentation.cs`：帧播放、符文 Alpha、悬石位移、光束铺设和封印演出。

这里是图三的分层场景与演出接入。十个地基保留为空定位点，后续可继续配置本关路径、波次和建塔逻辑。

## 动画与事件

紫火和橙火各复用一套 6 帧，在各位置错开相位。火根 Pivot 和物体位置固定。符文仅改变独立光纹层的 Alpha，石柱、石台保留在底图中。悬石使用固定身体精灵加 3／4 秒正弦浮动，不同时播放已导出的浮动帧，以免位移叠加。

两束光使用 Tiled SpriteRenderer，从固定的剑尖／法杖 Socket 接到同一碰撞点，随推进改变长度，保持纹理宽度。传送门单独用 SpriteMask 裁到门内孔，保留石门框。

通关逻辑确认获胜之后调用 `GateOfDeadPresentation.PlaySealingSequence()`。流程为推进 → 命中 → Boss 淡出 → 转向门 → 封印 → 保持最终静帧。重复调用会被忽略；`onSealed` 只在首次封印完成时触发。重开调用 `ResetPresentation()`。

## 亮度

当前场景不放入暮夜遮罩，底图和发光均使用原亮度。暮夜方案的源文件仍保存在工程外 `output/animation-pilot/gate-of-the-dead-v1/environment/lighting/`，可留待以后单独调整。

实际 Unity 运行检查、截图与结果见工程旁 `output/unity-gate-of-the-dead/`。
