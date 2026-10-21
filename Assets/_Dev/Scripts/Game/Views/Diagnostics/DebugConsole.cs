#if MB_DEBUG_FPS
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk;
using Game.Core.Domain;
using Game.Core.Services;
using Game.Game.SceneFlow;
using Game.Game.Services;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

namespace Game.Game.Views.Diagnostics
{
    // Tester cheat panel: tops up coins and jumps to any level. Rides the same define as
    // FpsOverlay, so the whole file compiles away unless MB_DEBUG_FPS is set in Player
    // Settings > Scripting Define Symbols.
    //
    // No [Inject]: the panel outlives every scene, so it pulls the services out of whichever
    // LifetimeScope the current scene built and drops them again on the next scene load.
    public sealed class DebugConsole : MonoBehaviour
    {
        const int ReferenceScreenHeight = 1920;
        const float Margin = 12f;
        const int CoinCap = 999999;
        const int SmallCoinGrant = 1000;
        const int LargeCoinGrant = 10000;
        const int MaxLevelShow = 99999;

        static readonly int[] LevelSteps = { -100, -10, -1, 1, 10, 100 };
        static readonly Color PanelColor = new Color(0f, 0f, 0f, 0.92f);
        static readonly Color AccentColor = new Color(1f, 0.83f, 0.29f);

        ICoinService _coins;
        ILevelProgressService _levelProgress;
        ISceneLoader _sceneLoader;
        I_LevelSessionService _session;
        IRemoteConfigService _remoteConfig;
        GameRemoteConfig _gameRemoteConfig;

        GUIStyle _labelStyle;
        GUIStyle _buttonStyle;
        GUIStyle _fieldStyle;
        GUIStyle _statusStyle;

        Vector2 _remoteConfigScroll;

        bool _isOpen;
        bool _levelSynced;
        string _levelInput = "1";
        string _status = string.Empty;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            // HideFlags.DontSave would keep the host alive across a Play Mode exit in the Editor,
            // so every session used to stack one more panel on top of the last.
            if (FindAnyObjectByType<DebugConsole>() != null) return;

            var host = new GameObject(nameof(DebugConsole));
            host.AddComponent<DebugConsole>();
            DontDestroyOnLoad(host);
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Update()
        {
            // The container is built after this object installs, and again after every scene
            // change, so keep retrying until the scene has one.
            if (_isOpen && _coins == null) Bind();
        }

        void OnGUI()
        {
            EnsureStyles();

            var unit = Mathf.Max(30f, 58f * Screen.height / ReferenceScreenHeight);
            var safeArea = Screen.safeArea;
            // safeArea is bottom-left based, GUI space is top-left based.
            var top = Screen.height - safeArea.yMax + Margin;
            var toggleWidth = unit * 2.4f;
            var toggleRect = new Rect(safeArea.xMax - Margin - toggleWidth, top, toggleWidth, unit);

            if (GUI.Button(toggleRect, _isOpen ? "X" : "DEV", _buttonStyle))
            {
                Toggle();
            }

            if (!_isOpen) return;

            var width = Mathf.Min(safeArea.width - Margin * 2f, unit * 9.5f);
            var height = unit * 14f;
            var panel = new Rect(safeArea.xMax - Margin - width, toggleRect.yMax + Margin * 0.5f, width, height);

            var backdrop = GUI.color;
            GUI.color = PanelColor;
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = backdrop;

            GUILayout.BeginArea(new Rect(panel.x + Margin, panel.y + Margin, panel.width - Margin * 2f, panel.height - Margin * 2f));
            DrawCoinSection(unit);
            DrawLevelSection(unit);
            DrawRemoteConfigSection(unit);
            GUILayout.Label(_status, _statusStyle);
            GUILayout.EndArea();
        }

