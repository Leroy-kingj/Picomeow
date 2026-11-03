using System;
using UnityEngine;

namespace Picomeow.Core
{
    [Serializable]
    public class SaveData
    {
        public int energy;
        public long lastRegenTicks; // DateTime.UtcNow.Ticks，记录上次回能时刻
        public int coins;          // 猫金币
        public int fish;           // 小鱼干
        public int fragments;      // 拼图碎片
        public int stars;
        public int unlockedLevel;    // 已解锁的最高关卡 id（1 基）
        public string decoratedRooms; // 已装修房间 id，逗号分隔
        public bool tutorialDone;
        public int collectedCats;   // 已入住（收集）的猫咪数量
    }

    public static class SaveSystem
    {
        const string KEY = "picomeow_save_v1";
        public static SaveData data;

        public static void Load()
        {
            if (data != null) return;
            if (PlayerPrefs.HasKey(KEY))
            {
                try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(KEY)); }
                catch { data = null; }
            }
            if (data == null) data = new SaveData();

            if (string.IsNullOrEmpty(data.decoratedRooms)) data.decoratedRooms = "";

            // 首次初始化默认值（依赖配置中的经济参数）
            if (!PlayerPrefs.HasKey(KEY))
            {
                var eco = (GameConfig.Data != null) ? GameConfig.Data.economy : null;
                data.energy = (eco != null) ? eco.startEnergy : 20;
                data.coins = (eco != null) ? eco.startCoins : 50;
                data.fish = (eco != null) ? eco.startFish : 10;
                data.fragments = (eco != null) ? eco.startFragments : 0;
                data.stars = (eco != null) ? eco.startStars : 0;
                data.unlockedLevel = 1;
                data.decoratedRooms = "";
                data.tutorialDone = false;
                data.collectedCats = 1; // 主角猫默认入住
                data.lastRegenTicks = DateTime.UtcNow.Ticks;
                Save();
            }
        }

        public static void Save()
        {
            if (data == null) return;
            PlayerPrefs.SetString(KEY, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        public static bool IsRoomDecorated(int roomId)
        {
            if (string.IsNullOrEmpty(data.decoratedRooms)) return false;
            foreach (var s in data.decoratedRooms.Split(','))
                if (int.TryParse(s, out int r) && r == roomId) return true;
            return false;
        }

        public static void MarkRoomDecorated(int roomId)
        {
            if (IsRoomDecorated(roomId)) return;
            if (string.IsNullOrEmpty(data.decoratedRooms))
                data.decoratedRooms = roomId.ToString();
            else
                data.decoratedRooms += "," + roomId;
            Save();
        }
    }
}
