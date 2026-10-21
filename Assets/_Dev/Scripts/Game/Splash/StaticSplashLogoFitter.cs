using UnityEngine;
using UnityEngine.UI;

namespace Game.Game.Splash
{
   
    [RequireComponent(typeof(Image))]
    public sealed class StaticSplashLogoFitter : MonoBehaviour
    {
        Image image;
        RectTransform rect;
        Canvas canvas;
        Vector2 lastScreen;

        void Awake()
        {
            image = GetComponent<Image>();
            rect = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
        }

        void OnEnable()
        {
            lastScreen = Vector2.zero;
            Fit();
        }

        void Update()
        {
            Fit();
        }

        void Fit()
        {
            var sprite = image.sprite;
            if (sprite == null)
                return;

            var screen = new Vector2(Screen.width, Screen.height);
            if (screen == lastScreen)
                return;
            lastScreen = screen;

            // Kích thước pixel gốc của ảnh, đúng cái mà BitmapFactory trả về với inScaled=false.
            float width = sprite.rect.width;
            float height = sprite.rect.height;
            float aspect = width / height;

            // "Only scale down": chỉ co lại khi màn hình nhỏ hơn ảnh, không bao giờ phóng to.
            if (screen.x < width)
            {
                width = screen.x;
                height = screen.x / aspect;
            }

            if (screen.y < height)
            {
                width = screen.y * aspect;
                height = screen.y;
            }

            float scaleFactor = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            rect.sizeDelta = new Vector2(width, height) / scaleFactor;
        }
    }
}
