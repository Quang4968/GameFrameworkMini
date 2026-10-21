using System;
using MoodBoost.Sdk.Tracking;
using Game.Core.Services;
using Game.Game.Services;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace Game.Editor.Windows
{
    // Bàn điều khiển user property: xem CHÍNH XÁC những gì đã được đẩy xuống provider, và sửa tay
    // các counter có persist.
    //
    // Lý do chính nó tồn tại: Firebase Analytics không chạy được trong Editor trên macOS (thiếu
    // native lib), nên DebugView không giúp gì lúc phát triển. TrackingService giữ lại giá trị cuối
    // của từng key, và cửa sổ này đọc thẳng cache đó — kiểm được cả 21 property mà không cần build.
    //
    // Chưa chạy game thì làm việc thẳng trên PlayerPrefs; đang chạy thì đi qua service của scene,
    // vì service giữ trạng thái trong RAM và sẽ ghi đè PlayerPrefs bị sửa dưới chân nó.
    public sealed class UserPropertyDebugWindow : EditorWindow
    {
        Vector2 scroll;

        [MenuItem("Game/User Property Debug")]
        public static void Open()
        {
            GetWindow<UserPropertyDebugWindow>("User Property Debug").minSize = new Vector2(360f, 460f);
        }

        void OnEnable() => EditorApplication.update += Repaint;

        void OnDisable() => EditorApplication.update -= Repaint;

        void OnGUI()
        {
            using (var view = new EditorGUILayout.ScrollViewScope(scroll))
            {
                scroll = view.scrollPosition;

                DrawPublished();
                EditorGUILayout.Space();
                DrawStreaks();
                EditorGUILayout.Space();
                DrawActiveDay();
                EditorGUILayout.Space();
                DrawReset();
            }
        }

        void DrawPublished()
        {
            EditorGUILayout.LabelField("Đã bắn xuống provider", EditorStyles.boldLabel);

            var tracking = Resolve<ITrackingService>() as TrackingService;
            if (tracking == null)
            {
                EditorGUILayout.HelpBox(
                    "Chưa vào Play Mode, hoặc scene hiện tại chưa dựng SDK. Vào Play từ scene Boot để thấy danh sách.",
                    MessageType.Info);
                return;
            }

            if (tracking.UserProperties.Count == 0)
            {
                EditorGUILayout.HelpBox("Chưa có property nào.", MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                foreach (var property in tracking.UserProperties)
                {
                    EditorGUILayout.TextField(property.Key, property.Value);
                }
            }

            EditorGUILayout.LabelField($"{tracking.UserProperties.Count}/25 slot của Firebase", EditorStyles.miniLabel);
        }

        void DrawStreaks()
        {
            EditorGUILayout.LabelField("Chuỗi thắng/thua", EditorStyles.boldLabel);

            var streaks = Resolve<I_LevelStreakService>();

            using (new EditorGUI.DisabledScope(true))
            {
                if (streaks != null)
                {
                    EditorGUILayout.TextField("Nguồn", "I_LevelStreakService (đang chạy)");
                    EditorGUILayout.TextField("Thắng / Thua", $"{streaks.Streak.Win} / {streaks.Streak.Lose}");
                }
                else
                {
                    EditorGUILayout.TextField("Nguồn", "PlayerPrefs (chưa chạy game)");
                    EditorGUILayout.TextField("Thắng / Thua",
                        $"{PlayerPrefs.GetInt(PlayerPrefsLevelStreakService.WinPrefsKey, 0)} / " +
                        $"{PlayerPrefs.GetInt(PlayerPrefsLevelStreakService.LosePrefsKey, 0)}");
                }

                EditorGUILayout.TextField("Đang chơi dở một màn",
                    PlayerPrefs.GetInt(PlayerPrefsLevelStreakService.InProgressPrefsKey, 0) == 1 ? "có" : "không");
            }

            if (streaks == null) return;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Thắng")) streaks.RecordWin();
                if (GUILayout.Button("Thua")) streaks.RecordLoss();
                if (GUILayout.Button("Gỡ lần thua")) streaks.CancelLoss();
            }
        }

        void DrawActiveDay()
        {
            EditorGUILayout.LabelField("Ngày hoạt động", EditorStyles.boldLabel);

            var activeDay = Resolve<I_ActiveDayService>();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("active_day_n", activeDay != null
                    ? (activeDay.IsEstimated ? "~" : string.Empty) + activeDay.DayIndex
                    : PlayerPrefs.GetInt(PlayerPrefsActiveDayService.MaxSeenPrefsKey, 0).ToString());

                EditorGUILayout.TextField("Mốc cài", ReadFirstOpen());
                EditorGUILayout.TextField("Lệch múi giờ lúc cài",
                    PlayerPrefs.GetInt(PlayerPrefsActiveDayService.OffsetPrefsKey, 0) + " phút");
            }

            // Tua giờ dùng chung cửa trập với hệ tim, nên D+1 kiểm được ngay mà không phải đợi.
            EditorGUILayout.LabelField(
                $"Tua giờ ở Life Debug (lệch hiện tại {SystemClockService.DebugOffset})", EditorStyles.miniLabel);

            if (activeDay != null && GUILayout.Button("Tính lại")) activeDay.Refresh();
        }

        void DrawReset()
        {
            EditorGUILayout.LabelField("Dọn dẹp", EditorStyles.boldLabel);

            if (!GUILayout.Button("Xoá sạch dữ liệu user property")) return;

            PlayerPrefs.DeleteKey(PlayerPrefsLevelStreakService.WinPrefsKey);
            PlayerPrefs.DeleteKey(PlayerPrefsLevelStreakService.LosePrefsKey);
            PlayerPrefs.DeleteKey(PlayerPrefsLevelStreakService.InProgressPrefsKey);
            PlayerPrefs.DeleteKey(PlayerPrefsActiveDayService.FirstOpenPrefsKey);
            PlayerPrefs.DeleteKey(PlayerPrefsActiveDayService.OffsetPrefsKey);
            PlayerPrefs.DeleteKey(PlayerPrefsActiveDayService.MaxSeenPrefsKey);
            PlayerPrefs.DeleteKey(PlayerPrefsActiveDayService.EstimatedPrefsKey);
            PlayerPrefs.Save();

            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("User Property Debug",
                    "Đã xoá PlayerPrefs. Service đang chạy vẫn giữ số cũ trong RAM — thoát Play Mode rồi vào lại " +
                    "để thấy trạng thái máy mới cài.",
                    "OK");
            }
        }

        static string ReadFirstOpen()
        {
            var raw = PlayerPrefs.GetString(PlayerPrefsActiveDayService.FirstOpenPrefsKey, "0");
            if (!long.TryParse(raw, out var ticks) || ticks <= 0) return "— (chưa có)";

            return new DateTime(ticks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm") + " UTC";
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
                    // Scope của scene này không đăng ký thứ đang tìm — thử scope kế tiếp.
                }
            }

            return null;
        }
    }
}
