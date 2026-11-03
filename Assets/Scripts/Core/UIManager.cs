using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Picomeow.Config;
using Picomeow.Core;
using Picomeow.UI;
using Picomeow.Nonogram;
using Picomeow.Apartment;

namespace Picomeow.Core
{
    // 总控：构建并管理所有界面（严格参照交互原型 lv1-1 视觉稿布局），串联玩法 / 经济 / 装修 / 引导
    public class UIManager : MonoBehaviour
    {
        Transform screenRoot, popupRoot, overlayRoot;
        GameObject loadingScreen, mainScreen, levelSelectScreen, puzzleScreen, catsScreen, dialogueScreen;
        Transform puzzleHolder, catsGridRoot;
        Economy economy;
        DialoguePlayer dialoguePlayer;
        LevelData currentLevel;
        int revealedCatId = 0; // 本次装修后待入住的新猫 id（0=无）
        HashSet<int> claimedDays = new HashSet<int>(); // 已签到日期（会话内）

        public void Build()
        {
            economy = Economy.Instance;
            SaveSystem.Load(); // 提前初始化存档，避免 UI 构建时数据为空
            screenRoot = UIHelpers.Fill(transform, "ScreenRoot", new Color(0, 0, 0, 0));
            popupRoot = UIHelpers.Fill(transform, "PopupRoot", new Color(0, 0, 0, 0));
            overlayRoot = UIHelpers.Fill(transform, "OverlayRoot", new Color(0, 0, 0, 0));

            BuildLoading();
            BuildMain();
            BuildLevelSelect();
            BuildPuzzleScreen();
            BuildCats();
            dialogueScreen = UIHelpers.Fill(overlayRoot, "Dialogue", new Color(0, 0, 0, 0)).gameObject;
            dialoguePlayer = dialogueScreen.AddComponent<DialoguePlayer>();

            loadingScreen.SetActive(true);
            mainScreen.SetActive(false);
            levelSelectScreen.SetActive(false);
            puzzleScreen.SetActive(false);
            catsScreen.SetActive(false);
            dialogueScreen.SetActive(false);

            ConfigLoader.Load(this, OnConfigLoaded);
        }

        // ---------------- 加载界面（视觉稿 01） ----------------
        void BuildLoading()
        {
            loadingScreen = UIHelpers.Fill(screenRoot, "Loading", UIHelpers.Cream).gameObject;
            var quote = UIHelpers.EL(loadingScreen.transform, "Quote", 120, 1320, 840, 260);
            UIHelpers.AddLabel(quote, "Play 30 minutes a day,\nyou'll sleep better and deeper.", 46, UIHelpers.Ink);
            // 进度条
            var bar = UIHelpers.EL(loadingScreen.transform, "Bar", 120, 1020, 840, 70);
            UIHelpers.AddImage(bar, null, new Color(0.85f, 0.82f, 0.78f));
            var fill = UIHelpers.EL(bar, "Fill", 4, 4, 700, 62);
            UIHelpers.AddImage(fill, null, UIHelpers.Amber);
            var cat = UIHelpers.EL(bar, "CatFace", 700, 8, 56, 56);
            UIHelpers.AddImage(cat, UIHelpers.LoadSprite("momo_cat"), Color.white);
            // 游戏名占位
            var name = UIHelpers.EL(loadingScreen.transform, "Name", 240, 760, 600, 160);
            UIHelpers.AddLabel(name, "Picomeow", 96, UIHelpers.AmberDk);
        }

        // ---------------- 主界面 / HUB（视觉稿 12） ----------------
        void BuildMain()
        {
            mainScreen = UIHelpers.Fill(screenRoot, "Main", Color.white).gameObject;
            var root = mainScreen.transform;

            // 顶部 HUD：头像 / 猫金币 / 小鱼干 / 设置
            var avatar = UIHelpers.EL(root, "Avatar", 40, 1760, 130, 130);
            var ab = UIHelpers.AddButton(avatar, UIHelpers.Sage, "", 1);
            ab.onClick.AddListener(() => OpenProfile());
            UIHelpers.AddImage(UIHelpers.EL(avatar, "AImg", 10, 10, 110, 110), UIHelpers.LoadSprite("momo_cat"), Color.white).preserveAspect = true;

            CurrencyPill(root, 190, 1765, 360, 120, UIHelpers.Amber, () => economy != null ? economy.Coins : 0, "Cat Coins", () => OpenAdForCoins());
            CurrencyPill(root, 570, 1765, 360, 120, UIHelpers.Teal, () => economy != null ? economy.Fish : 0, "Fish", () => OpenAdForEnergy());

            var settings = CircleBtn(root, 980, 1765, 120, null, "Settings", () => OpenSettings());

            // 任务（左上，绿勾角标）
            var tasks = CircleBtn(root, 70, 1600, 120, UIHelpers.LoadSprite("ui_circle"), "Tasks", () => OpenTasks());
            UIHelpers.Badge(tasks, 92, 96, 44, "✓");
            // 社交/好友（右上，红点角标）
            var friends = CircleBtn(root, 980, 1600, 120, UIHelpers.LoadSprite("ui_circle"), "Friends", () => OpenSocial());
            UIHelpers.Badge(friends, 92, 96, 44, "!");
            // 排行榜（居中空带）
            var rank = CircleBtn(root, 480, 1610, 110, UIHelpers.LoadSprite("ui_circle"), "Ranking", () => OpenRanking());

            // 底部导航栏
            var nav = UIHelpers.EL(root, "NavBar", 0, 20, 1080, 280);
            UIHelpers.AddImage(nav, null, new Color(0.98f, 0.95f, 0.90f));
            var dBtn = CircleBtn(nav, 70, 80, 130, UIHelpers.LoadSprite("ui_circle"), "Decorate", () => OpenDialogue(0));
            UIHelpers.Badge(dBtn, 100, 100, 44, "!");
            var cBtn = CircleBtn(nav, 220, 80, 130, UIHelpers.LoadSprite("ui_circle"), "Cats", () => ShowScreen("Cats"));
            UIHelpers.Badge(cBtn, 100, 100, 44, "!");
            // 中央大关卡按钮
            var play = UIHelpers.EL(nav, "Play", 410, 20, 260, 260);
            UIHelpers.AddButton(play, UIHelpers.Teal, "", 1).onClick.AddListener(() => ShowScreen("LevelSelect"));
            var pImg = UIHelpers.AddImage(UIHelpers.EL(play, "PImg", 30, 120, 200, 120), UIHelpers.LoadSprite("momo_cat"), Color.white);
            pImg.preserveAspect = true;
            var pLbl = UIHelpers.EL(play, "PLbl", 0, 40, 260, 70);
            UIHelpers.AddLabel(pLbl, "Lv " + SaveSystem.data.unlockedLevel, 44, UIHelpers.Paper);
            var pBtn = CircleBtn(nav, 720, 80, 130, UIHelpers.LoadSprite("ui_circle"), "Puzzle", () => ShowScreen("LevelSelect"));
            var chBtn = CircleBtn(nav, 900, 80, 130, UIHelpers.LoadSprite("ui_circle"), "Challenge", () => OpenChallenge());
        }

