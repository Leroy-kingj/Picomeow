using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Picomeow.Config;
using Picomeow.Core;
using Picomeow.UI;
using Picomeow.Nonogram;
using Picomeow.Apartment;
using Picomeow.Tutorial;

namespace Picomeow.Core
{
    // 总控：构建并管理所有界面，串联玩法 / 经济 / 装修 / 引导
    public class UIManager : MonoBehaviour
    {
        Transform screenRoot, hudRoot, popupRoot, overlayRoot;
        GameObject mainPanel, levelPanel, puzzlePanel, dialoguePanel, resultPopup, onboardingOverlay, loadingText;
        Transform mainFurnitureRoot, puzzleHolder;
        Text energyText, coinsText, starsText;
        Economy economy;
        DialoguePlayer dialoguePlayer;

        LevelData currentLevel;
        Vector2[] furnitureSlots = new Vector2[] {
            new Vector2(120, 1150), new Vector2(820, 1150),
            new Vector2(120, 820),  new Vector2(820, 820),
            new Vector2(450, 980)
        };

        public void Build()
        {
            economy = Economy.Instance;

            screenRoot = UIHelpers.Fill(transform, "ScreenRoot", new Color(0, 0, 0, 0));
            hudRoot = UIHelpers.Fill(transform, "HUD", new Color(0, 0, 0, 0));
            popupRoot = UIHelpers.Fill(transform, "PopupRoot", new Color(0, 0, 0, 0));
            overlayRoot = UIHelpers.Fill(transform, "OverlayRoot", new Color(0, 0, 0, 0));

            loadingText = UIHelpers.EL(screenRoot, "Loading", 0, 820, 1080, 200).gameObject;
            UIHelpers.AddLabel(loadingText.transform, "Loading Picomeow...", 60, UIHelpers.Ink);

            BuildHUD();
            BuildScreens();

            ConfigLoader.Load(this, OnConfigLoaded);
        }

        void BuildHUD()
        {
            var bar = UIHelpers.EL(hudRoot, "Bar", 0, 1720, 1080, 200);
            Image barImg = bar.gameObject.AddComponent<Image>();
            barImg.color = UIHelpers.Sage;

            var e = UIHelpers.EL(bar, "Energy", 40, 40, 380, 120);
            energyText = UIHelpers.AddLabel(e, "Energy 20", 40, UIHelpers.Ink, TextAnchor.MiddleLeft);

            var c = UIHelpers.EL(bar, "Coins", 430, 40, 280, 120);
            coinsText = UIHelpers.AddLabel(c, "Coins 0", 40, UIHelpers.Ink, TextAnchor.MiddleCenter);

            var s = UIHelpers.EL(bar, "Stars", 730, 40, 310, 120);
            starsText = UIHelpers.AddLabel(s, "Stars 0", 40, UIHelpers.Ink, TextAnchor.MiddleRight);
        }

        void BuildScreens()
        {
            // 主界面
            mainPanel = UIHelpers.Fill(screenRoot, "Main", new Color(0, 0, 0, 0)).gameObject;
            var bg = UIHelpers.Fill(mainPanel.transform, "BG", Color.white);
            var bgImg = bg.gameObject.GetComponent<Image>();
            bgImg.sprite = UIHelpers.LoadSprite("apartment_main");
            bgImg.preserveAspect = false;

            mainFurnitureRoot = UIHelpers.EL(mainPanel.transform, "FurnitureRoot", 0, 0, 1080, 1920);

            var title = UIHelpers.EL(mainPanel.transform, "Title", 240, 1500, 600, 130);
            UIHelpers.AddLabel(title, "Your Apartment", 56, UIHelpers.Ink);

            var play = UIHelpers.EL(mainPanel.transform, "PlayBtn", 290, 820, 500, 200);
            var pb = UIHelpers.AddButton(play, UIHelpers.WarmYellow, "PLAY", 64);
            pb.onClick.AddListener(() => ShowScreen("LevelSelect"));

            var momo = UIHelpers.EL(mainPanel.transform, "MomoBtn", 60, 260, 240, 240);
            var mbImg = UIHelpers.AddImage(momo, UIHelpers.LoadSprite("momo_cat"), Color.white);
            mbImg.preserveAspect = true;
            var mb = momo.gameObject.AddComponent<Button>();
            mb.targetGraphic = mbImg;
            mb.onClick.AddListener(() => OpenDialogue(0));

            // 关卡选择
            levelPanel = UIHelpers.Fill(screenRoot, "LevelSelect", new Color(0.96f, 0.94f, 0.89f, 1)).gameObject;
            var ltitle = UIHelpers.EL(levelPanel.transform, "LTitle", 240, 1560, 600, 120);
            UIHelpers.AddLabel(ltitle, "Select Level", 56, UIHelpers.Ink);
            var lback = UIHelpers.EL(levelPanel.transform, "LBack", 40, 1560, 180, 120);
            var lb = UIHelpers.AddButton(lback, UIHelpers.Sage, "Back", 40);
            lb.onClick.AddListener(() => ShowScreen("Main"));

            // 数织面板
            puzzlePanel = UIHelpers.Fill(screenRoot, "Puzzle", new Color(0.96f, 0.94f, 0.89f, 1)).gameObject;
            puzzleHolder = UIHelpers.Fill(puzzlePanel.transform, "Holder", new Color(0, 0, 0, 0));

            // 对话面板
            dialoguePanel = UIHelpers.Fill(overlayRoot, "Dialogue", new Color(0, 0, 0, 0)).gameObject;
            dialoguePlayer = dialoguePanel.AddComponent<DialoguePlayer>();

            // 结算弹窗
            resultPopup = UIHelpers.Fill(popupRoot, "Result", new Color(0.1f, 0.09f, 0.08f, 0.6)).gameObject;

            // 新手引导
            onboardingOverlay = UIHelpers.Fill(overlayRoot, "Onboarding", new Color(0, 0, 0, 0)).gameObject;

            mainPanel.SetActive(false);
            levelPanel.SetActive(false);
            puzzlePanel.SetActive(false);
            dialoguePanel.SetActive(false);
            resultPopup.SetActive(false);
            onboardingOverlay.SetActive(false);
        }

