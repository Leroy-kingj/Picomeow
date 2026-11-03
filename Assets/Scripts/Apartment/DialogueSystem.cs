using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Picomeow.Apartment
{
    // 对话驱动装修：按数据播放对话节点，玩家选择如何装修
    public class DialoguePlayer : MonoBehaviour
    {
        DialogueData data;
        Dictionary<string, DialogueNode> nodeMap = new Dictionary<string, DialogueNode>();
        System.Action<string> onChoice;
        System.Action onFinish;

        Transform cardRoot;

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
            // 半透明遮罩（点击不穿透）
            var bg = UIHelpers.Fill(panel, "Dbg", UIHelpers.Panel);

            // 卡片
            cardRoot = UIHelpers.EL(panel, "Card", 90, 300, 900, 1200);
            Image cardImg = cardRoot.gameObject.AddComponent<Image>();
            cardImg.color = UIHelpers.Paper;

            // 猫咪 Momo
            var momo = UIHelpers.EL(cardRoot, "Momo", 600, 340, 320, 320);
            var momoImg = UIHelpers.AddImage(momo, UIHelpers.LoadSprite("momo_cat"), Color.white);
            momoImg.preserveAspect = true;

            // 对话气泡
            var bubble = UIHelpers.EL(cardRoot, "Bubble", 60, 980, 780, 480);
            Image bubbleImg = bubble.gameObject.AddComponent<Image>();
            bubbleImg.color = UIHelpers.Cream;
            var bubbleText = UIHelpers.EL(bubble, "BubbleText", 30, 30, 720, 420);
            UIHelpers.AddLabel(bubbleText, "", 40, UIHelpers.Ink, TextAnchor.MiddleCenter);

            // 选项容器
            var choices = UIHelpers.EL(cardRoot, "Choices", 60, 380, 780, 560);
        }

        void ShowNode(DialogueNode node)
        {
            var bubbleText = cardRoot.Find("Bubble/BubbleText");
            if (bubbleText != null) bubbleText.GetComponent<Text>().text = node.text;

            var choicesRoot = cardRoot.Find("Choices");
            // 清空旧选项
            for (int i = choicesRoot.childCount - 1; i >= 0; i--)
                Object.Destroy(choicesRoot.GetChild(i).gameObject);

            if (node.choices != null && node.choices.Length > 0)
            {
                float bh = Mathf.Min(240, 520f / node.choices.Length);
                float gap = 20;
                float totalH = node.choices.Length * bh + (node.choices.Length - 1) * gap;
                float startY = (560 - totalH) / 2f;
                for (int i = 0; i < node.choices.Length; i++)
                {
                    var ch = node.choices[i];
                    var rt = UIHelpers.EL(choicesRoot, "Choice_" + i, 20, startY + i * (bh + gap), 740, bh);
                    var btn = UIHelpers.AddButton(rt, UIHelpers.Sage, ch.text, 36);
                    DialogueChoice captured = ch;
                    btn.onClick.AddListener(() => OnChoice(captured));
                }
            }
            else
            {
                var rt = UIHelpers.EL(choicesRoot, "Done", 20, 160, 740, 240);
                var btn = UIHelpers.AddButton(rt, UIHelpers.WarmYellow, "Done", 44);
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