        // 货币胶囊：暗色胶囊 + 圆形图标 + 数值 + "+" 按钮
        void CurrencyPill(Transform parent, float x, float y, float w, float h, Color iconCol, System.Func<int> getter, string label, UnityEngine.Events.UnityAction onPlus)
        {
            var pill = UIHelpers.EL(parent, label + "Pill", x, y, w, h);
            UIHelpers.AddImage(pill, null, new Color(0.25f, 0.23f, 0.21f));
            var icon = UIHelpers.EL(pill, "Icon", 16, 20, h - 40, h - 40);
            UIHelpers.AddImage(icon, UIHelpers.LoadSprite("ui_circle"), iconCol).preserveAspect = true;
            var val = UIHelpers.EL(pill, "Val", 80, 0, w - 200, h);
            UIHelpers.AddLabel(val, label + " " + getter(), 30, UIHelpers.Paper, TextAnchor.MiddleLeft);
            var plus = UIHelpers.EL(pill, "Plus", w - 110, 20, h - 40, h - 40);
            var pb = UIHelpers.AddButton(plus, Color.white, "+", 44);
            pb.onClick.AddListener(onPlus);
        }

        // 圆形图标按钮（带下方文字标签）
        Transform CircleBtn(Transform parent, float x, float y, float d, Sprite spr, string label, UnityEngine.Events.UnityAction onClick)
        {
            var rt = UIHelpers.EL(parent, label + "Btn", x, y, d, d + 44);
            var circle = UIHelpers.EL(rt, "Circle", 0, 44, d, d);
            Color baseCol = (spr == null) ? UIHelpers.Sage : Color.white;
            var img = UIHelpers.AddImage(circle, spr, baseCol);
            img.preserveAspect = (spr != null);
            var btn = circle.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors; cb.normalColor = baseCol; cb.highlightedColor = UIHelpers.Sage; cb.pressedColor = UIHelpers.TealDk;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(onClick);
            if (!string.IsNullOrEmpty(label))
            {
                var lt = UIHelpers.EL(rt, "Lbl", 0, 0, d, 44);
                UIHelpers.AddLabel(lt, label, 26, UIHelpers.Ink);
            }
            return rt;
        }

        // ---------------- 拼图关卡选择（视觉稿 22） ----------------
        void BuildLevelSelect()
        {
            levelSelectScreen = UIHelpers.Fill(screenRoot, "LevelSelect", UIHelpers.Cream).gameObject;
            var root = levelSelectScreen.transform;
            UIHelpers.RibbonBanner(root, 290, 1700, 500, 130, "Puzzle", UIHelpers.Red, UIHelpers.Paper);
            UIHelpers.CloseButton(root, 1000, 1740, 90, () => ShowScreen("Main"));
            // 标签
            var mainTab = UIHelpers.EL(root, "TabMain", 40, 1560, 480, 90);
            UIHelpers.AddButton(mainTab, UIHelpers.Paper, "Main Line", 38).onClick.AddListener(() => BuildLevelGrid(true));
            var monTab = UIHelpers.EL(root, "TabMon", 560, 1560, 480, 90);
            UIHelpers.AddButton(monTab, UIHelpers.Amber, "Monthly", 38).onClick.AddListener(() => OpenMonthlyPuzzles());

            var scroll = UIHelpers.EL(root, "Scroll", 40, 220, 1000, 1280);
            var sImg = scroll.gameObject.AddComponent<Image>(); sImg.color = new Color(1, 1, 1, 0.001f);
            var mask = scroll.gameObject.AddComponent<Mask>(); mask.showMaskGraphic = false;
            var sr = scroll.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.movementType = ScrollRect.MovementType.Clamped;
            var content = new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(scroll, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
            var gl = content.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(300, 360); gl.spacing = new Vector2(20, 20); gl.padding = new RectOffset(20, 20, 20, 20);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3;
            var csf = content.gameObject.AddComponent<ContentSizeFitter>(); csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = crt; sr.viewport = scroll;
            catsGridRoot = content; // 复用为关卡网格容器（本屏销毁重建）
        }

        void BuildLevelGrid(bool mainLine)
        {
            Transform content = levelSelectScreen.transform.Find("Scroll/Content");
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--) Object.Destroy(content.GetChild(i).gameObject);
            if (GameConfig.Data == null) return;
            int unlocked = SaveSystem.data.unlockedLevel;
            int shown = 0;
            foreach (var lvl in GameConfig.Data.levels)
            {
                if (shown >= 9) break; // 3x3 展示首章九关
                int id = lvl.id;
                bool open = id <= unlocked;
                var cell = UIHelpers.EL(content, "Lv_" + id, 0, 0, 300, 360);
                UIHelpers.AddImage(cell, null, UIHelpers.Paper);
                // 缩略图
                var thumb = UIHelpers.EL(cell, "Thumb", 30, 150, 240, 180);
                Sprite pic = open ? UIHelpers.LoadSprite("momo_cat") : null;
                UIHelpers.AddImage(thumb, pic, UIHelpers.Cream).preserveAspect = true;
                // 关卡号
                UIHelpers.AddLabel(UIHelpers.EL(cell, "No", 0, 100, 300, 50), "Level " + id, 34, UIHelpers.Ink);
                if (open)
                {
                    UIHelpers.Badge(cell, 250, 330, 44, "✓");
                    var btn = cell.gameObject.AddComponent<Button>();
                    var img = cell.GetComponent<Image>(); btn.targetGraphic = img;
                    ColorBlock cb = btn.colors; cb.normalColor = Color.white; cb.highlightedColor = UIHelpers.Cream; cb.pressedColor = UIHelpers.Sage;
                    btn.colors = cb;
                    btn.onClick.AddListener(() => OpenLevelDetails(GameConfig.GetLevel(id)));
                }
                else
                {
                    UIHelpers.Badge(cell, 250, 330, 44, "L");
                }
                shown++;
            }
        }

        // ---------------- 关卡详情（视觉稿 17） ----------------
        void OpenLevelDetails(LevelData lvl)
        {
            ClearPopup();
            var dim = UIHelpers.Fill(popupRoot, "Dim", UIHelpers.Panel);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => ClearPopup());
            var modal = UIHelpers.EL(popupRoot, "LvDetail", 140, 360, 800, 1180);
            UIHelpers.AddImage(modal, null, UIHelpers.Paper);
            UIHelpers.RibbonBanner(modal, 130, 1010, 540, 120, "Level Details", UIHelpers.Amber, UIHelpers.Paper);
            UIHelpers.CloseButton(modal, 700, 1050, 80, () => ClearPopup());

