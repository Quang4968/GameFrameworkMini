using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MoodBoost.Sdk.Ads.Providers
{
    // The placeholder the simulated network draws in place of a real ad, the way MAX's Editor stub does: the panel
    // appearing is the proof that a button actually reached the ads layer with the right format and ad unit, and its
    // buttons are the only way to exercise "the player closed it early" without a device build.
    //
    // Drawn with IMGUI on purpose: no prefab, no canvas, no EventSystem and no UI package reference, so it draws over
    // any scene - including one with no UI of its own.
    sealed class FakeAdScreen : MonoBehaviour
    {
        const float ReferenceHeight = 720f;

        static FakeAdScreen instance;

        AdFormat format;
        string adUnitId;
        string placement;
        float remaining;
        AdResult? outcome;
        bool showing;

        // Blocks until the developer picks an outcome; only Completed pays a reward out, exactly like a real network.
        public static UniTask<AdResult> ShowAsync(
            AdFormat format, string adUnitId, string placement, double watchSeconds, CancellationToken ct)
            => GetOrCreate().PresentAsync(format, adUnitId, placement, watchSeconds, ct);

        static FakeAdScreen GetOrCreate()
        {
            if (instance != null) return instance;

            var go = new GameObject("[Fake Ad]");
            DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.NotEditable;

            instance = go.AddComponent<FakeAdScreen>();
            return instance;
        }

        async UniTask<AdResult> PresentAsync(
            AdFormat adFormat, string unitId, string placementName, double watchSeconds, CancellationToken ct)
        {
            format = adFormat;
            adUnitId = unitId;
            placement = placementName;
            remaining = (float)watchSeconds;
            outcome = null;
            showing = true;

            try
            {
                await UniTask.WaitUntil(() => outcome.HasValue, PlayerLoopTiming.Update, ct);
                return outcome.Value;
            }
            finally
            {
                showing = false;
            }
        }

        void Update()
        {
            // Unscaled: a real ad takes over while the game sits at timeScale 0, and the countdown still runs.
            if (showing && remaining > 0f) remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime);
        }

        void OnGUI()
        {
            if (!showing) return;

            // Over anything else drawing with IMGUI, such as an FPS overlay.
            GUI.depth = int.MinValue;

            // Scaled off the SHORT side: sized off the height alone, the text overflows the card in portrait.
            var scale = Mathf.Min(Screen.width, Screen.height) / 480f;
            var width = Screen.width * 0.86f;
            var height = Mathf.Min(Screen.height * 0.62f, width * 1.5f);
            var card = new Rect(
                (Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

            Fill(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.01f, 0.01f, 0.02f, 1f));
            Fill(card, new Color(0.13f, 0.15f, 0.22f, 1f));

            DrawText(card, scale);
            DrawButtons(card, scale);
            ReadKeys();
        }

        void DrawText(Rect card, float scale)
        {
            var pad = 16f * scale;
            var line = 22f * scale;
            var cursor = new Rect(card.x + pad, card.y + pad, card.width - pad * 2f, line * 1.6f);

            GUI.Label(cursor, $"FAKE {format.ToString().ToUpperInvariant()} AD", Style((int)(26f * scale), TextAnchor.UpperCenter));

            cursor.y += cursor.height + line * 0.5f;
            cursor.height = line;

            var body = Style((int)(14f * scale), TextAnchor.UpperLeft);

            // A null placement is not a bug: the game's placement only reaches the network when
            // ads_forward_placement_to_network is on (AdPolicySettings.ForwardPlacementToNetwork).
            var shown = string.IsNullOrEmpty(placement) ? "(not forwarded to the network)" : placement;

            GUI.Label(cursor, $"placement:  {shown}", body);
            cursor.y += line;
            GUI.Label(cursor, $"ad unit:  {adUnitId}", body);
            cursor.y += line;
            GUI.Label(cursor, "network:  Fake - simulated, no real revenue", body);
            cursor.y += line;

            // Two different reasons to be here, and mistaking one for the other is how a build ships with no revenue.
#if UNITY_EDITOR
            GUI.Label(cursor, "reason:  Editor (MB_ADS_EDITOR_MEDIATION opts out)", body);
#else
            GUI.Label(cursor, "reason:  MB_ADS_FAKE is enabled in this build", body);
#endif
        }

        void DrawButtons(Rect card, float scale)
        {
            var pad = 16f * scale;
            var height = 40f * scale;
            var row = new Rect(card.x + pad, card.yMax - pad - height, card.width - pad * 2f, height);
            var label = ButtonStyle((int)(15f * scale));
            var counting = remaining > 0f;

            GUI.Label(
                new Rect(row.x, row.y - height, row.width, height),
                counting ? $"watching ... {Mathf.CeilToInt(remaining)}s" : "Enter = finish     Esc = close early",
                Style((int)(14f * scale), TextAnchor.MiddleCenter));

            if (format != AdFormat.Rewarded)
            {
                // One outcome only: an interstitial closed at any time still counts as shown.
                GUI.enabled = !counting;
                if (GUI.Button(row, "Close", label)) outcome = AdResult.Completed;
                GUI.enabled = true;
                return;
            }

            var gap = 8f * scale;
            var half = (row.width - gap) * 0.5f;

            GUI.enabled = !counting;
            if (GUI.Button(new Rect(row.x, row.y, half, height), "Claim reward", label)) outcome = AdResult.Completed;
            GUI.enabled = true;

            if (GUI.Button(new Rect(row.xMax - half, row.y, half, height), "Close, no reward", label))
                outcome = AdResult.Skipped;
        }

        void ReadKeys()
        {
            var pressed = Event.current;
            if (pressed.type != EventType.KeyDown) return;

            if (pressed.keyCode == KeyCode.Escape)
            {
                outcome = format == AdFormat.Rewarded ? AdResult.Skipped : AdResult.Completed;
                pressed.Use();
                return;
            }

            if (remaining <= 0f && (pressed.keyCode == KeyCode.Return || pressed.keyCode == KeyCode.KeypadEnter))
            {
                outcome = AdResult.Completed;
                pressed.Use();
            }
        }

        static void Fill(Rect rect, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        // Both styles have to be built inside OnGUI - GUI.skin is null outside it.
        static GUIStyle Style(int fontSize, TextAnchor alignment)
            => new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                alignment = alignment,
                normal = { textColor = Color.white },
                wordWrap = true,
            };

        static GUIStyle ButtonStyle(int fontSize)
            => new GUIStyle(GUI.skin.button) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter };
    }
}
