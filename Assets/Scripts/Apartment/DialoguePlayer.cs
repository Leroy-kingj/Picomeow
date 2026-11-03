using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Picomeow.Config;

namespace Picomeow.Apartment
{
    // 对话驱动装修：按数据播放对话节点，玩家选择如何装修（匹配原型 "Momo's Room" 视觉）
    public class DialoguePlayer : MonoBehaviour
    {
        DialogueData data;
        Dictionary<string, DialogueNode> nodeMap = new Dictionary<string, DialogueNode>();
        System.Action<string> onChoice;
        System.Action onFinish;

        Transform choicesRoot;
        Text bubbleText;

        public void Play(Transform panel, DialogueData d, System.Action<string> onChoiceCb, System.Action onFinishCb)
        {
            this.data = d;
            this.onChoice = onChoiceCb;
            this.onFinish = onFinishCb;
            nodeMap.Clear();
            if (d != null && d.nodes != null)
                foreach (var nd in d.nodes) nodeMap[nd.id] = nd;

            BuildUI(panel);
            if (d != null && d.nodes != null && d.nodes.Length > 0)
                ShowNode(d.nodes[0]);
            else
                Finish();
        }

        void BuildUI(Transform panel)
        {
            // 房间背景（Momo's Room 风格）
            var bg = UIHelpers.Fill(panel, "RoomBG", Color.white);
            var bgImg = bg.gameObject.GetComponent<Image>();
            bgImg.sprite = UIHelpers.LoadSprite("momo_room");
            bgImg.preserveAspect = false;

            // 顶部缎带标题：主角猫 + "'s Room"
            string roomTitle = GameConfig.HeroName() + "'s Room";
            UIHelpers.RibbonBanner(bg, 60, 1700, 960, 120, roomTitle, UIHelpers.Amber, UIHelpers.Paper);

            // 关闭按钮（放弃装修）
            UIHelpers.CloseButton(bg, 60, 1560, 90, () => Finish());

            // 猫咪角色
            var cat = UIHelpers.EL(bg, "HeroCat", 620, 760, 360, 360);
            var catImg = UIHelpers.AddImage(cat, UIHelpers.LoadSprite("momo_cat"), Color.white);
            catImg.preserveAspect = true;

            // 对话气泡（奶油底）
            var bubble = UIHelpers.EL(bg, "Bubble", 80, 980, 920, 420);
            Image bubbleImg = bubble.gameObject.AddComponent<Image>();
            bubbleImg.color = UIHelpers.Cream;
            var bt = UIHelpers.EL(bubble, "BubbleText", 40, 40, 840, 340);
            bubbleText = UIHelpers.AddLabel(bt, "", 40, UIHelpers.Ink, TextAnchor.MiddleCenter);

            // 选项容器
            choicesRoot = UIHelpers.EL(bg, "Choices", 80, 360, 920, 560);
        }

        void ShowNode(DialogueNode node)
        {
            if (bubbleText != null) bubbleText.text = node.text;

            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(choicesRoot.GetChild(i).gameObject);

            if (node.choices != null && node.choices.Length > 0)
            {
                float bh = Mathf.Min(220, 520f / node.choices.Length);
                float gap = 20;
                float totalH = node.choices.Length * bh + (node.choices.Length - 1) * gap;
                float startY = (560 - totalH) / 2f;
                for (int i = 0; i < node.choices.Length; i++)
                {
                    var ch = node.choices[i];
                    var rt = UIHelpers.EL(choicesRoot, "Choice_" + i, 20, startY + i * (bh + gap), 880, bh);
                    var btn = UIHelpers.AddButton(rt, UIHelpers.Teal, ch.text, 34);
                    DialogueChoice captured = ch;
                    btn.onClick.AddListener(() => OnChoice(captured));
                }
            }
            else
            {
                // 末尾节点：继续按钮（>>）
                var rt = UIHelpers.EL(choicesRoot, "Done", 620, 170, 360, 220);
                var btn = UIHelpers.AddButton(rt, UIHelpers.Amber, ">>", 64);
                btn.onClick.AddListener(() => Finish());
            }
        }

        void OnChoice(DialogueChoice ch)
        {
            if (onChoice != null) onChoice(ch.furnitureId);
            if (!string.IsNullOrEmpty(ch.next) && nodeMap.ContainsKey(ch.next))
                ShowNode(nodeMap[ch.next]);
            else
                Finish();
        }

        void Finish()
        {
            if (onFinish != null) onFinish();
        }
    }
}
