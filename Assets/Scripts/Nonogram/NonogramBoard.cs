using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Picomeow.Nonogram
{
    // 数织棋盘：在指定父级（应为全屏 Fill 面板）内以 1080x1920 参考坐标构建
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

        Transform parent;
        Text mistakeText;
        System.Action<bool> onComplete;

        // 布局
        const float boardLeft = 30, boardRight = 1050;
        const float boardBottom = 120, boardTop = 1540;
        const float rowClueW = 110, colClueH = 110;
        const float headerY = 1560;

        float cell;
        float gridX, gridY;

        // 每个格子的可视组件
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
            mistakes = 0; maxMistakes = lvl.maxMistakes; finished = false;

            float gridW = (boardRight - boardLeft) - rowClueW;
            float gridH = (boardTop - boardBottom) - colClueH;
            cell = Mathf.Floor(Mathf.Min(gridW / n, gridH / n));
            gridX = boardLeft + rowClueW + (gridW - cell * n) / 2f;
            gridY = boardBottom + (gridH - cell * n) / 2f;

            BuildHeader();
            BuildRowClues();
            BuildColClues();
            BuildGrid();

            // 预填提示：锁定若干正确格
            int toFill = Mathf.Min(lvl.initialMarks, CountCorrect());
            int filled = 0;
            for (int r = 0; r < n && filled < toFill; r++)
                for (int c = 0; c < n && filled < toFill; c++)
                    if (solution[r, c] == 1) { SetCell(r, c, Cell.Filled, true); filled++; }

            RefreshAllCells();
        }

        int CountCorrect() { int k = 0; for (int r = 0; r < n; r++) for (int c = 0; c < n; c++) k += solution[r, c]; return k; }

        void BuildHeader()
        {
            // 返回按钮（左上）
            var back = UIHelpers.EL(parent, "BackBtn", 30, headerY, 150, 110);
            var bb = UIHelpers.AddButton(back, UIHelpers.Sage, "Back", 40);
            bb.onClick.AddListener(() => { if (!finished) onComplete(false); });

            // 错误计数（中上）
            var mt = UIHelpers.EL(parent, "MistakeText", 200, headerY, 500, 110);
            mistakeText = UIHelpers.AddLabel(mt, "Mistakes: 0/" + maxMistakes, 42, UIHelpers.Ink);

            // 提示道具（右上）：铲子 / 灯泡 / 零食
            var hints = GameConfig.Data != null ? GameConfig.Data.hintItems : null;
            if (hints != null)
            {
                float hx = boardRight - 150;
                for (int i = hints.Length - 1; i >= 0; i--)
                {
                    var ht = UIHelpers.EL(parent, "Hint_" + hints[i].id, hx, headerY, 140, 110);
                    var hb = UIHelpers.AddButton(ht, UIHelpers.WarmYellow, hints[i].name + "\n" + hints[i].cost, 26);
                    string id = hints[i].id; int cost = hints[i].cost;
                    hb.onClick.AddListener(() => UseHint(id, cost));
                    hx -= 160;
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
                UIHelpers.AddLabel(rt, txt, Mathf.Clamp((int)(cell * 0.4f), 18, 40), UIHelpers.Ink, TextAnchor.MiddleRight);
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
                    // 顶部第一条线索在上
                    float y = boardTop - colClueH + (parts.Length - 1 - i) * per;
                    var rt = UIHelpers.EL(parent, "ColClue_" + c + "_" + i, x, y, cell, per);
                    UIHelpers.AddLabel(rt, parts[i], Mathf.Clamp((int)(cell * 0.35f), 16, 36), UIHelpers.Ink, TextAnchor.MiddleCenter);
                }
            }
        }

        void BuildGrid()
        {
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

                    // 网格线描边
                    var line = rt.gameObject.AddComponent<Outline>();
                    line.effectColor = new Color(0.8f, 0.78f, 0.72f);
                    line.effectDistance = new Vector2(1, 1);

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
            Cell next = (Cell)(((int)cur + 1) % 3); // Empty->Filled->Marked->Empty

            // 错误判定：从非填充变为填充且本应空
            if (next == Cell.Filled && solution[r, c] == 0)
            {
                mistakes++;
                if (mistakeText != null) mistakeText.text = "Mistakes: " + mistakes + "/" + maxMistakes;
                if (mistakes > maxMistakes)
                {
                    finished = true;
                    if (onComplete != null) onComplete(false);
                    return;
                }
            }
            state[r, c] = next;
            RefreshCell(r, c);
            CheckWin();
        }

        void RefreshCell(int r, int c)
        {
            int i = Idx(r, c);
            Cell s = state[r, c];
            if (s == Cell.Filled) cellImages[i].color = UIHelpers.Ink;
            else if (s == Cell.Marked) cellImages[i].color = UIHelpers.Sage;
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
                {
                    bool filled = state[r, c] == Cell.Filled;
                    if (filled != (solution[r, c] == 1)) return;
                }
            finished = true;
            if (onComplete != null) onComplete(true);
        }

        // -------- 提示道具 --------
        void UseHint(string id, int cost)
        {
            if (finished) return;
            var eco = Economy.Instance;
            if (eco == null || !eco.SpendCoins(cost))
            {
                FlashNoCoins();
                return;
            }
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
                    if (solution[r, c] == 1 && state[r, c] != Cell.Filled)
                    { SetCell(r, c, Cell.Filled, true); return; }
        }

        void FillOneRow()
        {
            for (int r = 0; r < n; r++)
            {
                bool need = false;
                for (int c = 0; c < n; c++)
                    if (solution[r, c] == 1 && state[r, c] != Cell.Filled) { need = true; break; }
                if (need)
                {
                    for (int c = 0; c < n; c++)
                        if (solution[r, c] == 1) SetCell(r, c, Cell.Filled, true);
                    return;
                }
            }
        }

        void Reveal3x3()
        {
            int br = Random.Range(0, n - 2), bc = Random.Range(0, n - 2);
            for (int r = br; r < br + 3; r++)
                for (int c = bc; c < bc + 3; c++)
                    if (solution[r, c] == 1) SetCell(r, c, Cell.Filled, true);
        }

        void FlashNoCoins()
        {
            // 资源不足时不执行任何操作（可在此接入 Toast）
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
