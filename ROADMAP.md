# Picomeow —— 后续开发路线图（MVP 之后）

> 目标市场：北美（英语优先，后续 es/pt/fr）。
> 当前状态：**MVP 垂直切片已完成并推送 `main`**，可在 Unity 2021.3+ 打开 `Assets/Scenes/Main.unity` 直接 Play。

---

## 0. 如何运行当前 MVP
1. 用 Unity 2021.3 LTS+ 打开仓库根目录（`Picomeow/`）。
2. 打开 `Assets/Scenes/Main.unity`，点击 Play。
3. 无需任何预制体：所有 UI 由 `GameRoot` 在运行时通过代码构建（见 `Assets/Scripts/Core/`）。
4. 配置数据：`Assets/StreamingAssets/Config/config.json`（10 张图 / 90 关，全部为**逻辑可解的唯一解**数织）。
5. 美术：`Assets/Resources/Art/`（9 张 cozy cat 风格占位素材）。

核心循环已可玩：数织闯关（填/叉/消除、错误计数、提示道具）→ 体力/金币/星星经济 → 猫咪 Momo 对话驱动公寓装修 → 新手引导。

---

## 0.5 本次迭代已完成（原型驱动 UI 重建 + 品种命名 + 其余内容）

> 严格对照 `界面原型截图.docx` / `lv1-1 界面交互` 33 张视觉稿重建全部界面，并把猫咪按**真实品种**英文命名（北美友好）。

- [x] **UI 全面对齐交互原型**：加载页 / 主 HUB / 关卡选择 / 关卡详情 / 数织对局 / 结算 / 对话装修 / 图鉴 / 档案 / 任务 / 设置，全部按视觉稿布局与配色（奶油底、墨褐字、橙黄 CTA、青绿按钮、红缎带、黑圆 X）重建。
- [x] **猫咪按品种命名**：`CatData` 12 只猫（Calico / Bombay / Siamese / Persian / Ragdoll / Maine Coon / British Shorthair / Scottish Fold / Russian Blue / Bengal / Sphynx / Norwegian Forest），主角猫默认 Calico；图鉴、档案、新猫入住、装修对话均使用品种名。
- [x] **新猫入住两段式**：原型 06 的「揭晓 → 欢迎」流程（Reveal 按钮 → 展示品种猫 → Welcome!）。
- [x] **其余原型内容补齐**（此前为 coming-soon 占位，现已接真实界面）：
  - 排行榜（Ranking）：Top10 榜单，玩家行高亮。
  - 社交（Friends）：好友列表 + 送体力/拜访 + Facebook/Google 连接。
  - 挑战（Challenge）：每日挑战卡 + 倒计时 + 开始。
  - 个性装扮（Customize）：猫咪/棋子/头像框/背景 四标签 + 预览 + 九宫格（已装备✓/锁定锁），锁定项可购买弹窗。
  - 每日签到（Calendar）：月历网格 + 连续登录宝箱进度 + 领取。
  - 登录 / 保存进度（Login）：Facebook / Google 登录弹窗。
  - 月度订阅（$2.99，原型 13）/ 去广告（$4.99 终身，原型 12）。
  - 看广告得猫金币 / 得体力弹窗（原型 23 / 28）。
  - 月度拼图标签、语言选择、反馈弹窗。
- [x] 新增 `Economy.AddEnergy`；新增 `UIHelpers.BuildModal` 统一弹窗骨架；清理废弃 `DialogueSystem.cs`（由 `DialoguePlayer.cs` 替代）。

---

## 1. 内容扩展（P1，建议 2–3 周）
- [ ] 关卡：把 `Tools/gen_config.py` 的 `CHAPTER_SIZES` 扩到 15 章（尺寸 5–13），目标 300+ 关；用脚本重新生成并热更 `config.json`。
- [ ] 美术切图：把占位 PNG 替换为**可切片的标准切图**（九宫格/多分辨率 @1x@2x@3x），建立 `SpriteAtlas`。
- [ ] 猫咪图鉴：每关揭示一只猫，建立 `CatData`（名称/品种/稀有度/收集进度），主界面增加图鉴入口。
- [ ] 家具与房间：扩到 3–5 个主题房间（客厅/卧室/厨房/书房/花园），每个房间 6–10 件家具，`DecorationDialogue` 增加多轮分支。
- [ ] 验收：章节选择页、图鉴页可进入；同一尺寸彩色像素猫图不重复。

