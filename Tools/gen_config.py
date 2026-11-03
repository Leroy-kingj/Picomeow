#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Picomeow 运行时配置生成器
- 内置数织(Nonogram)严格唯一解/逻辑可解求解器
- 生成 config.json: 图片(谜题)、关卡、家具、装修对话、新手引导、经济、道具
用法:
  python3 gen_config.py --measure      # 仅测量各尺寸接受率
  python3 gen_config.py                # 生成正式 config.json
"""
import os, sys, json, random, argparse, time

# ---------------- 数织求解器 ----------------
def clues_of(row):
    clues, run, n = [], 0, len(row)
    for v in row:
        if v == 1:
            run += 1
        else:
            if run > 0:
                clues.append(run); run = 0
    if run > 0:
        clues.append(run)
    return clues if clues else [0]

_MASK_CACHE = {}
def masks_of(n, clues):
    """返回所有满足 clues 的长度为 n 的位掩码(整型)。clues=[0] 表示全空。"""
    key = (n, tuple(clues))
    cached = _MASK_CACHE.get(key)
    if cached is not None:
        return cached
    if clues == [0]:
        _MASK_CACHE[key] = [0]
        return [0]
    res = []
    def gen(idx, start, cur):
        if idx == len(clues):
            res.append(cur)
            return
        c = clues[idx]
        after = 0
        if idx + 1 < len(clues):
            after = sum(clues[idx+1:]) + (len(clues) - idx - 1)
        max_start = n - c - after
        for s in range(start, max_start + 1):
            v = ((1 << c) - 1) << s
            gen(idx + 1, s + c + 1, cur | v)
    gen(0, 0, 0)
    _MASK_CACHE[key] = res
    return res

def _consistent(ms, line):
    out = []
    for m in ms:
        ok = True
        for i, v in enumerate(line):
            if v >= 0 and ((m >> i) & 1) != v:
                ok = False; break
        if ok:
            out.append(m)
    return out

def propagate(n, row_clues, col_clues, init=None):
    grid = [[ -1 if init is None else init[r][c] for c in range(n)] for r in range(n)]
    row_ms = [masks_of(n, c) for c in row_clues]
    col_ms = [masks_of(n, c) for c in col_clues]
    changed = True
    while changed:
        changed = False
        for r in range(n):
            vm = _consistent(row_ms[r], [grid[r][c] for c in range(n)])
            if not vm: return None
            andm = orm = vm[0]
            for m in vm[1:]:
                andm &= m; orm |= m
            for c in range(n):
                if (andm >> c) & 1:
                    if grid[r][c] == 0: return None
                    if grid[r][c] == -1: grid[r][c] = 1; changed = True
                elif not ((orm >> c) & 1):
                    if grid[r][c] == 1: return None
                    if grid[r][c] == -1: grid[r][c] = 0; changed = True
        for c in range(n):
            vm = _consistent(col_ms[c], [grid[r][c] for r in range(n)])
            if not vm: return None
            andm = orm = vm[0]
            for m in vm[1:]:
                andm &= m; orm |= m
            for r in range(n):
                if (andm >> r) & 1:
                    if grid[r][c] == 0: return None
                    if grid[r][c] == -1: grid[r][c] = 1; changed = True
                elif not ((orm >> r) & 1):
                    if grid[r][c] == 1: return None
                    if grid[r][c] == -1: grid[r][c] = 0; changed = True
    complete = all(grid[r][c] != -1 for r in range(n) for c in range(n))
    return grid, complete

def count_solutions(n, row_clues, col_clues, cap=2, init=None):
    res = propagate(n, row_clues, col_clues, init)
    if res is None: return 0
    grid, complete = res
    if complete: return 1
    cnt = [0]
    def solve(g):
        if cnt[0] >= cap: return
        r = propagate(n, row_clues, col_clues, g)
        if r is None: return
        g2, done = r
        if done:
            cnt[0] += 1; return
        # 选择第一个未定格
        for rr in range(n):
            for cc in range(n):
                if g2[rr][cc] == -1:
                    for v in (0, 1):
                        ng = [row[:] for row in g2]
                        ng[rr][cc] = v
                        solve(ng)
                        if cnt[0] >= cap: return
                    return
    solve(grid)
    return cnt[0]

def is_logic_solvable(n, row_clues, col_clues):
    res = propagate(n, row_clues, col_clues)
    return res is not None and res[1]

def gen_puzzle(n, existing, attempts=300000, logic_only=True):
    for _ in range(attempts):
        grid = [[1 if random.random() < 0.5 else 0 for _ in range(n)] for _ in range(n)]
        row_clues = [clues_of(r) for r in grid]
        col_clues = [clues_of([grid[r][c] for r in range(n)]) for c in range(n)]
        if logic_only:
            if not is_logic_solvable(n, row_clues, col_clues):
                continue
        else:
            if count_solutions(n, row_clues, col_clues, cap=2) != 1:
                continue
        key = tuple(tuple(r) for r in grid)
        if key in existing:
            continue
        existing.add(key)
        return grid, row_clues, col_clues
    return None

def self_test():
    # 全空 5x5 -> 唯一
    n = 5
    g = [[0]*n for _ in range(n)]
    rc = [clues_of(r) for r in g]; cc = [clues_of([g[r][c] for r in range(n)]) for c in range(n)]
    assert count_solutions(n, rc, cc, cap=3) == 1, "all-zero fail"
    # 全满 5x5 -> 唯一
    g = [[1]*n for _ in range(n)]
    rc = [clues_of(r) for r in g]; cc = [clues_of([g[r][c] for r in range(n)]) for c in range(n)]
    assert count_solutions(n, rc, cc, cap=3) == 1, "all-one fail"
    # 单满行
    g = [[1,1,1,1,1],[0,0,0,0,0],[0,0,0,0,0],[0,0,0,0,0],[0,0,0,0,0]]
    rc = [clues_of(r) for r in g]; cc = [clues_of([g[r][c] for r in range(n)]) for c in range(n)]
    assert count_solutions(n, rc, cc, cap=3) == 1, "one-row fail"
    print("[self_test] OK")

# ---------------- 配置生成 ----------------
CHAPTER_SIZES = {1: [5, 6, 7, 8, 9], 2: [6, 7, 8, 9, 9]}   # 每章 5 张图
LEVELS_PER_PICTURE = 9

FURNITURE = [
    {"id": "rug",      "name": "Round Rug",      "icon": "furniture_set", "slot": 0},
    {"id": "sofa",     "name": "Cozy Sofa",      "icon": "furniture_set", "slot": 1},
    {"id": "plant",    "name": "Potted Plant",   "icon": "furniture_set", "slot": 2},
    {"id": "shelf",    "name": "Wooden Shelf",   "icon": "furniture_set", "slot": 3},
    {"id": "lamp",     "name": "Floor Lamp",     "icon": "furniture_set", "slot": 4},
    {"id": "clock",    "name": "Wall Clock",     "icon": "furniture_set", "slot": 5},
    {"id": "frame",    "name": "Framed Picture", "icon": "furniture_set", "slot": 6},
    {"id": "bed",      "name": "Cat Bed",        "icon": "furniture_set", "slot": 7},
]

# 装修对话（数据驱动，可由游戏内对话系统播放）
DIALOGUE = [
    {
        "id": "room1", "roomId": 1, "title": "The Living Room",
        "nodes": [
            {"id": "n1", "speaker": "Momo", "text": "Welcome home! This old living room needs some love. Where should we begin?",
             "choices": [
                 {"text": "Lay down a soft rug first.", "furnitureId": "rug", "next": "n2"},
                 {"text": "Put up a shelf for my books.", "furnitureId": "shelf", "next": "n2"}
             ]},
            {"id": "n2", "speaker": "Momo", "text": "Nice choice! Now somewhere to relax after a long day of puzzles?",
             "choices": [
                 {"text": "A cozy sofa, please.", "furnitureId": "sofa", "next": "n3"},
                 {"text": "A warm floor lamp.", "furnitureId": "lamp", "next": "n3"}
             ]},
            {"id": "n3", "speaker": "Momo", "text": "It's starting to feel like home. One more touch of life?",
             "choices": [
                 {"text": "A little potted plant.", "furnitureId": "plant", "next": "n4"},
                 {"text": "A framed picture on the wall.", "furnitureId": "frame", "next": "n4"}
             ]},
            {"id": "n4", "speaker": "Momo", "text": "Perfect! The living room is ready. You earned a star!"}
        ]
    },
    {
        "id": "room2", "roomId": 2, "title": "The Bedroom",
        "nodes": [
            {"id": "m1", "speaker": "Momo", "text": "Time to fix up the bedroom. A cat needs a good nap spot!",
             "choices": [
                 {"text": "A fluffy cat bed.", "furnitureId": "bed", "next": "m2"},
                 {"text": "A wall clock to track naps.", "furnitureId": "clock", "next": "m2"}
             ]},
            {"id": "m2", "speaker": "Momo", "text": "Cozy! Add something green?",
             "choices": [
                 {"text": "Another plant, I love them.", "furnitureId": "plant", "next": "m3"},
                 {"text": "A second shelf.", "furnitureId": "shelf", "next": "m3"}
             ]},
            {"id": "m3", "speaker": "Momo", "text": "The bedroom looks great. Sweet dreams!"}
        ]
    }
]

ONBOARDING = [
    {"step": 1, "screen": "Loading",       "instruction": "Picomeow is loading your cozy world..."},
    {"step": 2, "screen": "Main",          "instruction": "This is your apartment. Tap PLAY to start a puzzle and earn stars."},
    {"step": 3, "screen": "LevelSelect",   "instruction": "Pick a level. Each puzzle reveals a hidden cat picture."},
    {"step": 4, "screen": "Puzzle",        "instruction": "Tap a cell to FILL it, tap again to mark X, once more to clear. Match the number clues!"},
    {"step": 5, "screen": "Puzzle",        "instruction": "Fill the cells so the numbers on each row and column are satisfied."},
    {"step": 6, "screen": "Result",        "instruction": "Great job! You revealed a cat and earned coins."},
    {"step": 7, "screen": "Main",          "instruction": "Tap Momo the cat to renovate your apartment through dialogue."},
    {"step": 8, "screen": "Dialogue",      "instruction": "Choose how to decorate. Your choices place furniture and earn rewards."},
    {"step": 9, "screen": "Main",          "instruction": "Energy refills over time. Spend it wisely on puzzles!"},
    {"step": 10, "screen": "Main",         "instruction": "That's the basics. Enjoy Picomeow!"},
]

ECONOMY = {"energyMax": 20, "energyRegenMinutes": 5, "startEnergy": 20, "startCoins": 50, "startStars": 0}

HINT_ITEMS = [
    {"id": "shovel", "name": "Shovel",     "desc": "Reveal one correct cell.", "cost": 10},
    {"id": "bulb",   "name": "Light Bulb", "desc": "Auto-fill an entire row.", "cost": 25},
    {"id": "treat",  "name": "Cat Treat",  "desc": "Mark a 3x3 area correctly.", "cost": 15},
]

PALETTES = ["cat_basic", "cat_pastel", "cat_sunset", "cat_mint"]

def build_config():
    random.seed(20261103)
    existing = set()
    pictures, levels = [], []
    pid = 1
    lid = 1
    for chapter in sorted(CHAPTER_SIZES.keys()):
        for size in CHAPTER_SIZES[chapter]:
            res = gen_puzzle(size, existing, attempts=400000, logic_only=True)
            if res is None:
                print("[warn] size %d 在限定尝试内未产出逻辑可解谜题，回退为唯一解模式" % size)
                res = gen_puzzle(size, existing, attempts=400000, logic_only=False)
            if res is None:
                raise RuntimeError("size %d 仍无法生成" % size)
            grid, row_clues, col_clues = res
            reward_coins = 15 + chapter * 5 + size
            reward_stars = 1
            pictures.append({
                "id": pid, "chapter": chapter, "size": size,
                "solution": "".join(str(v) for row in grid for v in row),
                "rowClues": [",".join(str(x) for x in rc) for rc in row_clues],
                "colClues": [",".join(str(x) for x in cc) for cc in col_clues],
                "palette": PALETTES[(pid - 1) % len(PALETTES)],
                "rewardCoins": reward_coins,
                "rewardStars": reward_stars
            })
            for k in range(LEVELS_PER_PICTURE):
                levels.append({
                    "id": lid, "pictureId": pid, "indexInPicture": k,
                    "initialMarks": min(2 + k, size),          # 预填提示数递进
                    "maxMistakes": 3 if chapter == 1 else 4,
                    "energyCost": 5 + (k // 3),
                    "coinReward": 10 + k,
                    "starReward": 1 if k == LEVELS_PER_PICTURE - 1 else 0,
                    "decorationTrigger": (k == LEVELS_PER_PICTURE - 1)  # 每图最后一关触发装修
                })
                lid += 1
            pid += 1
    config = {
        "version": 1,
        "pictures": pictures,
        "levels": levels,
        "furniture": FURNITURE,
        "dialogue": DIALOGUE,
        "onboarding": ONBOARDING,
        "economy": ECONOMY,
        "hintItems": HINT_ITEMS
    }
    return config

def measure():
    self_test()
    print("size | logic_solvable_rate | time(s) for 10000 attempts")
    for n in range(5, 11):
        random.seed(n)
        ok_logic = 0; tot = 10000
        t0 = time.time()
        for _ in range(tot):
            grid = [[1 if random.random() < 0.5 else 0 for _ in range(n)] for _ in range(n)]
            rc = [clues_of(r) for r in grid]
            cc = [clues_of([grid[r][c] for r in range(n)]) for c in range(n)]
            if is_logic_solvable(n, rc, cc):
                ok_logic += 1
        dt = time.time() - t0
        print("%4d | %6.3f%%            | %.1f" % (n, 100.0*ok_logic/tot, dt))

if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("--measure", action="store_true")
    args = ap.parse_args()
    if args.measure:
        measure()
        sys.exit(0)
    self_test()
    cfg = build_config()
    out_dir = os.path.join(os.path.dirname(__file__), "..", "Assets", "StreamingAssets", "Config")
    out_dir = os.path.abspath(out_dir)
    os.makedirs(out_dir, exist_ok=True)
    out_path = os.path.join(out_dir, "config.json")
    with open(out_path, "w", encoding="utf-8") as f:
        json.dump(cfg, f, ensure_ascii=False, separators=(",", ":"))
    print("config.json 生成完成 ->", out_path)
    print("图片数:", len(cfg["pictures"]), " 关卡数:", len(cfg["levels"]))
    print("尺寸分布:", [p["size"] for p in cfg["pictures"]])
