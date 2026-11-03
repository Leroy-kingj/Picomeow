using UnityEngine;

namespace Picomeow.UI
{
    // UI 辅助：资源加载 + 色彩 + 布局小工具（以 1080x1920 为参考分辨率，原点在左下角）
    public static class UIHelpers
    {
        // 从 Resources/Art 加载 PNG 并在运行时创建 Sprite
        public static Sprite LoadSprite(string name)
        {
            Texture2D tex = Resources.Load<Texture2D>("Art/" + name);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // 调色板（与策划素材风格一致：奶油 / 鼠尾草绿 / 暖黄 / 青绿 / 墨色）
        public static readonly Color Cream = new Color(0.98f, 0.96f, 0.90f);
        public static readonly Color Sage = new Color(0.62f, 0.78f, 0.66f);
        public static readonly Color WarmYellow = new Color(0.98f, 0.84f, 0.46f);
        public static readonly Color Teal = new Color(0.36f, 0.70f, 0.70f);
        public static readonly Color Ink = new Color(0.25f, 0.22f, 0.20f);
        public static readonly Color Paper = new Color(1f, 1f, 1f);
        public static readonly Color Panel = new Color(0.20f, 0.18f, 0.16f, 0.55f);

        // 创建一个填充父级的面板（Image）
        public static RectTransform Fill(Transform parent, string name, Color color)
        {
            GameObject g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            RectTransform rt = g.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = g.AddComponent<Image>();
            img.color = color;
            return rt;
        }

        // 创建一个以左下角为原点、给定 (x,y) 与尺寸的元件
        public static RectTransform EL(Transform parent, string name, float x, float y, float w, float h)
        {
            GameObject g = new GameObject(name, typeof(RectTransform));
            g.transform.SetParent(parent, false);
            RectTransform rt = g.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x + w / 2f, y + h / 2f);
            return rt;
        }

        public static Image AddImage(RectTransform rt, Sprite sprite, Color color)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            if (sprite != null) img.sprite = sprite;
            img.color = color;
            return img;
        }

        public static Text AddLabel(RectTransform rt, string text, int fontSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            Text t = rt.gameObject.AddComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false; // 文字不拦截按钮点击
            return t;
        }

        public static Button AddButton(RectTransform rt, Color color, string label, int fontSize)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            Button btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors;
            cb.normalColor = color;
            cb.highlightedColor = color * 1.1f;
            cb.pressedColor = color * 0.9f;
            btn.colors = cb;
            if (!string.IsNullOrEmpty(label))
            {
                var lt = UIHelpers.EL(rt, "Label", 0, 0, rt.sizeDelta.x, rt.sizeDelta.y);
                AddLabel(lt, label, fontSize, Ink);
            }
            return btn;
        }
    }
}
