# 食尸鬼鸭（GhoulDuck）

启动后，靠近敌人死亡时掉落的战利品盒后会新增”吃“的选项，在原生交互菜单中用滚轮选择“吃”，再按游戏交互键食用。选项顺序为“战利品 → 吃 → 搬起”。食用需要 1.5 秒，进度由游戏原生进度条显示，可以用游戏的取消动作键中断；中断不会消耗盒子或恢复属性。默认允许直接食用未清空的盒子，剩余战利品散落在原地；每次恢复 30 点能量。

## 设置

启用 [Duckov Mod Settings](https://steamcommunity.com/sharedfiles/filedetails/?id=3789464208) 及其所需的 [Harmony 前置](https://steamcommunity.com/sharedfiles/filedetails/?id=3589088839)，在游戏设置的「模组设置」页面选择「食尸鬼鸭」。可以开启“吃掉后清除剩余战利品”，使剩余物品消失；也可以调整生命、黄色能量和水分的恢复量。数值范围为 0 至 10000，实际恢复不会超过角色属性上限。设置页支持主菜单和游戏内设置入口。

展开“可食用的敌人类型”可以选择哪些敌人的死亡盒子显示“吃”。默认只勾选拾荒者（包括暴走和雪地拾荒者），其余类型默认关闭；Boss 优先按 Boss 分类。设置修改后会更新当前关卡中已识别的盒子。敌人通过游戏预设名称和 Boss 标记识别，暂未识别的敌人不会显示“吃”。

Duckov Mod Settings 负责保存设置。旧版的 `Application.persistentDataPath/GhoulDuck.json` 只读作初始值；统一设置模组已保存的对应项目优先，旧文件不会被删除或覆盖。Duckov Mod Settings 未启用时仍可按默认值或旧版配置食用，但无法在页面编辑设置。

## 本地构建

安装 Windows 版 Visual Studio Build Tools、游戏和 Harmony 2.4.1 后，执行：

```powershell
.\build.ps1 -DuckovPath '游戏安装目录' -HarmonyPath '0Harmony.dll 的完整路径'
```

也可以设置环境变量 `DUCKOV_PATH` 和 `HARMONY_PATH`；如果游戏通过 Steam 安装，省略 `-HarmonyPath` 时脚本会尝试查找已订阅的 Harmony 前置。脚本通过 Visual Studio Installer 自动查找 C# 编译器，也可用 `-Compiler` 或 `CSC_PATH` 指定。游戏 DLL 仅用于编译，不包含在发布包中。

构建结果位于 `dist/GhoulDuck`，包含模组 DLL、封面、元数据、本项目的 MIT 许可证，以及 Harmony DLL 和其 MIT 许可证。封面和元数据源文件分别位于 `images/preview.png`、`package/info.ini`；构建时会自动复制到发布目录。手动安装时将 `dist/GhoulDuck` 文件夹放入游戏的 `Duckov_Data/Mods`。

执行 `./tests/run-tests.ps1` 可检查敌人分类；该检查不代替游戏内测试。

## 发布准备

GitHub 仓库应提交源码、构建脚本、`package`、`images/preview.png` 和测试。`.gitignore` 排除了游戏反编译参考资料、下载的参考仓库、本地工具和构建产物；这些资料不会随源码发布。`package/info.ini` 中的 `publishedFileId` 指向本项目的创意工坊作品；构建时会优先保留发布目录已有的 ID。若基于本项目发布自己的独立作品，请在上传前移除或替换该 ID，避免误指向原作品。

设置页面由 Duckov Mod Settings 提供；缺少该模组时仍可按默认或旧配置使用，但无法通过统一页面调整设置。发布时需要注明所需设置模组、其 Harmony 前置；封面是否适合公开发布由提供者确认。

## 注意事项

只识别模组加载后通过角色死亡流程创建的盒子，不包括玩家自己的死亡盒子和普通容器。开启清除剩余战利品会销毁盒内剩余物品。敌人类型映射已通过代码检查，具体敌人、选项变化和当前关卡盒子的显示仍需实机验证。

开发参考：[官方模组示例](https://github.com/xvrsl/duckov_modding)、[Duckov Mod Settings](https://github.com/SlimeNull/DuckovMods)、[Harmony](https://github.com/pardeike/Harmony)。
本项目使用 GPT-6.1 Terra vibe coding 实现。

## 许可证

本项目代码使用 [MIT 许可证](LICENSE)。随发布包附带的 Harmony 使用其独立的 [MIT 许可证](package/Harmony-LICENSE.txt)。
