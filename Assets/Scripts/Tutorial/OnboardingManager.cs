using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Picomeow.Tutorial
{
    // 新手引导：按配置步骤展示提示，点击「Next」或遮罩推进
    public static class OnboardingManager
    {
        public static void Run(Transform panel, OnboardingStep[] steps, System.Action onDone)
        {
            if (steps == null || steps.Length == 0) { onDone?.Invoke(); return; }

            var bg = UIHelpers.Fill(panel, "TutorialBg", new Color(0.1f, 0.09f, 0.08f, 0.72f));
            var card = UIHelpers.EL(panel, "TutorialCard", 90, 760, 900, 400);
            Image ci = card.gameObject.AddComponent<Image>();
            ci.color = UIHelpers.Paper;
            var txt = UIHelpers.EL(card, "TutText", 50, 60, 800, 220);
            UIHelpers.AddLabel(txt, "", 40, UIHelpers.Ink, TextAnchor.MiddleCenter);
            var next = UIHelpers.EL(card, "NextBtn", 340, 20, 400, 110);
            var nb = UIHelpers.AddButton(next, UIHelpers.WarmYellow, "Next", 42);

            int index = 0;
            System.Action refresh = null;
            refresh = () =>
            {
                if (index >= steps.Length)
                {
                    panel.gameObject.SetActive(false);
                    onDone?.Invoke();
                    return;
                }
                txt.GetComponent<Text>().text = steps[index].instruction;
                nb.GetComponentInChildren<Text>().text = (index == steps.Length - 1) ? "Start!" : "Next";
            };

            nb.onClick.AddListener(() => { index++; refresh(); });
            bg.GetComponent<Button>(); // 遮罩本身不拦截，点击卡片外不推进（避免误操作）
            refresh();
        }
    }
}
