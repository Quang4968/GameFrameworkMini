#if MB_DEBUG_FPS
using UnityEngine;
using UnityEngine.Profiling;

namespace Game.Game.Views.Diagnostics
{
    // On-screen frame counter. The whole file compiles away unless MB_DEBUG_FPS
    // is set in Player Settings > Scripting Define Symbols.
    public sealed class FpsOverlay : MonoBehaviour
    {
        const float RefreshInterval = 0.5f;
        const float SmoothFps = 55f;
        const float RoughFps = 40f;
        const int ReferenceScreenHeight = 1920;
        const float Margin = 12f;

        static readonly Color SmoothColor = new Color(0.42f, 0.93f, 0.42f);
        static readonly Color RoughColor = new Color(1f, 0.83f, 0.29f);
        static readonly Color StutterColor = new Color(1f, 0.36f, 0.36f);

        GUIStyle _style;
        GUIContent _content;
        int _frames;
        float _elapsed;
        float _worstDelta;
        Color _color = SmoothColor;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // HideFlags.DontSave would keep the host alive across a Play Mode exit in the Editor,
            // so every session used to stack one more overlay on top of the last.
            if (FindAnyObjectByType<FpsOverlay>() != null) return;

            var host = new GameObject(nameof(FpsOverlay));
            host.AddComponent<FpsOverlay>();
            DontDestroyOnLoad(host);
        }

        void Awake()
        {
            _content = new GUIContent("--");
        }

        void Update()
        {
            // Unscaled: the time-scale toolbar must not skew the reading.
            var delta = Time.unscaledDeltaTime;
            _frames++;
            _elapsed += delta;
            if (delta > _worstDelta)
            {
                _worstDelta = delta;
            }

            if (_elapsed < RefreshInterval)
            {
                return;
            }

            Refresh();
            _frames = 0;
            _elapsed = 0f;
            _worstDelta = 0f;
        }

        void OnGUI()
        {
            if (_style == null)
            {
                _style = BuildStyle();
            }

            _style.normal.textColor = _color;
            var size = _style.CalcSize(_content);
            var safeArea = Screen.safeArea;
            // safeArea is bottom-left based, GUI space is top-left based.
            var x = safeArea.xMin + Margin;
            var y = Screen.height - safeArea.yMax + Margin;

            var box = new Rect(x, y, size.x + Margin, size.y + Margin * 0.5f);
            var backdrop = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = backdrop;
            GUI.Label(box, _content, _style);
        }

        void Refresh()
        {
            var average = _frames / _elapsed;
            var worst = _worstDelta > 0f ? 1f / _worstDelta : average;
            var megabytes = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);

            _content.text = $"{average:0} FPS  ·  {_elapsed / _frames * 1000f:0.0} ms\nmin {worst:0}  ·  {megabytes:0} MB";
            _color = average >= SmoothFps ? SmoothColor : average >= RoughFps ? RoughColor : StutterColor;
        }

        static GUIStyle BuildStyle()
        {
            return new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(6, 6, 3, 3),
                fontSize = Mathf.Max(14, Mathf.RoundToInt(28f * Screen.height / ReferenceScreenHeight)),
            };
        }
    }
}
#endif
