using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Picomeow.Config;

namespace Picomeow.Nonogram
{
    // 数织棋盘：在指定父级（拼图屏 Holder）内以 1080x1920 参考坐标构建，严格匹配原型 10 拼图视觉稿
    public class BoardController
    {
        public PictureData pic;
        public LevelData lvl;
        int n;
        int[,] solution;
        enum Cell { Empty, Filled, Marked }
        Cell[,] state;
        bool[,] locked;
        int mistakes;
        int maxMistakes;
        bool finished;
        bool markMode; // false=填色, true=标记X

        Transform parent;
        Text mistakeText, chapterText, levelText;
        Button modeBtn;
        System.Action<bool> onComplete;

        // 布局（1080x1920，左下原点）
        const float boardLeft = 40, boardRight = 1040;
        const float boardBottom = 260, boardTop = 1520;
        const float rowClueW = 140, colClueH = 140;

        float cell;
        float gridX, gridY;
        List<Image> cellImages = new List<Image>();
        List<Text> cellX = new List<Text>();
        List<Button> cellButtons = new List<Button>();

        public void Init(Transform parent, PictureData pic, LevelData lvl, System.Action<bool> onComplete)
        {
            this.parent = parent;
            this.pic = pic; this.lvl = lvl; this.onComplete = onComplete;
            n = pic.size;
            solution = new int[n, n];
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    solution[r, c] = pic.solution[r * n + c] - '0';

            state = new Cell[n, n];
            locked = new bool[n, n];
            mistakes = 0; maxMistakes = lvl.maxMistakes; finished = false; markMode = false;

            float gridW = (boardRight - boardLeft) - rowClueW;
            float gridH = (boardTop - boardBottom) - colClueH;
            cell = Mathf.Floor(Mathf.Min(gridW / n, gridH / n));
            gridX = boardLeft + rowClueW + (gridW - cell * n) / 2f;
            gridY = boardBottom + (gridH - cell * n) / 2f;

            BuildHeader();
            BuildFooter();
            BuildRowClues();
            BuildColClues();
            BuildGrid();

            int toFill = Mathf.Min(lvl.initialMarks, CountCorrect());
            int filled = 0;
            for (int r = 0; r < n && filled < toFill; r++)
                for (int c = 0; c < n && filled < toFill; c++)
                    if (solution[r, c] == 1) { SetCell(r, c, Cell.Filled, true); filled++; }

            RefreshAllCells();
        }

        int CountCorrect() { int k = 0; for (int r = 0; r < n; r++) for (int c = 0; c < n; c++) k += solution[r, c]; return k; }

        // 顶部条：返回 / 体力(心) / 章节·关卡 / 猫咪 / 设置
        void BuildHeader()
        {
            // 返回（左上）
            UIHelpers.CloseButton(parent, 40, 1720, 90, () => { if (!finished) onComplete(false); });
            // 体力（3 颗心，左）
            for (int i = 0; i < 3; i++)
            {
                var h = UIHelpers.EL(parent, "Life_" + i, 160 + i * 70, 1720, 56, 56);
                UIHelpers.AddImage(h, UIHelpers.LoadSprite("ui_circle"), UIHelpers.Red).preserveAspect = true;
            }
            // 章节·关卡（中）
            var cl = UIHelpers.EL(parent, "ChapterLevel", 360, 1700, 360, 120);
            chapterText = UIHelpers.AddLabel(cl, "Chapter " + pic.chapter, 36, UIHelpers.Ink);
            levelText = UIHelpers.AddLabel(UIHelpers.EL(cl, "Lv", 0, 0, 360, 60), "Level " + lvl.id, 40, UIHelpers.Ink);
            // 猫咪插画（右）
            var cat = UIHelpers.EL(parent, "CatHead", 800, 1680, 160, 160);
            UIHelpers.AddImage(cat, UIHelpers.LoadSprite("momo_cat"), Color.white).preserveAspect = true;
            // 设置（右上）
            var set = UIHelpers.EL(parent, "SetBtn", 980, 1720, 90, 90);
            UIHelpers.AddButton(set, UIHelpers.Sage, "S", 50);
        }