        void DrawCoinSection(float unit)
        {
            GUILayout.Label(_coins != null ? $"COIN  {_coins.Coins:N0}" : "COIN  --", _labelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+1K", _buttonStyle, GUILayout.Height(unit))) GrantCoins(SmallCoinGrant);
            if (GUILayout.Button("+10K", _buttonStyle, GUILayout.Height(unit))) GrantCoins(LargeCoinGrant);
            if (GUILayout.Button("MAX", _buttonStyle, GUILayout.Height(unit))) GrantCoins(CoinCap);
            GUILayout.EndHorizontal();
        }

        void DrawLevelSection(float unit)
        {
            GUILayout.Space(unit * 0.3f);
            GUILayout.Label("LEVEL", _labelStyle);

            _levelInput = GUILayout.TextField(_levelInput, 5, _fieldStyle, GUILayout.Height(unit));

            GUILayout.BeginHorizontal();
            foreach (var step in LevelSteps)
            {
                if (GUILayout.Button(step > 0 ? $"+{step}" : step.ToString(), _buttonStyle, GUILayout.Height(unit)))
                {
                    StepLevel(step);
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button($"PLAY LEVEL {ParseLevel()}", _buttonStyle, GUILayout.Height(unit)))
            {
                JumpToLevel();
            }
        }

        void DrawRemoteConfigSection(float unit)
        {
            GUILayout.Space(unit * 0.3f);
            GUILayout.Label("REMOTE CONFIG", _labelStyle);

            if (_remoteConfig == null)
            {
                GUILayout.Label("Waiting for scene services...", _statusStyle);
                return;
            }

            var status = _remoteConfig.FetchStatus.ToString().ToUpperInvariant();
            GUILayout.Label($"Status: {status}", _statusStyle);

            if (_remoteConfig.FetchStatus == RemoteConfigFetchStatus.Failed)
            {
                GUILayout.Label($"Error: {_remoteConfig.FetchError}", _statusStyle);
                return;
            }

            var values = _remoteConfig.ActivatedValues;
            GUILayout.Label(values.Count > 0 ? $"Fetched values ({values.Count})" : "Fetched values: none", _statusStyle);
            if (values.Count == 0) return;

            _remoteConfigScroll = GUILayout.BeginScrollView(_remoteConfigScroll, GUILayout.Height(unit * 4f));
            for (var i = 0; i < values.Count; i++)
            {
                var entry = values[i];
                GUILayout.Label($"{entry.Key} = {entry.Value}", _statusStyle);
            }
            GUILayout.EndScrollView();
        }

        void Toggle()
        {
            _isOpen = !_isOpen;
            _status = string.Empty;
            if (_isOpen) Bind();
        }

        void GrantCoins(int amount)
        {
            if (_coins == null)
            {
                _status = "No container in this scene.";
                return;
            }

            // Add() alone would overflow once the wallet is already near the cap.
            var granted = Mathf.Clamp(CoinCap - _coins.Coins, 0, amount);
            _coins.Add(granted);
            _status = granted > 0 ? $"+{granted:N0} coin" : "Wallet already capped.";
        }

        void StepLevel(int step)
        {
            _levelInput = Mathf.Clamp(ParseLevel() + step, 1, MaxLevelShow + 1).ToString();
        }

        void JumpToLevel()
        {
            if (_levelProgress == null)
            {
                _status = "No container in this scene.";
                return;
            }

            var level = ParseLevel();
            _levelProgress.SetLevelShow(level - 1);
            // Without this the next session would inherit the play_type of whatever screen the
            // cheat was fired from.
            _session?.SetNextPlayType(E_LevelPlayType.Home);
            _isOpen = false;

            if (_sceneLoader != null) _sceneLoader.LoadSceneAsync(GameScenes.MainGame).Forget();
            else SceneManager.LoadScene(GameScenes.MainGame);
        }

        int ParseLevel()
        {
            return int.TryParse(_levelInput, out var level) ? Mathf.Clamp(level, 1, MaxLevelShow + 1) : 1;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _coins = null;
            _levelProgress = null;
            _sceneLoader = null;
            _session = null;
            _remoteConfig = null;
            _gameRemoteConfig = null;
            _levelSynced = false;
        }

        void Bind()
        {
            // Every scene builds its own scope, and MainGame nests a second one, so ask each of
            // them rather than assuming which one owns the service.
            var scopes = FindObjectsByType<LifetimeScope>(FindObjectsSortMode.None);
            foreach (var scope in scopes)
            {
                var container = scope.Container;
                if (container == null) continue;

                if (_coins == null) container.TryResolve(out _coins);
                if (_levelProgress == null) container.TryResolve(out _levelProgress);
                if (_sceneLoader == null) container.TryResolve(out _sceneLoader);
                if (_session == null) container.TryResolve(out _session);
                if (_remoteConfig == null) container.TryResolve(out _remoteConfig);
                if (_gameRemoteConfig == null) container.TryResolve(out _gameRemoteConfig);
            }

            if (_levelSynced || _levelProgress == null) return;

            // Player-facing levels count from 1, LevelShow from 0 — same convention as the level label.
            _levelInput = (_levelProgress.LevelShow + 1).ToString();
            _levelSynced = true;
        }

        void EnsureStyles()
        {
            if (_labelStyle != null) return;

            var fontSize = Mathf.Max(14, Mathf.RoundToInt(30f * Screen.height / ReferenceScreenHeight));

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = fontSize,
            };
            _labelStyle.normal.textColor = AccentColor;

            _buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold, fontSize = fontSize };
            _fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter };
            _statusStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(fontSize * 0.8f) };
        }
    }
}
#endif
