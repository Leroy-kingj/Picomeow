using UnityEngine;
using UnityEngine.UI;

namespace Picomeow.UI
{
    // UI 辅助：资源加载 + 原型配色 + 布局小工具（以 1080x1920 为参考分辨率，原点在左下角）
    public static class UIHelpers
    {
        // 从 Resources/Art 加载 PNG 并在运行时创建 Sprite
        public static Sprite LoadSprite(string name)
        {
            Texture2D tex = Resources.Load<Texture2D>("Art/" + name);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // 调色板（与原型视觉稿一致：奶油底 / 墨色字 / 橙黄强调 / 青绿按钮 / 红缎带 / 黑圆 X）
        public static readonly Color Cream      = new Color(0.98f, 0.96f, 0.90f); // 主背景奶油
        public static readonly Color Paper      = new Color(1.00f, 1.00f, 1.00f); // 卡片白
        public static readonly Color Peach      = new Color(0.99f, 0.93f, 0.86f); // 设置卡/浅桃
        public static readonly Color Ink        = new Color(0.27f, 0.21f, 0.17f); // 墨褐字
        public static readonly Color Amber      = new Color(0.94f, 0.70f, 0.30f); // 橙黄 CTA
        public static readonly Color AmberDk    = new Color(0.85f, 0.55f, 0.18f); // 橙黄深（字）
        public static readonly Color Teal       = new Color(0.23f, 0.70f, 0.66f); // 青绿按钮
        public static readonly Color TealDk     = new Color(0.18f, 0.58f, 0.54f);
        public static readonly Color Sage       = new Color(0.62f, 0.78f, 0.66f); // 鼠尾草（旧）
        public static readonly Color Red        = new Color(0.91f, 0.33f, 0.27f); // 红缎带/角标
        public static readonly Color BoardBg    = new Color(1.00f, 0.95f, 0.84f); // 数织面板浅黄
        public static readonly Color FooterBg   = new Color(0.79f, 0.91f, 0.82f); // 数织底栏浅绿
        public static readonly Color Panel      = new Color(0.18f, 0.16f, 0.14f, 0.55f); // 半透明遮罩
        public static readonly Color GreenOn    = new Color(0.30f, 0.72f, 0.45f); // 开关 ON 绿

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
            cb.highlightedColor = color * 1.08f;
            cb.pressedColor = color * 0.9f;
            btn.colors = cb;
            if (!string.IsNullOrEmpty(label))
            {
                var lt = UIHelpers.EL(rt, "Label", 0, 0, rt.sizeDelta.x, rt.sizeDelta.y);
                AddLabel(lt, label, fontSize, Ink);
            }
            return btn;
        }

        // 缎带标题 banner（原型的橙黄/红缎带 + 折角效果，文字居中）
        public static RectTransform RibbonBanner(Transform parent, float x, float y, float w, float h, string text, Color ribbon, Color txtCol)
        {
            var root = EL(parent, "Ribbon", x, y, w, h);
            // 左右折角
            var lEnd = EL(root, "L", -22, 0, 22, h * 0.78f);
            AddImage(lEnd, null, ribbon * 0.8f);
            var rEnd = EL(root, "R", w, 0, 22, h * 0.78f);
            AddImage(rEnd, null, ribbon * 0.8f);
            // 主体
            var body = EL(root, "Body", 0, 0, w, h);
            AddImage(body, null, ribbon);
            // 文字
            var lt = EL(root, "Txt", 0, 0, w, h);
            AddLabel(lt, text, (int)(h * 0.55f), txtCol);
            return root;
        }

        // 黑圆 X 关闭按钮
        public static Button CloseButton(Transform parent, float x, float y, float d, UnityEngine.Events.UnityAction onClick)
        {
            var rt = EL(parent, "Close", x, y, d, d);
            var img = rt.gameObject.AddComponent<Image>();
            Sprite circle = LoadSprite("ui_circle");
            if (circle != null) img.sprite = circle;
            img.color = Ink;
            img.type = Image.Type.Simple;
            var xt = EL(rt, "X", 0, 0, d, d);
            AddLabel(xt, "X", (int)(d * 0.55f), Paper);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock cb = btn.colors; cb.normalColor = Ink; cb.highlightedColor = Ink * 1.1f; cb.pressedColor = Ink * 0.85f;
            btn.colors = cb;
            if (onClick != null) btn.onClick.AddListener(onClick);
            return btn;
        }

        // 圆形角标（红点 + 数字）
        public static RectTransform Badge(Transform parent, float x, float y, float d, string text)
        {
            var rt = EL(parent, "Badge", x, y, d, d);
            AddImage(rt, null, Red);
            var t = EL(rt, "T", 0, 0, d, d);
            AddLabel(t, text, (int)(d * 0.5f), Paper);
            return rt;
        }

        // 通用模态弹窗：半透明遮罩 dim + 白卡 + 缎带标题 + 黑圆 X；点击遮罩或 X 关闭。
        // 返回 modal 的 RectTransform，调用方往其内添加内容。modal 作为 dim 的子物体，关闭时一并销毁。
        public static RectTransform BuildModal(Transform popupRoot, string title, Color ribbonCol, float w = 860, float h = 1360, float y = 280)
        {
            var dim = Fill(popupRoot, "Dim", Panel);
            var modal = EL(dim.transform, title.Replace(" ", "") + "Modal", (1080 - w) / 2f, y, w, h);
            AddImage(modal, null, Paper);
            RibbonBanner(modal, (w - 540) / 2f, h - 150, 540, 120, title, ribbonCol, Paper);
            CloseButton(modal, w - 110, h - 120, 80, () => Object.Destroy(dim.gameObject));
            var db = dim.gameObject.AddComponent<Button>();
            db.onClick.AddListener(() => Object.Destroy(dim.gameObject));
            return modal;
        }
    }
}