        // 底部栏：模式切换 + 3 个道具
        void BuildFooter()
        {
            var foot = UIHelpers.EL(parent, "Footer", 0, 20, 1080, 220);
            UIHelpers.AddImage(foot, null, UIHelpers.FooterBg);
            // 模式切换
            var m = UIHelpers.EL(foot, "Mode", 40, 60, 360, 110);
            modeBtn = UIHelpers.AddButton(m, UIHelpers.Teal, "Fill", 40);
            modeBtn.onClick.AddListener(() => { markMode = !markMode; modeBtn.GetComponentInChildren<Text>().text = markMode ? "Mark X" : "Fill"; });
            // 道具
            var hints = (GameConfig.Data != null) ? GameConfig.Data.hintItems : null;
            if (hints != null)
            {
                float hx = 460;
                for (int i = 0; i < hints.Length && i < 3; i++)
                {
                    var ht = UIHelpers.EL(foot, "Tool_" + hints[i].id, hx, 55, 180, 120);
                    var hb = UIHelpers.AddButton(ht, UIHelpers.Paper, hints[i].name + "\n$" + hints[i].cost, 26);
                    string id = hints[i].id; int cost = hints[i].cost;
                    hb.onClick.AddListener(() => UseHint(id, cost));
                    hx += 200;
                }
            }
        }

        void BuildRowClues()
        {
            for (int r = 0; r < n; r++)
            {
                string[] parts = pic.rowClues[r].Split(',');
                string txt = "";
                for (int i = 0; i < parts.Length; i++) txt += (i > 0 ? " " : "") + parts[i];
                float y = gridY + (n - 1 - r) * cell;
                var rt = UIHelpers.EL(parent, "RowClue_" + r, boardLeft, y, rowClueW, cell);
                UIHelpers.AddLabel(rt, txt, Mathf.Clamp((int)(cell * 0.4f), 16, 40), UIHelpers.Ink, TextAnchor.MiddleRight);
            }
        }

        void BuildColClues()
        {
            for (int c = 0; c < n; c++)
            {
                string[] parts = pic.colClues[c].Split(',');
                float x = gridX + c * cell;
                float bandH = colClueH;
                float per = bandH / parts.Length;
                for (int i = 0; i < parts.Length; i++)
                {
                    float y = boardTop - colClueH + (parts.Length - 1 - i) * per;
                    var rt = UIHelpers.EL(parent, "ColClue_" + c + "_" + i, x, y, cell, per);
                    UIHelpers.AddLabel(rt, parts[i], Mathf.Clamp((int)(cell * 0.34f), 14, 36), UIHelpers.Ink, TextAnchor.MiddleCenter);
                }
            }
        }

        void BuildGrid()
        {
            // 浅黄面板底
            var panel = UIHelpers.EL(parent, "BoardPanel", boardLeft + rowClueW - 10, boardBottom - 10, (boardRight - boardLeft) - rowClueW + 20, (boardTop - boardBottom) - colClueH + 20);
            UIHelpers.AddImage(panel, null, UIHelpers.BoardBg);

            cellImages.Clear(); cellX.Clear(); cellButtons.Clear();
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                {
                    float x = gridX + c * cell;
                    float y = gridY + (n - 1 - r) * cell;
                    var rt = UIHelpers.EL(parent, "Cell_" + r + "_" + c, x + 2, y + 2, cell - 4, cell - 4);
                    var img = UIHelpers.AddImage(rt, null, UIHelpers.Paper);
                    var xt = UIHelpers.EL(rt, "X", 0, 0, rt.sizeDelta.x, rt.sizeDelta.y);
                    var xtxt = UIHelpers.AddLabel(xt, "", (int)(cell * 0.5f), UIHelpers.Teal);
                    var btn = rt.gameObject.AddComponent<Button>();
                    btn.targetGraphic = img;
                    ColorBlock cb = btn.colors; cb.normalColor = UIHelpers.Paper; cb.highlightedColor = UIHelpers.Cream;
                    cb.pressedColor = UIHelpers.Sage; btn.colors = cb;
                    int rr = r, cc = c;
                    btn.onClick.AddListener(() => OnCellClick(rr, cc));
                    var line = rt.gameObject.AddComponent<Outline>();
                    line.effectColor = new Color(0.8f, 0.78f, 0.72f); line.effectDistance = new Vector2(1, 1);
                    cellImages.Add(img); cellX.Add(xtxt); cellButtons.Add(btn);
                }
            }
        }

