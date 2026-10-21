using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Game.Views.Tutorial
{
    // Turns whatever a step points at — a UI button or a piece in the world — into ONE currency:
    // a rectangle in screen pixels. The hand and the spotlight then never need to know which kind
    // of target they were handed.
    public static class TutorialScreenRect
    {
        // Reused every frame: the focus rect is recomputed in Update.
        static readonly List<Graphic> GraphicBuffer = new();

        // Null for an Overlay canvas — passing a camera there lands the hand off screen.
        public static Camera CanvasCamera(Canvas canvas)
        {
            if (canvas == null) return null;

            var root = canvas.rootCanvas;
            return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
        }

        public static bool TryFromRectTransform(RectTransform target, Canvas canvas, out Rect screenRect)
        {
            screenRect = default;
            if (target == null) return false;

            var camera = CanvasCamera(canvas);
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);

            var min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            var max = min;

            for (int i = 1; i < corners.Length; i++)
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, corners[i]);
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            screenRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }

        // What the button DRAWS, not the rect it lays out in: every booster icon overflows its
        // button by ~50px upward, and a hole cut to the layout rect slices the top off the art.
        public static bool TryFromUiTarget(RectTransform target, Canvas canvas, out Rect screenRect)
        {
            if (!TryFromRectTransform(target, canvas, out screenRect)) return false;

            target.GetComponentsInChildren(false, GraphicBuffer);

            foreach (var graphic in GraphicBuffer)
            {
                if (!graphic.enabled) continue;
                if (!TryFromRectTransform(graphic.rectTransform, canvas, out var part)) continue;

                screenRect = Rect.MinMaxRect(
                    Mathf.Min(screenRect.xMin, part.xMin), Mathf.Min(screenRect.yMin, part.yMin),
                    Mathf.Max(screenRect.xMax, part.xMax), Mathf.Max(screenRect.yMax, part.yMax));
            }

            GraphicBuffer.Clear();
            return true;
        }

        // A world point has no size, so the caller says how wide a hole it deserves.
        public static bool TryFromWorldPoint(Vector3 worldPoint, Camera worldCamera, float screenRadius,
            out Rect screenRect)
        {
            screenRect = default;
            if (worldCamera == null) return false;

            var point = worldCamera.WorldToScreenPoint(worldPoint);

            // Behind the camera WorldToScreenPoint still returns a plausible point with negative
            // z, which would put the hand on the wrong side of the screen.
            if (point.z <= 0f) return false;

            screenRect = new Rect(point.x - screenRadius, point.y - screenRadius,
                screenRadius * 2f, screenRadius * 2f);
            return true;
        }

        // Screen rect -> local rect of container, ready to drive anchoredPosition on centred
        // children.
        public static bool TryToLocalRect(Rect screenRect, RectTransform container, Canvas canvas,
            out Rect localRect)
        {
            localRect = default;
            if (container == null) return false;

            var camera = CanvasCamera(canvas);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    container, screenRect.min, camera, out var min)) return false;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    container, screenRect.max, camera, out var max)) return false;

            localRect = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            return true;
        }
    }
}
