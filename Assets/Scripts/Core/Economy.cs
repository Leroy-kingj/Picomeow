using System;
using UnityEngine;
using UnityEngine.Events;

namespace Picomeow.Core
{
    // 经济系统：体力 / 金币 / 星星，体力随时间回满
    public class Economy : MonoBehaviour
    {
        public static Economy Instance { get; private set; }
        public UnityEvent onChanged = new UnityEvent();

        void Awake() { Instance = this; }

        public int EnergyMax { get { return GameConfig.Data != null ? GameConfig.Data.economy.energyMax : 20; } }
        public int EnergyRegenMinutes { get { return GameConfig.Data != null ? GameConfig.Data.economy.energyRegenMinutes : 5; } }

        public int Energy { get { return SaveSystem.data.energy; } }
        public int Coins { get { return SaveSystem.data.coins; } }      // 猫金币 (Cat Coins)
        public int Fish { get { return SaveSystem.data.fish; } }          // 小鱼干 (Fish Treats)
        public int Fragments { get { return SaveSystem.data.fragments; } }// 拼图碎片 (Puzzle Fragments)
        public int Stars { get { return SaveSystem.data.stars; } }

        void Update()
        {
            RegenTick();
        }

        void RegenTick()
        {
            if (GameConfig.Data == null || SaveSystem.data == null) return;
            if (SaveSystem.data.energy >= EnergyMax)
            {
                SaveSystem.data.lastRegenTicks = DateTime.UtcNow.Ticks;
                return;
            }
            long now = DateTime.UtcNow.Ticks;
            long stepTicks = (long)EnergyRegenMinutes * 60 * TimeSpan.TicksPerSecond;
            long elapsed = now - SaveSystem.data.lastRegenTicks;
            if (elapsed >= stepTicks)
            {
                int steps = (int)(elapsed / stepTicks);
                SaveSystem.data.energy = Mathf.Min(EnergyMax, SaveSystem.data.energy + steps);
                SaveSystem.data.lastRegenTicks += steps * stepTicks;
                if (SaveSystem.data.energy >= EnergyMax) SaveSystem.data.lastRegenTicks = now;
                SaveSystem.Save();
                onChanged.Invoke();
            }
        }

        public string EnergyTimerText()
        {
            if (SaveSystem.data.energy >= EnergyMax) return "FULL";
            long now = DateTime.UtcNow.Ticks;
            long stepTicks = (long)EnergyRegenMinutes * 60 * TimeSpan.TicksPerSecond;
            long elapsed = now - SaveSystem.data.lastRegenTicks;
            long remain = stepTicks - (elapsed % stepTicks);
            int sec = (int)(remain / TimeSpan.TicksPerSecond);
            return string.Format("{0}:{1:D2}", sec / 60, sec % 60);
        }

        public bool SpendEnergy(int cost)
        {
            if (SaveSystem.data.energy < cost) return false;
            SaveSystem.data.energy -= cost;
            SaveSystem.Save();
            onChanged.Invoke();
            return true;
        }

        public bool SpendCoins(int cost)
        {
            if (SaveSystem.data.coins < cost) return false;
            SaveSystem.data.coins -= cost;
            SaveSystem.Save();
            onChanged.Invoke();
            return true;
        }

        public bool SpendFish(int cost)
        {
            if (SaveSystem.data.fish < cost) return false;
            SaveSystem.data.fish -= cost;
            SaveSystem.Save();
            onChanged.Invoke();
            return true;
        }

        public void AddCoins(int c) { SaveSystem.data.coins += c; SaveSystem.Save(); onChanged.Invoke(); }
        public void AddFish(int c) { SaveSystem.data.fish += c; SaveSystem.Save(); onChanged.Invoke(); }
        public void AddFragments(int c) { SaveSystem.data.fragments += c; SaveSystem.Save(); onChanged.Invoke(); }
        public void AddStars(int s) { SaveSystem.data.stars += s; SaveSystem.Save(); onChanged.Invoke(); }

        public void AddEnergy(int c)
        {
            SaveSystem.data.energy = Mathf.Min(EnergyMax, SaveSystem.data.energy + c);
            SaveSystem.data.lastRegenTicks = DateTime.UtcNow.Ticks;
            SaveSystem.Save();
            onChanged.Invoke();
        }
    }
}
