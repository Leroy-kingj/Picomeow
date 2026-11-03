using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Picomeow.Core
{
    // 运行时引导：不依赖任何预制体，所有 UI 由代码构建
    public class GameRoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            EnsureEventSystem();

            GameObject canvasGO = new GameObject("PicomeowCanvas");
            Object.DontDestroyOnLoad(canvasGO);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 1f; // 竖屏：以高度为基准

            canvasGO.AddComponent<GraphicRaycaster>();

            // 经济系统
            canvasGO.AddComponent<Economy>();

            // UI 管理器（构建全部界面）
            UIManager ui = canvasGO.AddComponent<UIManager>();
            ui.Build();
        }

        static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem");
            Object.DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }
    }
}
