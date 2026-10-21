using System;
using System.Globalization;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Game.Services;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace Game.Editor.Windows
{
    // Bàn điều khiển để test tay hệ tim: đọc trạng thái, đổi số tim/coin, và TUA GIỜ.
    //
    // Tua giờ là lý do chính cửa sổ này tồn tại — hồi 1 tim mất 30 phút thật, còn kiểm tra chống
    // vặn giờ thì phải đẩy đồng hồ tới rồi kéo lùi. Nó ghi vào SystemClockService.DebugOffset, thứ
    // chỉ được biên dịch trong Editor.
    //
    // Lúc CHƯA chạy game thì làm việc thẳng trên PlayerPrefs. Lúc ĐANG chạy thì đi qua ILifeService
    // của scene, vì service giữ trạng thái trong RAM và chỉ ghi đĩa khi số tim đổi — sửa PlayerPrefs
    // dưới chân nó sẽ bị nó ghi đè.
    public sealed class LifeDebugWindow : EditorWindow
    {
        const string LivesKey = "Game.Lives";
        const string RemainingKey = "Game.LivesRefillRemainingTicks";
        const string AnchorKey = "Game.LivesClockAnchorUtcTicks";
        const string CoinsKey = "Game.Coins";
        const string UnlimitedEndKey = "Game.UnlimitedLifeEndUtcTicks";

        int coinsToSet = 1000;

        [MenuItem("Game/Life Debug")]
        public static void Open()
        {
            GetWindow<LifeDebugWindow>("Life Debug").minSize = new Vector2(320f, 420f);
        }

        // Đếm ngược phải nhúc nhích trong cửa sổ, không đợi người dùng rê chuột mới vẽ lại.
        void OnEnable() => EditorApplication.update += Repaint;

        void OnDisable() => EditorApplication.update -= Repaint;

        static ILifeService RuntimeLives()
        {
            if (!Application.isPlaying) return null;

            foreach (var scope in FindObjectsByType<LifetimeScope>(FindObjectsSortMode.None))
            {
                if (scope.Container == null) continue;

                try
                {
                    return (ILifeService)scope.Container.Resolve(typeof(ILifeService), null);
                }
                catch (Exception)
                {
                    // Scope của scene này không đăng ký tim (Boot, Splash) — thử scope kế tiếp.
                }
            }

            return null;
        }

        void OnGUI()
        {
            var lives = RuntimeLives();

            DrawState(lives);
            EditorGUILayout.Space();
            DrawLives(lives);
            EditorGUILayout.Space();
            DrawUnlimited(lives);
            EditorGUILayout.Space();
            DrawTimeTravel();
            EditorGUILayout.Space();
            DrawCoins();
            EditorGUILayout.Space();
            DrawReset(lives);
        }

        void DrawState(ILifeService lives)
        {
            EditorGUILayout.LabelField("Trạng thái", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                if (lives != null)
                {
                    EditorGUILayout.TextField("Nguồn", "ILifeService (đang chạy)");
                    EditorGUILayout.TextField("Tim", $"{lives.Lives}/{lives.MaxLives}");
                    EditorGUILayout.TextField("Tim kế tiếp sau", lives.Lives >= lives.MaxLives
                        ? "— (đầy tim)"
                        : $"{(int)lives.TimeUntilNextLife.TotalMinutes:00}:{lives.TimeUntilNextLife.Seconds:00}");
                }
                else
                {
                    EditorGUILayout.TextField("Nguồn", "PlayerPrefs (chưa chạy game)");
                    EditorGUILayout.TextField("Tim", PlayerPrefs.GetInt(LivesKey, 0).ToString());
                    EditorGUILayout.TextField("Tim kế tiếp sau", FormatTicks(RemainingKey));
                }

                var unlimited = lives != null
                    ? lives.UnlimitedTimeLeft
                    : UnlimitedLifeRules.TimeLeft(ReadUnlimitedEnd(), GameNow());
                EditorGUILayout.TextField("Tim vô hạn còn",
                    unlimited > TimeSpan.Zero ? unlimited.ToString(@"h\:mm\:ss") : "—");

                EditorGUILayout.TextField("Mốc chống vặn giờ", ReadAnchor());
                EditorGUILayout.TextField("Coin", PlayerPrefs.GetInt(CoinsKey, 0).ToString());
                EditorGUILayout.TextField("Giờ game đang thấy", GameNow().ToString("HH:mm:ss") + " UTC");
            }
        }

        void DrawLives(ILifeService lives)
        {
            EditorGUILayout.LabelField("Tim", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("−1")) ChangeLives(lives, -1);
                if (GUILayout.Button("+1")) ChangeLives(lives, 1);
                if (GUILayout.Button("Về 0")) SetLives(lives, 0);
                if (GUILayout.Button("Đầy")) SetLives(lives, lives != null ? lives.MaxLives : 5);
            }
        }

        void DrawUnlimited(ILifeService lives)
        {
            EditorGUILayout.LabelField("Tim vô hạn", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+15 phút")) AddUnlimited(lives, TimeSpan.FromMinutes(15));
                if (GUILayout.Button("+1 giờ")) AddUnlimited(lives, TimeSpan.FromHours(1));
            }
        }

        void DrawTimeTravel()
        {
            EditorGUILayout.LabelField("Tua giờ (chỉ trong Editor)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Lệch hiện tại: {SystemClockService.DebugOffset}", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+1 phút")) Shift(TimeSpan.FromMinutes(1));
                if (GUILayout.Button("+10 phút")) Shift(TimeSpan.FromMinutes(10));
                if (GUILayout.Button("+30 phút")) Shift(TimeSpan.FromMinutes(30));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("+3 giờ")) Shift(TimeSpan.FromHours(3));
                if (GUILayout.Button("+1 ngày")) Shift(TimeSpan.FromDays(1));
                if (GUILayout.Button("−1 giờ")) Shift(TimeSpan.FromHours(-1));
            }

            // Đây chính là nửa sau của kịch bản gian lận: vặn tới lấy tim rồi chỉnh về giờ thật.
            if (GUILayout.Button("Về giờ thật (offset = 0)")) SystemClockService.DebugOffset = TimeSpan.Zero;
        }

        void DrawCoins()
        {
            EditorGUILayout.LabelField("Coin", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                coinsToSet = EditorGUILayout.IntField(coinsToSet);

                if (GUILayout.Button("Đặt"))
                {
                    var coins = RuntimeCoins();
                    if (coins != null)
                    {
                        // Không có Set nên đi bằng cộng/trừ, để sự kiện CoinsChanged vẫn bắn.
                        var delta = coinsToSet - coins.Coins;
                        if (delta > 0) coins.Add(delta);
                        else if (delta < 0) coins.TrySpend(-delta);
                    }
                    else
                    {
                        PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, coinsToSet));
                        PlayerPrefs.Save();
                    }
                }
            }
        }

        void DrawReset(ILifeService lives)
        {
            EditorGUILayout.LabelField("Dọn dẹp", EditorStyles.boldLabel);

            if (GUILayout.Button("Xoá sạch dữ liệu tim"))
            {
                PlayerPrefs.DeleteKey(LivesKey);
                PlayerPrefs.DeleteKey(RemainingKey);
                PlayerPrefs.DeleteKey(AnchorKey);
                PlayerPrefs.DeleteKey(UnlimitedEndKey);
                PlayerPrefs.Save();
                SystemClockService.DebugOffset = TimeSpan.Zero;

                if (lives != null)
                    EditorUtility.DisplayDialog("Life Debug",
                        "Đã xoá PlayerPrefs. Service đang chạy vẫn giữ số cũ trong RAM — thoát Play Mode rồi vào lại để thấy trạng thái máy mới cài.",
                        "OK");
            }
        }

        static ICoinService RuntimeCoins()
        {
            if (!Application.isPlaying) return null;

            foreach (var scope in FindObjectsByType<LifetimeScope>(FindObjectsSortMode.None))
            {
                if (scope.Container == null) continue;

                try
                {
                    return (ICoinService)scope.Container.Resolve(typeof(ICoinService), null);
                }
                catch (Exception)
                {
                }
            }

            return null;
        }

        static void Shift(TimeSpan delta)
        {
            SystemClockService.DebugOffset += delta;
        }

        static void ChangeLives(ILifeService lives, int delta)
        {
            if (lives != null)
            {
                if (delta > 0) lives.Add(delta);
                else lives.TrySpend(-delta);
                return;
            }

            var current = PlayerPrefs.GetInt(LivesKey, 0);
            PlayerPrefs.SetInt(LivesKey, Mathf.Max(0, current + delta));
            PlayerPrefs.Save();
        }

        static void SetLives(ILifeService lives, int value)
        {
            if (lives != null)
            {
                ChangeLives(lives, value - lives.Lives);
                return;
            }

            PlayerPrefs.SetInt(LivesKey, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }

        static void AddUnlimited(ILifeService lives, TimeSpan duration)
        {
            if (lives != null)
            {
                lives.AddUnlimited(duration);
                return;
            }

            var end = UnlimitedLifeRules.Extend(ReadUnlimitedEnd(), GameNow(), duration);
            PlayerPrefs.SetString(UnlimitedEndKey, end.Ticks.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();
        }

        static DateTime GameNow() => DateTime.UtcNow + SystemClockService.DebugOffset;

        static DateTime ReadUnlimitedEnd()
        {
            return long.TryParse(PlayerPrefs.GetString(UnlimitedEndKey, "0"), out var ticks) && ticks > 0
                ? new DateTime(ticks, DateTimeKind.Utc)
                : DateTime.MinValue;
        }

        static string FormatTicks(string key)
        {
            if (!long.TryParse(PlayerPrefs.GetString(key, "0"), out var ticks) || ticks <= 0) return "—";

            var span = TimeSpan.FromTicks(ticks);
            return $"{(int)span.TotalMinutes:00}:{span.Seconds:00}";
        }

        static string ReadAnchor()
        {
            if (!long.TryParse(PlayerPrefs.GetString(AnchorKey, "0"), out var ticks) || ticks <= 0) return "— (chưa có)";

            return new DateTime(ticks, DateTimeKind.Utc).ToString("HH:mm:ss") + " UTC";
        }
    }
}