        void OnConfigLoaded()
        {
            SaveSystem.Load();
            loadingText.SetActive(false);
            if (economy != null) economy.onChanged.AddListener(RefreshHUD);
            RefreshHUD();
            ShowScreen("Main");
            if (!SaveSystem.data.tutorialDone)
                StartOnboarding();
        }

        void RefreshHUD()
        {
            if (economy == null) return;
            energyText.text = "Energy " + economy.Energy + "  (" + economy.EnergyTimerText() + ")";
            coinsText.text = "Coins " + economy.Coins;
            starsText.text = "Stars " + economy.Stars;
        }

        void ShowScreen(string name)
        {
            mainPanel.SetActive(name == "Main");
            levelPanel.SetActive(name == "LevelSelect");
            puzzlePanel.SetActive(name == "Puzzle");
            if (name == "Main") RefreshApartment();
            if (name == "LevelSelect") BuildLevelGrid();
        }

        // ---------- 关卡选择 ----------
        void BuildLevelGrid()
        {
            // 清理旧列表
            var old = levelPanel.transform.Find("ScrollRoot");
            if (old != null) Object.Destroy(old.gameObject);

            float top = 1460, bottom = 220, left = 40, right = 1040;
            var scroll = UIHelpers.EL(levelPanel.transform, "ScrollRoot", left, bottom, right - left, top - bottom);
            var sImg = scroll.gameObject.AddComponent<Image>();
            sImg.color = new Color(1, 1, 1, 0.001f);
            var mask = scroll.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            var sr = scroll.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;

            var content = new GameObject("Content", typeof(RectTransform)).transform;
            content.SetParent(scroll, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1); crt.sizeDelta = new Vector2(0, 0);
            var gl = content.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(300, 220);
            gl.spacing = new Vector2(20, 20);
            gl.padding = new RectOffset(20, 20, 20, 20);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gl.constraintCount = 3;
            var csf = content.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = crt;
            sr.viewport = scroll;

            if (GameConfig.Data == null) return;
            int unlocked = SaveSystem.data.unlockedLevel;
            foreach (var lvl in GameConfig.Data.levels)
            {
                bool open = lvl.id <= unlocked;
                var cell = UIHelpers.EL(content, "Lv_" + lvl.id, 0, 0, 300, 220);
                Color col = open ? UIHelpers.WarmYellow : new Color(0.7f, 0.7f, 0.7f);
                var btn = UIHelpers.AddButton(cell, col, (open ? "Lv " + lvl.id : "Locked"), 40);
                if (open)
                {
                    int id = lvl.id;
                    btn.onClick.AddListener(() => StartLevel(GameConfig.GetLevel(id)));
                }
            }
        }

        // ---------- 关卡开始 ----------
        void StartLevel(LevelData level)
        {
            if (economy == null) return;
            if (economy.Energy < level.energyCost)
            {
                Toast("Not enough energy!");
                return;
            }
            economy.SpendEnergy(level.energyCost);
            currentLevel = level;
            ClearChildren(puzzleHolder);
            ShowScreen("Puzzle");
            var pic = GameConfig.GetPicture(level.pictureId);
            if (pic != null)
                NonogramBoard.Build(puzzleHolder, pic, level, OnPuzzleDone);
        }

        void OnPuzzleDone(bool win)
        {
            ShowResult(currentLevel, win);
        }