            var pic = GameConfig.GetPicture(lvl.pictureId);
            string chapter = "Chapter " + (pic != null ? pic.chapter : 1);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "CL", 0, 900, 800, 60), chapter + "  Level " + lvl.id, 40, UIHelpers.Ink);
            var prev = UIHelpers.EL(modal, "Prev", 230, 520, 340, 340);
            UIHelpers.AddImage(prev, UIHelpers.LoadSprite("momo_cat"), UIHelpers.Cream).preserveAspect = true;
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Size", 0, 360, 800, 50), "Grid Size: " + (pic != null ? pic.size : 5) + " x " + (pic != null ? pic.size : 5), 36, UIHelpers.Ink);
            string diff = (pic != null && pic.chapter >= 2) ? "Normal" : "Easy";
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Diff", 0, 280, 800, 50), "Difficulty: " + diff, 36, UIHelpers.Ink);

            var start = UIHelpers.EL(popupRoot, "StartBtn", 140, 120, 800, 200);
            var sb = UIHelpers.AddButton(start, UIHelpers.Amber, "Start", 60);
            sb.onClick.AddListener(() => { ClearPopup(); StartLevel(lvl); });
            // 体力花费标记
            UIHelpers.AddLabel(UIHelpers.EL(start, "Cost", 560, 0, 240, 200), "Energy " + lvl.energyCost, 36, UIHelpers.Paper);
        }

        // ---------------- 拼图界面容器 ----------------
        void BuildPuzzleScreen()
        {
            puzzleScreen = UIHelpers.Fill(screenRoot, "Puzzle", UIHelpers.Cream).gameObject;
            puzzleHolder = UIHelpers.Fill(puzzleScreen.transform, "Holder", new Color(0, 0, 0, 0));
        }

        void StartLevel(LevelData level)
        {
            if (economy == null) return;
            if (economy.Energy < level.energyCost) { Toast("Not enough energy!"); return; }
            economy.SpendEnergy(level.energyCost);
            currentLevel = level;
            ClearChildren(puzzleHolder);
            ShowScreen("Puzzle");
            var pic = GameConfig.GetPicture(level.pictureId);
            if (pic != null) NonogramBoard.Build(puzzleHolder, pic, level, OnPuzzleDone);
        }

        void OnPuzzleDone(bool win)
        {
            if (win) ShowResult(currentLevel);
            else ShowScreen("LevelSelect");
        }

        // ---------------- 结果结算（视觉稿 21） ----------------
        void ShowResult(LevelData level)
        {
            int coins = level.coinReward;
            int frags = 1;
            economy.AddCoins(coins);
            economy.AddFragments(frags);
            if (level.id + 1 > SaveSystem.data.unlockedLevel)
            {
                SaveSystem.data.unlockedLevel = level.id + 1;
                SaveSystem.Save();
            }
            ClearPopup();
            var dim = UIHelpers.Fill(popupRoot, "Dim", UIHelpers.Panel);
            var modal = UIHelpers.EL(popupRoot, "Result", 120, 300, 840, 1320);
            UIHelpers.AddImage(modal, null, UIHelpers.Paper);
            UIHelpers.RibbonBanner(modal, 170, 1140, 500, 110, "Pattern Revealed", UIHelpers.Red, UIHelpers.Paper);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Beat", 0, 980, 840, 80), "Beat 88.7% of players!", 40, UIHelpers.Ink);
            var tile = UIHelpers.EL(modal, "Tile", 290, 560, 290, 290);
            UIHelpers.AddImage(tile, UIHelpers.LoadSprite("momo_cat"), UIHelpers.Cream).preserveAspect = true;
            UIHelpers.RibbonBanner(modal, 200, 470, 440, 80, "Level Clear Rewards", UIHelpers.Amber, UIHelpers.Paper);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "RW", 0, 360, 840, 90),
                "+" + coins + " Cat Coins     +" + frags + " Fragment", 38, UIHelpers.Teal);
            // 3x / Claim
            var x3 = UIHelpers.EL(popupRoot, "X3", 160, 120, 360, 180);
            UIHelpers.AddButton(x3, UIHelpers.Teal, "3x Reward", 40).onClick.AddListener(() =>
            {
                economy.AddCoins(coins * 2); economy.AddFragments(frags * 2); // 额外 2 倍（模拟广告）
                Toast("Reward tripled!");
            });
            var claim = UIHelpers.EL(popupRoot, "Claim", 560, 120, 360, 180);
            UIHelpers.AddButton(claim, UIHelpers.Amber, "Claim", 44).onClick.AddListener(() =>
            {
                ClearPopup();
                // 每图最后一关触发装修对话；完成房间后入住新猫
                if (level.decorationTrigger && !DecorationManager.IsDecorated(DecorationManager.CurrentRoom()))
                {
                    revealedCatId = (DecorationManager.CurrentRoom() == 1) ? 2 : 3; // 入住对应品种猫
                    OpenDialogue(0);
                }
                else ShowScreen("Main");
            });
        }

        // ---------------- 对话 / 装修（Momo's Room） ----------------
        void OpenDialogue(int roomId)
        {
            int room = roomId > 0 ? roomId : DecorationManager.CurrentRoom();
            var d = GameConfig.GetDialogueByRoom(room);
            if (d == null) { Toast("No dialogue for room " + room); return; }
            ClearChildren(dialogueScreen.transform);
            dialogueScreen.SetActive(true);
            dialoguePlayer.Play(dialogueScreen.transform, d,
                (fid) => { },
                () => OnDialogueFinish(room));
        }

        void OnDialogueFinish(int room)
        {
            DecorationManager.Decorate(room);
            dialogueScreen.SetActive(false);
            // 入住新猫（图鉴 +1）
            if (revealedCatId > 0 && GameConfig.Data != null)
            {
                foreach (var c in GameConfig.Data.cats)
                    if (c.id == revealedCatId && !c.unlocked) { c.unlocked = true; SaveSystem.data.collectedCats++; }
                SaveSystem.Save();
                int rid = revealedCatId; revealedCatId = 0;
                OpenNewCat(rid);
                return;
            }
            ShowScreen("Main");
            Toast("Room decorated! +1 star");
        }

        // ---------------- 新猫入住（视觉稿 06，两段式：揭晓 → 欢迎） ----------------
        void OpenNewCat(int catId)
        {
            if (GameConfig.Data == null || GameConfig.Data.cats == null) { ShowScreen("Main"); return; }
            CatData cat = null; foreach (var c in GameConfig.Data.cats) if (c.id == catId) cat = c;
            if (cat == null) { ShowScreen("Main"); return; }
            ClearPopup();
            var bg = UIHelpers.Fill(popupRoot, "NewCatBG", new Color(0.96f, 0.80f, 0.30f));
            // 爪印底纹（简化：若干深色圆点）
            for (int i = 0; i < 26; i++)
            {
                float px = (i * 137) % 1000 + 40; float py = (i * 311) % 1700 + 60;
                UIHelpers.AddImage(UIHelpers.EL(bg, "paw" + i, px, py, 70, 70), UIHelpers.LoadSprite("ui_circle"), new Color(0.85f, 0.55f, 0.18f)).preserveAspect = true;
            }
            UIHelpers.RibbonBanner(bg, 150, 1560, 780, 130, "NEW CAT RESIDENT", UIHelpers.Red, UIHelpers.Paper);
            var body = UIHelpers.EL(bg, "Body", 0, 120, 1080, 1560);
            BuildNewCatReveal(body, cat);
        }

        void BuildNewCatReveal(Transform body, CatData cat)
        {
            ClearChildren(body);
            var q = UIHelpers.EL(body, "Q", 340, 480, 400, 400);
            UIHelpers.AddImage(q, UIHelpers.LoadSprite("ui_circle"), new Color(0.85f, 0.55f, 0.18f)).preserveAspect = true;
            UIHelpers.AddLabel(UIHelpers.EL(body, "Hint", 0, 360, 1080, 90), "Which cat will come?", 56, UIHelpers.Paper);
            var rev = UIHelpers.EL(body, "Reveal", 340, 150, 400, 160);
            UIHelpers.AddButton(rev, UIHelpers.Teal, "Reveal", 50).onClick.AddListener(() => BuildNewCatShown(body, cat));
        }

        void BuildNewCatShown(Transform body, CatData cat)
        {
            ClearChildren(body);
            var hero = UIHelpers.EL(body, "Hero", 340, 480, 400, 400);
            UIHelpers.AddImage(hero, CatSprite(cat.id), Color.white).preserveAspect = true;
            UIHelpers.AddLabel(UIHelpers.EL(body, "Name", 0, 360, 1080, 90), cat.name + " (" + cat.breed + ")", 56, UIHelpers.Paper);
            UIHelpers.AddLabel(UIHelpers.EL(body, "Desc", 140, 200, 800, 150), cat.desc, 32, UIHelpers.Paper);
            var wel = UIHelpers.EL(body, "Welcome", 290, 40, 500, 160);
            UIHelpers.AddButton(wel, UIHelpers.Teal, "Welcome!", 50).onClick.AddListener(() => { ClearPopup(); ShowScreen("Main"); });
        }

        // ---------------- 猫咪图鉴（视觉稿 15） ----------------
        void BuildCats()
        {
            catsScreen = UIHelpers.Fill(screenRoot, "Cats", new Color(0.96f, 0.55f, 0.20f)).gameObject;
            var root = catsScreen.transform;
            UIHelpers.RibbonBanner(root, 290, 1700, 500, 130, "Album", UIHelpers.Red, UIHelpers.Paper);
            UIHelpers.CloseButton(root, 1000, 1740, 90, () => ShowScreen("Main"));
            var panel = UIHelpers.EL(root, "Book", 60, 240, 960, 1360);
            UIHelpers.AddImage(panel, null, UIHelpers.Cream);
            // 螺旋环
            for (int i = 0; i < 5; i++)
                UIHelpers.AddImage(UIHelpers.EL(panel, "ring" + i, 40 + i * 10, 1320, 60, 30), UIHelpers.LoadSprite("ui_circle"), new Color(0.55f, 0.40f, 0.25f)).preserveAspect = true;
            var grid = UIHelpers.EL(panel, "Grid", 60, 80, 840, 1180);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(270, 290); gl.spacing = new Vector2(10, 10); gl.padding = new RectOffset(10, 10, 10, 10);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3;
            catsGridRoot = grid;
            RefreshCats();
        }

        void RefreshCats()
        {
            for (int i = catsGridRoot.childCount - 1; i >= 0; i--) Object.Destroy(catsGridRoot.GetChild(i).gameObject);
            if (GameConfig.Data == null || GameConfig.Data.cats == null) return;
            int collected = 0;
            foreach (var c in GameConfig.Data.cats)
            {
                bool own = c.unlocked;
                if (own) collected++;
                var card = UIHelpers.EL(catsGridRoot, "Cat_" + c.id, 0, 0, 270, 290);
                UIHelpers.AddImage(card, null, UIHelpers.Paper);
                if (own)
                {
                    var img = UIHelpers.AddImage(UIHelpers.EL(card, "Img", 35, 70, 200, 160), CatSprite(c.id), Color.white);
                    img.preserveAspect = true;
                    UIHelpers.AddLabel(UIHelpers.EL(card, "Nm", 0, 20, 270, 44), c.name, 30, UIHelpers.Ink);
                    var btn = card.gameObject.AddComponent<Button>();
                    var ci = card.GetComponent<Image>(); btn.targetGraphic = ci;
                    ColorBlock cb = btn.colors; cb.normalColor = Color.white; cb.highlightedColor = UIHelpers.Cream; cb.pressedColor = UIHelpers.Sage; btn.colors = cb;
                    int cid = c.id; btn.onClick.AddListener(() => OpenCatProfile(cid));
                }
                else
                {
                    UIHelpers.AddImage(UIHelpers.EL(card, "Q", 35, 70, 200, 160), null, new Color(0.8f, 0.78f, 0.75f));
                    UIHelpers.AddLabel(UIHelpers.EL(card, "Qm", 0, 20, 270, 44), "???", 30, UIHelpers.Ink);
                }
            }
            UIHelpers.AddLabel(UIHelpers.EL(catsScreen.transform, "Count", 420, 200, 240, 50), collected + "/" + GameConfig.Data.cats.Length, 40, UIHelpers.Paper);
        }

        Sprite CatSprite(int id)
        {
            if (id == 1) return UIHelpers.LoadSprite("momo_cat");
            if (id == 2) return UIHelpers.LoadSprite("cat_bombay");
            return UIHelpers.LoadSprite("momo_cat");
        }

        // ---------------- 猫咪档案（视觉稿 16） ----------------
        void OpenCatProfile(int catId)
        {
            if (GameConfig.Data == null) return;
            CatData cat = null; foreach (var c in GameConfig.Data.cats) if (c.id == catId) cat = c;
            if (cat == null) return;
            ClearPopup();
            var dim = UIHelpers.Fill(popupRoot, "Dim", UIHelpers.Panel);
            var page = UIHelpers.EL(popupRoot, "Profile", 120, 300, 840, 1320);
            UIHelpers.AddImage(page, null, UIHelpers.Cream);
            for (int i = 0; i < 4; i++)
                UIHelpers.AddImage(UIHelpers.EL(page, "sp" + i, 40 + i * 8, 1280, 50, 26), UIHelpers.LoadSprite("ui_circle"), new Color(0.7f, 0.5f, 0.3f)).preserveAspect = true;
            var photo = UIHelpers.EL(page, "Photo", 170, 820, 500, 420);
            UIHelpers.AddImage(photo, CatSprite(cat.id), Color.white).preserveAspect = true;
            UIHelpers.AddLabel(UIHelpers.EL(page, "NameTag", 170, 720, 360, 70), cat.name, 44, UIHelpers.Ink);
            UIHelpers.AddLabel(UIHelpers.EL(page, "No", 560, 720, 200, 70), "No." + cat.id, 36, UIHelpers.Ink);
            UIHelpers.AddLabel(UIHelpers.EL(page, "Desc", 120, 560, 720, 140), cat.desc, 30, UIHelpers.Ink, TextAnchor.UpperLeft);
            UIHelpers.AddLabel(UIHelpers.EL(page, "RelL", 120, 440, 720, 50), "Relationship: " + cat.rel, 32, UIHelpers.Teal, TextAnchor.MiddleLeft);
            UIHelpers.AddLabel(UIHelpers.EL(page, "SecL", 120, 330, 720, 90), "Secret: " + cat.secret, 28, UIHelpers.Ink, TextAnchor.UpperLeft);
            UIHelpers.CloseButton(page, 720, 1220, 80, () => ClearPopup());
        }

        // ---------------- 任务弹窗（视觉稿 02/03） ----------------
        void OpenTasks()
        {
            ClearPopup();
            var dim = UIHelpers.Fill(popupRoot, "Dim", UIHelpers.Panel);
            var modal = UIHelpers.EL(popupRoot, "Tasks", 140, 320, 800, 1240);
            UIHelpers.AddImage(modal, null, UIHelpers.Paper);
            UIHelpers.RibbonBanner(modal, 130, 980, 540, 110, "Task", UIHelpers.Amber, UIHelpers.Paper);
            UIHelpers.CloseButton(modal, 700, 1010, 70, () => ClearPopup());
            var cat = UIHelpers.EL(modal, "Cat", 290, 560, 300, 300);
            UIHelpers.AddImage(cat, UIHelpers.LoadSprite("momo_cat"), Color.white).preserveAspect = true;
            UIHelpers.AddLabel(UIHelpers.EL(modal, "TaskDesc", 80, 360, 640, 160),
                "Clear Level 9 of Chapter 1 to earn a Cat Bed!", 34, UIHelpers.Ink);
            var go = UIHelpers.EL(popupRoot, "GoTask", 140, 120, 800, 180);
            UIHelpers.AddButton(go, UIHelpers.Amber, "Go Challenge", 48).onClick.AddListener(() => { ClearPopup(); ShowScreen("LevelSelect"); });
        }

        // ---------------- 设置（视觉稿 08） ----------------
        void OpenSettings()
        {
            ClearPopup();
            var dim = UIHelpers.Fill(popupRoot, "Dim", UIHelpers.Panel);
            var modal = UIHelpers.EL(popupRoot, "Settings", 140, 200, 800, 1460);
            UIHelpers.AddImage(modal, null, UIHelpers.Paper);
            UIHelpers.RibbonBanner(modal, 130, 1240, 540, 120, "Settings", UIHelpers.Amber, UIHelpers.Paper);
            UIHelpers.CloseButton(modal, 700, 1270, 70, () => ClearPopup());
            ToggleRow(modal, "Music", 1060, true);
            ToggleRow(modal, "Sound", 940, true);
            ToggleRow(modal, "Vibration", 820, true);
            RowButton(modal, "Save Progress", 680, () => OpenLogin());
            RowButton(modal, "Language", 560, () => OpenLanguage());
            RowButton(modal, "Customize", 440, () => OpenPersonalization());
            RowButton(modal, "Feedback", 320, () => OpenFeedback());
            RowButton(modal, "Subscribe", 200, () => OpenMonthlySub());
            var noads = UIHelpers.EL(popupRoot, "NoAds", 200, 120, 680, 150);
            UIHelpers.AddButton(noads, UIHelpers.Amber, "Remove Ads", 44).onClick.AddListener(() => OpenNoAds());
        }

        void ToggleRow(Transform parent, string label, float y, bool on)
        {
            var row = UIHelpers.EL(parent, label + "Row", 60, y, 680, 110);
            UIHelpers.AddImage(row, null, UIHelpers.Peach);
            UIHelpers.AddLabel(UIHelpers.EL(row, "L", 20, 0, 400, 110), label, 38, UIHelpers.Ink, TextAnchor.MiddleLeft);
            var tg = UIHelpers.EL(row, "Toggle", 540, 25, 110, 60);
            UIHelpers.AddImage(tg, UIHelpers.LoadSprite("ui_circle"), on ? UIHelpers.GreenOn : new Color(0.7f, 0.68f, 0.66f)).preserveAspect = true;
        }

        void RowButton(Transform parent, string label, float y, UnityEngine.Events.UnityAction onClick)
        {
            var row = UIHelpers.EL(parent, label + "Row", 60, y, 680, 110);
            UIHelpers.AddImage(row, null, UIHelpers.Peach);
            UIHelpers.AddLabel(UIHelpers.EL(row, "L", 20, 0, 560, 110), label, 36, UIHelpers.Ink, TextAnchor.MiddleLeft);
            var btn = row.gameObject.AddComponent<Button>();
            var img = row.GetComponent<Image>(); btn.targetGraphic = img;
            ColorBlock cb = btn.colors; cb.normalColor = UIHelpers.Peach; cb.highlightedColor = UIHelpers.Cream; cb.pressedColor = UIHelpers.Sage; btn.colors = cb;
            btn.onClick.AddListener(onClick);
        }

        // ---------------- 主角档案（头像点击） ----------------
        void OpenProfile()
        {
            if (GameConfig.Data == null) return;
            var h = GameConfig.HeroCat();
            if (h == null) return;
            OpenCatProfile(h.id);
        }

        // ---------------- 通用 ----------------
        void OnConfigLoaded()
        {
            SaveSystem.Load();
            if (economy != null) economy.onChanged.AddListener(RefreshHUD);
            loadingScreen.SetActive(false);
            ShowScreen("Main");
            if (!SaveSystem.data.tutorialDone) StartOnboarding();
        }

        void RefreshHUD()
        {
            // 主界面货币胶囊刷新（若存在）
            UpdatePill("Cat Coins", economy != null ? economy.Coins : 0);
            UpdatePill("Fish", economy != null ? economy.Fish : 0);
        }

        void UpdatePill(string label, int val)
        {
            var pill = mainScreen.transform.Find(label + "Pill");
            if (pill == null) return;
            var v = pill.Find("Val");
            if (v != null) v.GetComponent<Text>().text = label + " " + val;
            // 中央关卡按钮
            var pl = mainScreen.transform.Find("NavBar/Play/PLbl");
            if (pl != null) pl.GetComponent<Text>().text = "Lv " + SaveSystem.data.unlockedLevel;
        }

        void ShowScreen(string name)
        {
            mainScreen.SetActive(name == "Main");
            levelSelectScreen.SetActive(name == "LevelSelect");
            puzzleScreen.SetActive(name == "Puzzle");
            catsScreen.SetActive(name == "Cats");
            if (name == "LevelSelect") BuildLevelGrid(true);
        }

        void StartOnboarding()
        {
            if (GameConfig.Data == null || GameConfig.Data.onboarding == null) { SaveSystem.data.tutorialDone = true; SaveSystem.Save(); return; }
            SaveSystem.data.tutorialDone = true; SaveSystem.Save();
            // MVP：以一次性 Toast 序列引导（可后续接入高亮遮罩）
            StartCoroutine(OnboardRoutine());
        }

        IEnumerator OnboardRoutine()
        {
            string[] tips = {
                "Welcome to Picomeow! Tap PLAY to solve a cat puzzle.",
                "Fill cells to match the number clues and reveal a cat.",
                "Earn Cat Coins, then tap DECORATE to renovate via dialogue.",
                "Collect cats by breed in your Album. Enjoy!"
            };
            foreach (var t in tips) { Toast(t); yield return new WaitForSeconds(1.8f); }
        }

        // ================= 新增界面（其余原型内容） =================

        // ---------------- 排行榜（原型 04） ----------------
        void OpenRanking()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Ranking", UIHelpers.Red, 900, 1400, 260);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Sub", 0, 1130, 900, 60), "Top players this week", 32, UIHelpers.Ink);
            var list = UIHelpers.EL(modal, "List", 40, 200, 820, 900);
            string[] names = { "Whiskers", "Mittens", "Shadow", "Luna", "Oreo", "Snowball", "You", "Gizmo", "Peanut", "Coco" };
            int you = Mathf.Max(economy != null ? economy.Coins : 0, 4980);
            int[] scores = { 9820, 8640, 7510, 6890, 6120, 5540, you, 4320, 3990, 3210 };
            for (int i = 0; i < names.Length; i++)
            {
                float ry = 900 - i * 90;
                var row = UIHelpers.EL(list, "R" + i, 0, ry, 820, 80);
                UIHelpers.AddImage(row, null, (names[i] == "You") ? UIHelpers.Peach : UIHelpers.Cream);
                UIHelpers.AddLabel(UIHelpers.EL(row, "Rank", 10, 0, 90, 80), "#" + (i + 1), 34, (i < 3) ? UIHelpers.Red : UIHelpers.Ink, TextAnchor.MiddleLeft);
                UIHelpers.AddImage(UIHelpers.EL(row, "Av", 110, 10, 60, 60), UIHelpers.LoadSprite("momo_cat"), Color.white).preserveAspect = true;
                UIHelpers.AddLabel(UIHelpers.EL(row, "Nm", 190, 0, 360, 80), names[i], 32, UIHelpers.Ink, TextAnchor.MiddleLeft);
                UIHelpers.AddLabel(UIHelpers.EL(row, "Sc", 540, 0, 270, 80), scores[i].ToString(), 32, UIHelpers.Teal, TextAnchor.MiddleRight);
            }
        }

        // ---------------- 社交 / 好友（原型 06） ----------------
        void OpenSocial()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Friends", UIHelpers.Teal, 900, 1380, 270);
            var list = UIHelpers.EL(modal, "List", 40, 480, 820, 650);
            string[] fnames = { "Bella", "Max", "Chloe", "Leo", "Nala" };
            for (int i = 0; i < fnames.Length; i++)
            {
                float ry = 650 - i * 130;
                var row = UIHelpers.EL(list, "F" + i, 0, ry, 820, 120);
                UIHelpers.AddImage(row, null, UIHelpers.Cream);
                UIHelpers.AddImage(UIHelpers.EL(row, "Av", 20, 30, 60, 60), UIHelpers.LoadSprite((i % 2 == 0) ? "momo_cat" : "cat_bombay"), Color.white).preserveAspect = true;
                UIHelpers.AddLabel(UIHelpers.EL(row, "Nm", 100, 0, 320, 120), fnames[i], 34, UIHelpers.Ink, TextAnchor.MiddleLeft);
                var send = UIHelpers.EL(row, "Send", 460, 30, 150, 60);
                UIHelpers.AddButton(send, UIHelpers.Teal, "Send", 28).onClick.AddListener(() => Toast("Energy sent to " + fnames[i]));
                var visit = UIHelpers.EL(row, "Visit", 630, 30, 170, 60);
                UIHelpers.AddButton(visit, UIHelpers.Amber, "Visit", 28).onClick.AddListener(() => Toast("Visiting " + fnames[i]));
            }
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Hint", 0, 400, 900, 60), "Connect to find more friends:", 30, UIHelpers.Ink);
            var fb = UIHelpers.EL(modal, "FB", 80, 270, 360, 100);
            UIHelpers.AddButton(fb, new Color(0.23f, 0.35f, 0.60f), "Facebook", 32).onClick.AddListener(() => Toast("Connecting Facebook..."));
            var gl = UIHelpers.EL(modal, "GL", 460, 270, 360, 100);
            UIHelpers.AddButton(gl, new Color(0.95f, 0.95f, 0.95f), "Google", 32).onClick.AddListener(() => Toast("Connecting Google..."));
        }

        // ---------------- 挑战（原型 11） ----------------
        void OpenChallenge()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Challenge", UIHelpers.Amber, 880, 1300, 310);
            var card = UIHelpers.EL(modal, "Card", 60, 520, 760, 560);
            UIHelpers.AddImage(card, null, UIHelpers.Cream);
            UIHelpers.AddImage(UIHelpers.EL(card, "Pic", 60, 300, 220, 220), UIHelpers.LoadSprite("momo_cat"), UIHelpers.BoardBg).preserveAspect = true;
            UIHelpers.AddLabel(UIHelpers.EL(card, "T", 320, 440, 380, 60), "Daily Challenge", 40, UIHelpers.Ink, TextAnchor.MiddleLeft);
            UIHelpers.AddLabel(UIHelpers.EL(card, "D", 320, 320, 380, 110), "Clear any 10x10 puzzle before time runs out!", 28, UIHelpers.Ink, TextAnchor.UpperLeft);
            UIHelpers.AddLabel(UIHelpers.EL(card, "R", 60, 40, 640, 60), "Reward: +200 Cat Coins   +5 Fragments", 30, UIHelpers.Teal, TextAnchor.MiddleLeft);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Timer", 0, 410, 880, 60), "Resets in 12:34:56", 30, UIHelpers.Red);
            var start = UIHelpers.EL(modal, "Start", 140, 180, 600, 160);
            UIHelpers.AddButton(start, UIHelpers.Amber, "Start Challenge", 46).onClick.AddListener(() =>
            {
                ClearPopup();
                int lid = Mathf.Min(SaveSystem.data.unlockedLevel, 90);
                var lvl = GameConfig.GetLevel(lid);
                if (lvl != null) StartLevel(lvl); else Toast("No challenge available");
            });
        }

        // ---------------- 个性装扮（原型 02） ----------------
        void OpenPersonalization()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Customize", UIHelpers.Amber, 980, 1560, 180);
            // 预览面板
            var prev = UIHelpers.EL(modal, "Preview", 60, 800, 860, 400);
            UIHelpers.AddImage(prev, null, UIHelpers.Cream);
            UIHelpers.AddLabel(UIHelpers.EL(prev, "AvL", 40, 300, 200, 50), "Avatar", 30, UIHelpers.Ink, TextAnchor.MiddleLeft);
            UIHelpers.AddLabel(UIHelpers.EL(prev, "BgL", 40, 230, 200, 50), "Background", 30, UIHelpers.Ink, TextAnchor.MiddleLeft);
            var pcat = UIHelpers.EL(prev, "PCat", 360, 60, 200, 200);
            UIHelpers.AddImage(pcat, CatSprite(1), Color.white).preserveAspect = true;
            // 标签栏
            string[] tabs = { "Cat", "Piece", "Frame", "Bg" };
            float tx = 60;
            for (int i = 0; i < tabs.Length; i++)
            {
                var tb = UIHelpers.EL(modal, "Tab" + i, tx, 1230, 215, 90);
                int ti = i; Color c = (i == 0) ? UIHelpers.Amber : UIHelpers.Peach;
                UIHelpers.AddButton(tb, c, TabName(tabs[i]), 30).onClick.AddListener(() => BuildCustomizeGrid(modal, tabs[ti], pcat));
                tx += 230;
            }
            // 网格容器
            var grid = UIHelpers.EL(modal, "GridBox", 80, 120, 820, 660);
            BuildCustomizeGrid(grid, "Cat", pcat);
        }

        string TabName(string t)
        {
            if (t == "Cat") return "猫咪";
            if (t == "Piece") return "棋子";
            if (t == "Frame") return "头像框";
            return "背景";
        }

        void BuildCustomizeGrid(Transform grid, string tab, Transform previewCat)
        {
            ClearChildren(grid);
            int step = 220; // cell 200 + gap 20
            if (tab == "Cat" && GameConfig.Data != null && GameConfig.Data.cats != null)
            {
                int idx = 0;
                foreach (var c in GameConfig.Data.cats)
                {
                    if (idx >= 9) break;
                    int col = idx % 3; int row = idx / 3;
                    int cx = 20 + col * step; int cy = 660 - (row + 1) * step;
                    bool own = c.unlocked;
                    var cellRt = UIHelpers.EL(grid, "C" + idx, cx, cy, 200, 200);
                    UIHelpers.AddImage(cellRt, null, UIHelpers.Paper);
                    if (own)
                    {
                        UIHelpers.AddImage(UIHelpers.EL(cellRt, "Img", 35, 50, 130, 110), CatSprite(c.id), Color.white).preserveAspect = true;
                        UIHelpers.AddLabel(UIHelpers.EL(cellRt, "Nm", 0, 15, 200, 40), c.name, 26, UIHelpers.Ink);
                        if (c.id == 1) UIHelpers.Badge(cellRt, 150, 150, 44, "✓");
                        int cid = c.id;
                        var btn = cellRt.gameObject.AddComponent<Button>();
                        var im = cellRt.GetComponent<Image>(); btn.targetGraphic = im;
                        ColorBlock cb = btn.colors; cb.normalColor = Color.white; cb.highlightedColor = UIHelpers.Cream; cb.pressedColor = UIHelpers.Sage; btn.colors = cb;
                        btn.onClick.AddListener(() => { if (previewCat != null) { var p = previewCat.GetComponent<Image>(); if (p != null) p.sprite = CatSprite(cid); } Toast("Equipped " + c.name); });
                    }
                    else
                    {
                        UIHelpers.AddImage(UIHelpers.EL(cellRt, "L", 35, 50, 130, 110), null, new Color(0.8f, 0.78f, 0.75f));
                        UIHelpers.Badge(cellRt, 150, 150, 44, "L");
                    }
                    idx++;
                }
            }
            else
            {
                for (int i = 0; i < 9; i++)
                {
                    int col = i % 3; int row = i / 3;
                    int cx = 20 + col * step; int cy = 660 - (row + 1) * step;
                    var cellRt = UIHelpers.EL(grid, "C" + i, cx, cy, 200, 200);
                    UIHelpers.AddImage(cellRt, null, UIHelpers.Paper);
                    bool unlocked = (i == 0);
                    if (unlocked)
                    {
                        UIHelpers.AddImage(UIHelpers.EL(cellRt, "Img", 35, 50, 130, 110), UIHelpers.LoadSprite("ui_circle"), (tab == "Bg") ? UIHelpers.Sage : UIHelpers.Amber).preserveAspect = true;
                        UIHelpers.Badge(cellRt, 150, 150, 44, "✓");
                        UIHelpers.AddLabel(UIHelpers.EL(cellRt, "Nm", 0, 15, 200, 40), "Default", 26, UIHelpers.Ink);
                    }
                    else
                    {
                        UIHelpers.AddImage(UIHelpers.EL(cellRt, "L", 35, 50, 130, 110), null, new Color(0.8f, 0.78f, 0.75f));
                        UIHelpers.Badge(cellRt, 150, 150, 44, "L");
                        int price = 1000;
                        var btn = cellRt.gameObject.AddComponent<Button>();
                        var im = cellRt.GetComponent<Image>(); btn.targetGraphic = im;
                        ColorBlock cb = btn.colors; cb.normalColor = Color.white; cb.highlightedColor = UIHelpers.Cream; cb.pressedColor = UIHelpers.Sage; btn.colors = cb;
                        btn.onClick.AddListener(() => OpenCosmeticBuy(TabName(tab) + " #" + (i + 1), price));
                    }
                }
            }
        }

        // ---------------- 每日签到（原型：月历） ----------------
        void OpenCalendar()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Daily Rewards", UIHelpers.Red, 900, 1500, 210);
            UIHelpers.RibbonBanner(modal, 200, 1230, 500, 110, "November 2026", UIHelpers.Red, UIHelpers.Paper);
            string[] wd = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            for (int i = 0; i < 7; i++)
                UIHelpers.AddLabel(UIHelpers.EL(modal, "W" + i, 60 + i * 115, 1100, 115, 60), wd[i], 26, UIHelpers.Ink);
            int today = 2;
            for (int d = 1; d <= 30; d++)
            {
                int col = (d - 1) % 7; int row = (d - 1) / 7;
                float x = 60 + col * 115; float y = 1040 - row * 120;
                var cell = UIHelpers.EL(modal, "D" + d, x, y, 100, 100);
                bool claimed = claimedDays.Contains(d);
                bool isToday = (d == today);
                UIHelpers.AddImage(cell, null, claimed ? UIHelpers.Cream : (isToday ? UIHelpers.Peach : UIHelpers.Paper));
                UIHelpers.AddLabel(UIHelpers.EL(cell, "N", 0, 25, 100, 50), d.ToString(), 34, UIHelpers.Ink);
                if (claimed) UIHelpers.Badge(cell, 60, 55, 40, "✓");
                else if (isToday) UIHelpers.Badge(cell, 60, 55, 40, "!");
            }
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Prog", 0, 420, 900, 60), "Streak chests at 7 / 14 / 21 / 28 days", 28, UIHelpers.Ink);
            var claim = UIHelpers.EL(modal, "Claim", 250, 200, 400, 130);
            UIHelpers.AddButton(claim, UIHelpers.Amber, "Claim Day " + today, 40).onClick.AddListener(() =>
            {
                if (claimedDays.Contains(today)) { Toast("Already claimed today"); return; }
                claimedDays.Add(today);
                if (economy != null) economy.AddFish(20);
                Toast("Day " + today + " reward: +20 Fish!");
            });
        }

        // ---------------- 登录 / 保存进度（原型 09） ----------------
        void OpenLogin()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Save Progress", UIHelpers.Teal, 860, 1100, 410);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Msg", 0, 760, 860, 120), "Log in to save your progress!", 36, UIHelpers.Ink);
            var fb = UIHelpers.EL(modal, "FB", 130, 560, 600, 130);
            UIHelpers.AddButton(fb, new Color(0.23f, 0.35f, 0.60f), "Log in with Facebook", 36).onClick.AddListener(() => Toast("Logged in with Facebook (mock)"));
            var gl = UIHelpers.EL(modal, "GL", 130, 400, 600, 130);
            UIHelpers.AddButton(gl, new Color(0.95f, 0.95f, 0.95f), "Log in with Google", 36).onClick.AddListener(() => Toast("Logged in with Google (mock)"));
            var del = UIHelpers.EL(modal, "Del", 130, 230, 600, 90);
            UIHelpers.AddButton(del, UIHelpers.Peach, "Delete Account", 32).onClick.AddListener(() => Toast("Account deletion requested"));
        }

        // ---------------- 月度订阅 $2.99（原型 13） ----------------
        void OpenMonthlySub()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Monthly", UIHelpers.Amber, 860, 1400, 260);
            UIHelpers.RibbonBanner(modal, 180, 1180, 500, 110, "SUPER OFFER", UIHelpers.Red, UIHelpers.Paper);
            var price = UIHelpers.EL(modal, "Price", 280, 1020, 300, 130);
            UIHelpers.AddImage(price, null, UIHelpers.Amber);
            UIHelpers.AddLabel(UIHelpers.EL(price, "T", 0, 0, 300, 130), "$ 2.99", 56, UIHelpers.Paper);
            string[] ben = { "Stamina cap +10", "Extra stamina x10 daily login", "Unlock limited furniture", "Get 36 monthly fragments now" };
            for (int i = 0; i < ben.Length; i++)
            {
                float y = 880 - i * 130;
                var row = UIHelpers.EL(modal, "B" + i, 120, y, 620, 110);
                UIHelpers.AddImage(row, null, UIHelpers.Cream);
                UIHelpers.AddImage(UIHelpers.EL(row, "Chk", 30, 25, 60, 60), UIHelpers.LoadSprite("ui_circle"), UIHelpers.GreenOn).preserveAspect = true;
                UIHelpers.AddLabel(UIHelpers.EL(row, "Tx", 110, 0, 500, 110), ben[i], 30, UIHelpers.Ink, TextAnchor.MiddleLeft);
            }
            var sub = UIHelpers.EL(modal, "Sub", 130, 300, 600, 150);
            UIHelpers.AddButton(sub, UIHelpers.Amber, "Subscribe", 46).onClick.AddListener(() => { if (economy != null) economy.AddEnergy(10); Toast("Subscribed! Stamina +10 (mock)"); });
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Restore", 0, 150, 860, 70), "Restore Purchase >", 30, UIHelpers.Teal);
        }

        // ---------------- 月度拼图（关卡选择 Monthly 标签） ----------------
        void OpenMonthlyPuzzles()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Monthly Puzzles", UIHelpers.Amber, 880, 1300, 310);
            var list = UIHelpers.EL(modal, "List", 60, 240, 760, 980);
            int shown = 0;
            if (GameConfig.Data != null)
            {
                foreach (var lvl in GameConfig.Data.levels)
                {
                    if (lvl.id < 10 || lvl.id > 12) continue;
                    if (shown >= 3) break;
                    float ry = 980 - shown * 330;
                    var card = UIHelpers.EL(list, "M" + lvl.id, 0, ry, 760, 300);
                    UIHelpers.AddImage(card, null, UIHelpers.Cream);
                    UIHelpers.AddImage(UIHelpers.EL(card, "Pic", 40, 60, 180, 180), UIHelpers.LoadSprite("momo_cat"), UIHelpers.BoardBg).preserveAspect = true;
                    UIHelpers.AddLabel(UIHelpers.EL(card, "T", 260, 200, 460, 60), "Monthly #" + (shown + 1), 38, UIHelpers.Ink, TextAnchor.MiddleLeft);
                    UIHelpers.AddLabel(UIHelpers.EL(card, "D", 260, 110, 460, 80), "Special seasonal puzzle", 28, UIHelpers.Ink, TextAnchor.MiddleLeft);
                    int lid = lvl.id;
                    var go = UIHelpers.EL(card, "Go", 260, 20, 460, 70);
                    UIHelpers.AddButton(go, UIHelpers.Amber, "Play", 36).onClick.AddListener(() => { ClearPopup(); var L = GameConfig.GetLevel(lid); if (L != null) StartLevel(L); });
                    shown++;
                }
            }
            if (shown == 0) UIHelpers.AddLabel(UIHelpers.EL(modal, "Empty", 0, 600, 880, 60), "Monthly puzzles coming soon", 32, UIHelpers.Ink);
        }

        // ---------------- 去广告 $4.99（原型 12） ----------------
        void OpenNoAds()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "No Ads", UIHelpers.Amber, 860, 1100, 410);
            UIHelpers.RibbonBanner(modal, 180, 880, 500, 110, "FOREVER DEAL", UIHelpers.Red, UIHelpers.Paper);
            var price = UIHelpers.EL(modal, "Price", 280, 720, 300, 130);
            UIHelpers.AddImage(price, null, UIHelpers.Amber);
            UIHelpers.AddLabel(UIHelpers.EL(price, "T", 0, 0, 300, 130), "$ 4.99", 56, UIHelpers.Paper);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Terms", 80, 300, 700, 320),
                "Remove all ads forever. Manage or cancel anytime via your app store. No refund for the current period.", 30, UIHelpers.Ink, TextAnchor.UpperLeft);
            var buy = UIHelpers.EL(modal, "Buy", 130, 150, 600, 130);
            UIHelpers.AddButton(buy, UIHelpers.Amber, "Buy", 46).onClick.AddListener(() => Toast("Ads removed (mock)"));
        }

        // ---------------- 看广告得猫金币（原型 23） ----------------
        void OpenAdForCoins()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "More Coins", UIHelpers.Teal, 860, 1300, 310);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Title", 0, 1060, 860, 80), "Need more Cat Coins!", 42, UIHelpers.Ink);
            UIHelpers.AddImage(UIHelpers.EL(modal, "Banner", 130, 820, 600, 200), UIHelpers.LoadSprite("reward_chest"), Color.white).preserveAspect = true;
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Cap", 0, 740, 860, 60), "Used to buy in-game items", 28, UIHelpers.Ink);
            int[] tiers = { 100, 200, 300 };
            for (int i = 0; i < tiers.Length; i++)
            {
                float x = 60 + i * 270;
                var pill = UIHelpers.EL(modal, "T" + i, x, 560, 240, 130);
                UIHelpers.AddImage(pill, null, UIHelpers.Peach);
                UIHelpers.AddLabel(UIHelpers.EL(pill, "V", 0, 0, 240, 130), "+" + tiers[i], 44, UIHelpers.Ink);
            }
            var ad = UIHelpers.EL(modal, "Ad", 130, 360, 600, 150);
            UIHelpers.AddButton(ad, UIHelpers.Teal, "Free (Watch Ad)", 42).onClick.AddListener(() =>
            {
                if (economy != null) economy.AddCoins(100);
                Toast("+100 Cat Coins! (ad)");
                ClearPopup();
            });
            var noad = UIHelpers.EL(modal, "NoAd", 130, 180, 600, 120);
            UIHelpers.AddButton(noad, UIHelpers.Amber, "No Ads", 40).onClick.AddListener(() => OpenNoAds());
        }

        // ---------------- 看广告得体力（原型 28） ----------------
        void OpenAdForEnergy()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "More Energy", UIHelpers.Teal, 860, 1300, 310);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Title", 0, 1060, 860, 80), "Need more Energy!", 42, UIHelpers.Ink);
            UIHelpers.AddImage(UIHelpers.EL(modal, "Banner", 130, 820, 600, 200), UIHelpers.LoadSprite("reward_chest"), Color.white).preserveAspect = true;
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Cap", 0, 740, 860, 60), "Used to play main story", 28, UIHelpers.Ink);
            int[] tiers = { 5, 7 };
            for (int i = 0; i < tiers.Length; i++)
            {
                float x = 120 + i * 320;
                var pill = UIHelpers.EL(modal, "T" + i, x, 560, 280, 130);
                UIHelpers.AddImage(pill, null, UIHelpers.Peach);
                UIHelpers.AddLabel(UIHelpers.EL(pill, "V", 0, 0, 280, 130), "+" + tiers[i], 44, UIHelpers.Ink);
            }
            var ad = UIHelpers.EL(modal, "Ad", 130, 360, 600, 150);
            UIHelpers.AddButton(ad, UIHelpers.Teal, "Free (Watch Ad)", 42).onClick.AddListener(() =>
            {
                if (economy != null) economy.AddEnergy(5);
                Toast("+5 Energy! (ad)");
                ClearPopup();
            });
            var sub = UIHelpers.EL(modal, "Sub", 130, 180, 600, 120);
            UIHelpers.AddButton(sub, UIHelpers.Amber, "Subscribe", 40).onClick.AddListener(() => OpenMonthlySub());
        }

        // ---------------- 装扮道具购买（原型 33） ----------------
        void OpenCosmeticBuy(string item, int price)
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Buy Item", UIHelpers.Amber, 760, 1000, 460);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Name", 0, 720, 760, 70), item, 40, UIHelpers.Ink);
            UIHelpers.AddImage(UIHelpers.EL(modal, "Coin", 250, 540, 60, 60), UIHelpers.LoadSprite("ui_circle"), UIHelpers.Amber).preserveAspect = true;
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Price", 320, 500, 300, 140), price + " Cat Coins", 40, UIHelpers.Teal, TextAnchor.MiddleLeft);
            var buy = UIHelpers.EL(modal, "Buy", 130, 300, 500, 140);
            UIHelpers.AddButton(buy, UIHelpers.Amber, "Buy", 46).onClick.AddListener(() =>
            {
                if (economy != null && economy.SpendCoins(price)) { Toast("Purchased " + item + "!"); ClearPopup(); }
                else Toast("Not enough Cat Coins");
            });
        }

        // ---------------- 语言（原型 08 子页） ----------------
        void OpenLanguage()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Language", UIHelpers.Amber, 760, 1000, 460);
            string[] langs = { "English", "Español", "Français", "简体中文" };
            for (int i = 0; i < langs.Length; i++)
            {
                float y = 720 - i * 150;
                var row = UIHelpers.EL(modal, "L" + i, 80, y, 600, 120);
                int li = i;
                UIHelpers.AddButton(row, (i == 0) ? UIHelpers.Amber : UIHelpers.Peach, langs[i], 36).onClick.AddListener(() => Toast("Language set: " + langs[li]));
            }
        }

        // ---------------- 反馈 ----------------
        void OpenFeedback()
        {
            ClearPopup();
            var modal = UIHelpers.BuildModal(popupRoot, "Feedback", UIHelpers.Amber, 860, 1100, 410);
            UIHelpers.AddLabel(UIHelpers.EL(modal, "Msg", 80, 700, 700, 220),
                "Love Picomeow? Tell us what you think or report a bug. We read every message!", 32, UIHelpers.Ink, TextAnchor.UpperLeft);
            var send = UIHelpers.EL(modal, "Send", 130, 380, 600, 140);
            UIHelpers.AddButton(send, UIHelpers.Amber, "Send Feedback", 44).onClick.AddListener(() => Toast("Thanks for your feedback!"));
        }

        void ClearPopup()
        {
            for (int i = popupRoot.childCount - 1; i >= 0; i--) Object.Destroy(popupRoot.GetChild(i).gameObject);
        }

        void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }

        void Toast(string msg)
        {
            StartCoroutine(ToastRoutine(msg));
        }

        IEnumerator ToastRoutine(string msg)
        {
            var t = UIHelpers.EL(popupRoot, "Toast", 190, 320, 700, 140);
            UIHelpers.AddImage(t, null, new Color(0.18f, 0.16f, 0.14f, 0.9f));
            UIHelpers.AddLabel(t, msg, 34, UIHelpers.Paper);
            yield return new WaitForSeconds(1.8f);
            Object.Destroy(t.gameObject);
        }
    }
}