        int Idx(int r, int c) { return r * n + c; }

        void SetCell(int r, int c, Cell s, bool lockIt)
        {
            state[r, c] = s;
            if (lockIt) locked[r, c] = true;
        }

        void OnCellClick(int r, int c)
        {
            if (finished || locked[r, c]) return;
            Cell cur = state[r, c];
            Cell next;
            if (markMode) next = (cur == Cell.Marked) ? Cell.Empty : Cell.Marked;
            else next = (cur == Cell.Filled) ? Cell.Marked : Cell.Filled; // 填模式：填->标X->空

            if (next == Cell.Filled && solution[r, c] == 0)
            {
                mistakes++;
                if (mistakeText != null) mistakeText.text = "Mistakes: " + mistakes + "/" + maxMistakes;
                if (mistakes > maxMistakes) { finished = true; onComplete(false); return; }
            }
            state[r, c] = next;
            RefreshCell(r, c);
            CheckWin();
        }

        void RefreshCell(int r, int c)
        {
            int i = Idx(r, c);
            Cell s = state[r, c];
            if (s == Cell.Filled) cellImages[i].color = UIHelpers.Amber;
            else if (s == Cell.Marked) cellImages[i].color = UIHelpers.Cream;
            else cellImages[i].color = UIHelpers.Paper;
            cellX[i].text = (s == Cell.Marked) ? "X" : "";
        }

        void RefreshAllCells()
        {
            for (int r = 0; r < n; r++) for (int c = 0; c < n; c++) RefreshCell(r, c);
        }

        void CheckWin()
        {
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if ((state[r, c] == Cell.Filled) != (solution[r, c] == 1)) return;
            finished = true;
            if (onComplete != null) onComplete(true);
        }

        // 提示道具
        void UseHint(string id, int cost)
        {
            if (finished) return;
            var eco = Economy.Instance;
            if (eco == null || !eco.SpendCoins(cost)) { return; }
            if (id == "shovel") RevealOneCorrect();
            else if (id == "bulb") FillOneRow();
            else if (id == "treat") Reveal3x3();
            RefreshAllCells();
            CheckWin();
        }

        void RevealOneCorrect()
        {
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (solution[r, c] == 1 && state[r, c] != Cell.Filled) { SetCell(r, c, Cell.Filled, true); return; }
        }

        void FillOneRow()
        {
            for (int r = 0; r < n; r++)
            {
                bool need = false;
                for (int c = 0; c < n; c++) if (solution[r, c] == 1 && state[r, c] != Cell.Filled) { need = true; break; }
                if (need) { for (int c = 0; c < n; c++) if (solution[r, c] == 1) SetCell(r, c, Cell.Filled, true); return; }
            }
        }

        void Reveal3x3()
        {
            int br = Random.Range(0, n - 2), bc = Random.Range(0, n - 2);
            for (int r = br; r < br + 3; r++)
                for (int c = bc; c < bc + 3; c++)
                    if (solution[r, c] == 1) SetCell(r, c, Cell.Filled, true);
        }
    }

    public static class NonogramBoard
    {
        public static BoardController Build(Transform parent, PictureData pic, LevelData lvl, System.Action<bool> onComplete)
        {
            var ctrl = new BoardController();
            ctrl.Init(parent, pic, lvl, onComplete);
            return ctrl;
        }
    }
}