## 2. 商业化（P2，建议 3–4 周）
- [ ] 广告（IAA，北美主流）：接入 LevelPlay / AdMob，覆盖「复活」「额外体力」「离线收益」3 类激励视频 + 插屏（关卡间）。
- [ ] 内购（IAP）：去广告、体力包、星星包、家具礼包；接入 Unity IAP + 各平台支付。
- [ ] 商店页：家具/体力/去广告购买，与 `Economy` 打通。
- [ ] 验收：广告填充率/ecpm 可埋点；IAP 沙盒可购买；无广告开关生效。

## 3. 留存与社交（P3，建议 3–4 周）
- [ ] 云存档：Firebase / Play Games / Game Center，跨设备同步 `SaveData`。
- [ ] 排行榜：周/总榜（参考「挖到美国去」排行榜修复经验，避免重复账户展示）。
- [ ] 每日任务 / 成就 / 连续登录奖励。
- [ ] 好友与分享（可选）。
- [ ] 验收：换机存档不丢；排行榜不出现重复/TapTap 名泄漏（北美版需彻底去除任何开发者/平台字样）。

## 4. 装修系统深化（P4）
- [ ] 多主题房间切换 + 家具商店（金币/星星购买，参考「法宝系统」红点/激活思路）。
- [ ] 猫咪好感度：对话选择影响好感，解锁专属家具/剧情。
- [ ] 剧情分支：对话节点支持条件跳转与多结局。
- [ ] 验收：装修进度持久化；家具放置位置可定制（拖拽）。

## 5. 平台、合规与本地化（P5，贯穿）
- [ ] **合规**：北美儿童向/全年龄需满足 COPPA / GDPR / App Store 年龄分级；广告与数据收集合规弹窗。
- [ ] 多语言：抽离所有 UI 文案到 `Localization` 表（en 优先，es/pt/fr 跟进），替换当前硬编码英文字符串。
- [ ] A/B 素材：准备 A/B/C 风格包（参考「转生哥布林」风格 A/B/C 重绘经验），用 Firebase Remote Config 切换。
- [ ] 埋点：接入分析（Firebase Analytics / Unity Analytics），监控关卡通过率、广告 ARPU。
- [ ] 验收：无硬编码敏感词；年龄弹窗首次启动出现；语言切换即时生效。

## 6. 工程化与质量（P6，持续）
- [ ] 资源分包：`Addressables` 管理图片/音频/配置，减少首包体积（北美包体敏感）。
- [ ] 架构升级：当前 UI 全代码构建，后续请迁移到 **Prefab + UI Toolkit** 以便策划可视化维护；配置从 JSON 迁移到 `ScriptableObject` 或 Addressables。
- [ ] 数织生成器：保留 `Tools/gen_config.py` 作为关卡生产工具，增加「难度曲线」「尺寸分布」参数化。
- [ ] CI：GitHub Actions 自动构建 Android/iOS 包；基础自动化冒烟测试（jsdom/Playwright 思路可类比到 Unity Test Framework）。
- [ ] 验收：主流程自动化测试通过；构建流水线一键出包。

---

## 已知技术债（接手时需注意）
1. UI 由代码构建、无 Prefab，长期维护成本高 → 计划 P6 迁移。
2. 配置为单个 `config.json`，大版本更新建议改 Addressables/ScriptableObject。
3. 美术为占位生成图，需替换为正式切图与音效。
4. 仓库当前无 `.meta` 之外的额外缓存文件；首次打开 Unity 会自动生成 `Library/` 与 `.meta`，属正常。
