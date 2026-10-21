using System;
using Game.Core.Services;
using Game.Game.Services;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace Game.Editor.Windows
{
    // Jumps the player to a level and, in Play mode, reports a win or a loss through the
    // GameplayBridge so the whole framework flow (popups, coins, lives, tracking) runs without a
    // finished board.
    public sealed class OutcomeDebugWindow : EditorWindow
    {
        int jumpLevel = 1;

        [MenuItem("Game/Gameplay Debug")]
        public static void Open()
        {
            GetWindow<OutcomeDebugWindow>("Gameplay Debug").minSize = new Vector2(320f, 180f);
        }

        void OnEnable() => EditorApplication.update += Repaint;

        void OnDisable() => EditorApplication.update -= Repaint;

        void OnGUI()
        {
            DrawLevelJump();
            EditorGUILayout.Space();

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Forcing an outcome needs Play mode in the MainGame scene.", MessageType.Info);
                return;
            }

            var bridge = Resolve<GameplayBridge>();
            if (bridge == null)
            {
                EditorGUILayout.HelpBox("No GameplayBridge in the running scene - this is not MainGame.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Force outcome", EditorStyles.boldLabel);
            if (GUILayout.Button("Force LOSS")) bridge.ReportLost();
            if (GUILayout.Button("Force WIN")) bridge.ReportWon();

            EditorGUILayout.HelpBox(
                "A forced loss opens the Lose popup and costs the life; a forced win pays the coins and " +
                "advances the level, exactly like a real outcome.",
                MessageType.None);
        }

        void DrawLevelJump()
        {
            EditorGUILayout.LabelField("Level", EditorStyles.boldLabel);

            var progress = Resolve<ILevelProgressService>();
            int current = progress != null
                ? progress.LevelShow
                : PlayerPrefs.GetInt(PlayerPrefsLevelProgressService.PrefsKey, 0);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Current", $"Level {current + 1}");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                jumpLevel = Mathf.Max(1, EditorGUILayout.IntField("Jump to", jumpLevel));

                if (GUILayout.Button("Go", GUILayout.Width(60f)))
                {
                    if (progress != null) progress.SetLevelShow(jumpLevel - 1);
                    else
                    {
                        PlayerPrefs.SetInt(PlayerPrefsLevelProgressService.PrefsKey, jumpLevel - 1);
                        PlayerPrefs.Save();
                    }
                }
            }

            EditorGUILayout.LabelField("Re-enter the level (Replay, or Home then Play) to see the change.",
                EditorStyles.miniLabel);
        }

        static T Resolve<T>() where T : class
        {
            if (!Application.isPlaying) return null;

            foreach (var scope in FindObjectsByType<LifetimeScope>(FindObjectsSortMode.None))
            {
                if (scope.Container == null) continue;

                try
                {
                    return (T)scope.Container.Resolve(typeof(T), null);
                }
                catch (Exception)
                {
                }
            }

            return null;
        }
    }
}
