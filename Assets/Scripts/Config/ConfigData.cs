using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Picomeow.Config
{
    // ---------- 配置数据模型（与 config.json 一一对应） ----------
    [Serializable]
    public class PictureData
    {
        public int id;
        public int chapter;
        public int size;
        public string solution;   // 长度 size*size，'0'/'1'
        public string[] rowClues; // 每行 "3" 或 "2,1" 或 "0"
        public string[] colClues;
        public string palette;
        public int rewardCoins;
        public int rewardStars;
    }

    [Serializable]
    public class LevelData
    {
        public int id;
        public int pictureId;
        public int indexInPicture;
        public int initialMarks;
        public int maxMistakes;
        public int energyCost;
        public int coinReward;
        public int starReward;
        public bool decorationTrigger;
    }

    [Serializable]
    public class FurnitureData
    {
        public string id;
        public string name;
        public string icon;
        public int slot;
    }

    [Serializable]
    public class DialogueChoice
    {
        public string text;
        public string furnitureId;
        public string next; // 下一节点 id，"" 表示结束
    }

    [Serializable]
    public class DialogueNode
    {
        public string id;
        public string speaker;
        public string text;
        public DialogueChoice[] choices;
    }

    [Serializable]
    public class DialogueData
    {
        public string id;
        public int roomId;
        public string title;
        public DialogueNode[] nodes;
    }

    [Serializable]
    public class OnboardingStep
    {
        public int step;
        public string screen;
        public string instruction;
    }

    [Serializable]
    public class EconomyData
    {
        public int energyMax;
        public int energyRegenMinutes;
        public int startEnergy;
        public int startCoins;
        public int startStars;
    }

    [Serializable]
    public class HintItemData
    {
        public string id;
        public string name;
        public string desc;
        public int cost;
    }

    // 猫咪数据：名字严格按品种（英文名），北美市场友好
    [Serializable]
    public class CatData
    {
        public int id;
        public string name;        // 按品种命名，如 Calico / Bombay
        public string breed;       // 品种
        public string coat;        // 毛色描述
        public string desc;        // 档案描述
        public string rel;         // 好感度文案
        public string secret;      // 秘密文案
        public bool unlocked;      // 是否已入住（收集）
    }

    [Serializable]
    public class ConfigRoot
    {
        public int version;
        public PictureData[] pictures;
        public LevelData[] levels;
        public FurnitureData[] furniture;
        public DialogueData[] dialogue;
        public OnboardingStep[] onboarding;
        public EconomyData economy;
        public HintItemData[] hintItems;
        public CatData[] cats;
    }

    public static class GameConfig
    {
        public static ConfigRoot Data;
        public static bool Loaded { get { return Data != null; } }

        public static PictureData GetPicture(int id)
        {
            if (Data == null) return null;
            foreach (var p in Data.pictures) if (p.id == id) return p;
            return null;
        }

        public static LevelData GetLevel(int id)
        {
            if (Data == null) return null;
            foreach (var l in Data.levels) if (l.id == id) return l;
            return null;
        }

        public static DialogueData GetDialogueByRoom(int roomId)
        {
            if (Data == null) return null;
            foreach (var d in Data.dialogue) if (d.roomId == roomId) return d;
            return null;
        }

        // 主角猫（collection 中第一只已入住的猫），用于对话说话者名字
        public static CatData HeroCat()
        {
            if (Data == null || Data.cats == null) return null;
            foreach (var c in Data.cats) if (c.unlocked) return c;
            return Data.cats.Length > 0 ? Data.cats[0] : null;
        }

        public static string HeroName()
        {
            var h = HeroCat();
            return h != null ? h.name : "Cat";
        }
    }

    // ---------- 运行时加载（支持 Standalone / Android / iOS） ----------
    public static class ConfigLoader
    {
        public static void Load(MonoBehaviour host, Action onDone)
        {
            host.StartCoroutine(LoadCoroutine(onDone));
        }

        private static IEnumerator LoadCoroutine(Action onDone)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, "Config/config.json");
            string url = new System.Uri(path).AbsoluteUri;
            using (UnityWebRequest uwr = UnityWebRequest.Get(url))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[ConfigLoader] 加载失败: " + uwr.error + " url=" + url);
                    yield break;
                }
                try
                {
                    GameConfig.Data = JsonUtility.FromJson<ConfigRoot>(uwr.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogError("[ConfigLoader] 解析失败: " + e.Message);
                    yield break;
                }
                if (onDone != null) onCompleteSafe(onDone);
            }
        }

        private static void onCompleteSafe(Action cb)
        {
            // 延迟到下一帧，确保 ConfigRoot 完全就绪
            cb();
        }
    }
}