        // ---------- 结算 ----------
        void ShowResult(LevelData level, bool win)
        {
            ClearChildren(resultPopup.transform);
            var card = UIHelpers.EL(resultPopup.transform, "Card", 140, 360, 800, 1200);
            var ci = card.gameObject.AddComponent<Image>();
            ci.color = UIHelpers.Paper;

            if (win)
            {
                // 奖励
                var pic = GameConfig.GetPicture(level.pictureId);
                int coins = level.coinReward;
                int stars = level.starReward;
                economy.AddCoins(coins);
                if (stars > 0) economy.AddStars(stars);
                if (level.id + 1 > SaveSystem.data.unlockedLevel)
                {
                    SaveSystem.data.unlockedLevel = level.id + 1;
                    SaveSystem.Save();
                }

                var head = UIHelpers.EL(card, "Head", 40, 1360, 720, 160);
                UIHelpers.AddLabel(head, "Picture Revealed!", 52, UIHelpers.Ink);
                BuildMiniReveal(card, pic, 60, 860, 720, 440);
                var reward = UIHelpers.EL(card, "Reward", 40, 680, 720, 120);
                UIHelpers.AddLabel(reward, "+" + coins + " coins" + (stars > 0 ? "   +" + stars + " star" : ""), 40, UIHelpers.Teal);

                var cont = UIHelpers.EL(card, "Continue", 240, 420, 520, 200);
                var cb = UIHelpers.AddButton(cont, UIHelpers.WarmYellow, "Continue", 52);
                cb.onClick.AddListener(() =>
                {
                    resultPopup.SetActive(false);
                    if (level.decorationTrigger && !DecorationManager.IsDecorated(DecorationManager.CurrentRoom()))
                        OpenDialogue(0);
                    else
                        ShowScreen("Main");
                });
            }
            else
            {
                var head = UIHelpers.EL(card, "Head", 40, 900, 720, 200);
                UIHelpers.AddLabel(head, "Out of moves!", 52, UIHelpers.Ink);
                var cont = UIHelpers.EL(card, "Retry", 240, 400, 520, 200);
                var cb = UIHelpers.AddButton(cont, UIHelpers.Sage, "Try Again", 52);
                cb.onClick.AddListener(() =>
                {
                    resultPopup.SetActive(false);
                    ShowScreen("LevelSelect");
                });
            }
            resultPopup.SetActive(true);
        }

        void BuildMiniReveal(Transform card, PictureData pic, float x, float y, float w, float h)
        {
            int n = pic.size;
            float cell = Mathf.Floor(Mathf.Min(w / n, h / n));
            float gx = x + (w - cell * n) / 2f;
            float gy = y + (h - cell * n) / 2f;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    bool on = pic.solution[r * n + c] == '1';
                    var rt = UIHelpers.EL(card, "Mini_" + r + "_" + c, gx + c * cell, gy + (n - 1 - r) * cell, cell - 2, cell - 2);
                    UIHelpers.AddImage(rt, null, on ? UIHelpers.Ink : UIHelpers.Paper);
                }
        }

        // ---------- 对话 / 装修 ----------
        void OpenDialogue(int roomId)
        {
            int room = roomId > 0 ? roomId : DecorationManager.CurrentRoom();
            var d = GameConfig.GetDialogueByRoom(room);
            if (d == null) { Toast("No dialogue for room " + room); return; }
            ClearChildren(dialoguePanel.transform);
            dialoguePanel.SetActive(true);
            dialoguePlayer.Play(dialoguePanel.transform, d,
                (fid) => { /* 选择即时视觉可在 OnDialogueFinish 统一处理 */ },
                () => OnDialogueFinish(room));
        }

        void OnDialogueFinish(int room)
        {
            DecorationManager.Decorate(room);
            dialoguePanel.SetActive(false);
            RefreshApartment();
            ShowScreen("Main");
            Toast("Room decorated! +1 star");
        }

        void RefreshApartment()
        {
            ClearChildren(mainFurnitureRoot);
            int decorated = 0;
            if (DecorationManager.IsDecorated(1)) decorated++;
            if (DecorationManager.IsDecorated(2)) decorated++;
            for (int i = 0; i < decorated && i < furnitureSlots.Length; i++)
            {
                var rt = UIHelpers.EL(mainFurnitureRoot, "Furn_" + i, furnitureSlots[i].x - 90, furnitureSlots[i].y - 90, 180, 180);
                UIHelpers.AddImage(rt, UIHelpers.LoadSprite("furniture_set"), Color.white);
            }
        }

        // ---------- 新手引导 ----------
        void StartOnboarding()
        {
            if (GameConfig.Data == null || GameConfig.Data.onboarding == null) { SaveSystem.data.tutorialDone = true; SaveSystem.Save(); return; }
            onboardingOverlay.SetActive(true);
            OnboardingManager.Run(onboardingOverlay.transform, GameConfig.Data.onboarding, () =>
            {
                SaveSystem.data.tutorialDone = true;
                SaveSystem.Save();
            });
        }

        // ---------- 工具 ----------
        void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
                Object.Destroy(t.GetChild(i).gameObject);
        }

        void Toast(string msg)
        {
            StartCoroutine(ToastRoutine(msg));
        }

        IEnumerator ToastRoutine(string msg)
        {
            var t = UIHelpers.EL(popupRoot, "Toast", 240, 320, 600, 140);
            UIHelpers.AddImage(t, null, new Color(0.2f, 0.18f, 0.16f, 0.85f));
            UIHelpers.AddLabel(t, msg, 40, UIHelpers.Paper);
            yield return new WaitForSeconds(1.6f);
            Object.Destroy(t.gameObject);
        }
    }
}
