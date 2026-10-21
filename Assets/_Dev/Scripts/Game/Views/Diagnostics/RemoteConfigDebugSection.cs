#if MB_DEBUG_FPS
using MoodBoost.Sdk;
using UnityEngine;

namespace Game.Game.Views.Diagnostics
{
    // DebugConsole's remote config block: fetch status, then every activated key and value.
    sealed class RemoteConfigDebugSection
    {
        Vector2 scroll;

        public void Draw(IRemoteConfigService remoteConfig, float unit, GUIStyle labelStyle, GUIStyle statusStyle)
        {
            GUILayout.Space(unit * 0.3f);
            GUILayout.Label("REMOTE CONFIG", labelStyle);

            if (remoteConfig == null)
            {
                GUILayout.Label("Waiting for scene services...", statusStyle);
                return;
            }

            var status = remoteConfig.FetchStatus.ToString().ToUpperInvariant();
            GUILayout.Label($"Status: {status}", statusStyle);

            if (remoteConfig.FetchStatus == RemoteConfigFetchStatus.Failed)
            {
                GUILayout.Label($"Error: {remoteConfig.FetchError}", statusStyle);
                return;
            }

            var values = remoteConfig.ActivatedValues;
            GUILayout.Label(values.Count > 0 ? $"Fetched values ({values.Count})" : "Fetched values: none", statusStyle);
            if (values.Count == 0) return;

            scroll = GUILayout.BeginScrollView(scroll, GUILayout.Height(unit * 4f));
            for (var i = 0; i < values.Count; i++)
            {
                var entry = values[i];
                GUILayout.Label($"{entry.Key} = {entry.Value}", statusStyle);
            }
            GUILayout.EndScrollView();
        }
    }
}
#endif
