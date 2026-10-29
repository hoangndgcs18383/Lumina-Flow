using UnityEngine;

namespace LuminaFlow
{
    public sealed class SafeAreaLayout : MonoBehaviour
    {
        public RectTransform Frame;
        private Rect lastSafe;
        private Vector2 lastScreen;
        private void LateUpdate()
        {
            var safe = Screen.safeArea;
            var screen = new Vector2(Screen.width, Screen.height);
            if (safe == lastSafe && screen == lastScreen) return;
            lastSafe = safe;
            lastScreen = screen;
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(safe.xMin / Mathf.Max(1, Screen.width), safe.yMin / Mathf.Max(1, Screen.height));
            rect.anchorMax = new Vector2(safe.xMax / Mathf.Max(1, Screen.width), safe.yMax / Mathf.Max(1, Screen.height));
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            if (Frame != null) Frame.sizeDelta = new Vector2(Mathf.Min(1080, rect.rect.width), 0);
        }
    }
}
