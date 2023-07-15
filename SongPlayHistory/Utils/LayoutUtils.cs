using UnityEngine;

namespace SongPlayHistory.Utils
{
    internal static class LayoutUtils
    {
        public static void MatchParent(this Transform transform)
        {
            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(1f, 1f);
        }

        public static void AlignBottom(this Transform transform, float height, float margin)
        {
            var rect = transform as RectTransform;
            if (rect == null)
            {
                return;
            }
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.anchoredPosition = new Vector2(0f, margin);
            rect.sizeDelta = new Vector2(0f, height);
        }
    }
}