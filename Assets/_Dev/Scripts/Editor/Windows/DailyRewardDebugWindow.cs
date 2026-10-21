using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Domain.DailyReward;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Core.UseCases;
using Game.Game.Data;
using Game.Game.Services;
using Game.Game.Views.Popup;
using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Editor.Windows
{
    // Manual test bench for the daily reward: where the player stands in the cycle, jump to any day,
    // let a day pass, and check a remote config JSON before it ships.
    //
    // Two modes like LifeDebugWindow: outside Play it writes PlayerPrefs; in Play on Home it goes through
    // the scope's services, because the running service keeps its state in RAM and overwrites the prefs.
    public sealed class DailyRewardDebugWindow : EditorWindow
    {
        int jumpToDay = 1;
        string json = string.Empty;
        string jsonReport = string.Empty;
        MessageType jsonReportType;
        Vector2 scroll;

        // Cancelled when Play stops or the window closes, so an open still in flight dies with the scene.
        CancellationTokenSource openCts;

        [MenuItem("Game/Daily Reward Debug")]
        public static void Open()
        {
            GetWindow<DailyRewardDebugWindow>("Daily Reward Debug").minSize = new Vector2(380f, 460f);
        }

        // Claims and the clock move from inside the game too, so redraw steadily.
        void OnEnable()
        {
            EditorApplication.update += Repaint;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        void OnDisable()
        {
            EditorApplication.update -= Repaint;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            CancelOpen();
        }

        void OnGUI()
        {
            var live = Resolve<IDailyRewardService>();
            var schedule = Resolve<IDailyRewardSchedule>();
            IDailyRewardService state = live ?? new PlayerPrefsDailyRewardService(new SystemClockService());
            var days = schedule != null ? schedule.Days : ShippedCycle();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawState(live != null, state, days.Count);
            EditorGUILayout.Space();
            DrawCycle(live != null, state, days);
            EditorGUILayout.Space();

            if (live != null) DrawPlayActions(live, schedule);
            else DrawPrefsActions(state, days.Count);

            EditorGUILayout.Space();
            DrawClock();
            EditorGUILayout.Space();
            DrawJsonCheck();

            EditorGUILayout.EndScrollView();
        }

        static void DrawState(bool live, IDailyRewardService state, int cycleLength)
        {
            EditorGUILayout.LabelField("Trạng thái", EditorStyles.boldLabel);

            var canClaim = DailyRewardRules.CanClaim(state.LastClaimDay, state.Today);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Nguồn", live ? "IDailyRewardService (đang chạy)" : "PlayerPrefs");
                EditorGUILayout.IntField("Đã nhận (lần)", state.ClaimedCount);
                EditorGUILayout.TextField("Nhận gần nhất", state.LastClaimDay < 0 ? "chưa bao giờ" : DateOf(state.LastClaimDay));
                EditorGUILayout.TextField("Hôm nay", DateOf(state.Today));
                EditorGUILayout.TextField("Nhận được?", canClaim ? "Có" : "Không, đợi sang ngày");
                EditorGUILayout.TextField("Ô tiếp theo", SlotName(DailyRewardRules.NextSlot(state.ClaimedCount, cycleLength)));
            }
        }

        static void DrawCycle(bool live, IDailyRewardService state, IReadOnlyList<DailyRewardDay> days)
        {
            EditorGUILayout.LabelField(live ? "Chu kỳ (remote config đang chạy)" : "Chu kỳ (bộ mặc định, remote có thể khác)",
                EditorStyles.boldLabel);

            var canClaim = DailyRewardRules.CanClaim(state.LastClaimDay, state.Today);
            for (var slot = 0; slot < days.Count; slot++)
            {
                var mark = DailyRewardRules.StateOf(slot, state.ClaimedCount, days.Count, canClaim) switch
                {
                    DailyRewardDayState.Claimed => "✓",
                    DailyRewardDayState.Claimable => "▶",
                    _ => "·"
                };
                EditorGUILayout.LabelField($"{mark} {SlotName(slot)}", Describe(days[slot]));
            }
        }

        void DrawPrefsActions(IDailyRewardService state, int cycleLength)
        {
            EditorGUILayout.LabelField("Đặt tiến độ (ghi PlayerPrefs)", EditorStyles.boldLabel);

            if (GUILayout.Button("Giả lập đã nhận hôm qua (vào Home là có quà)")) Write(state.ClaimedCount, state.Today - 1);
            if (GUILayout.Button("Đánh dấu đã nhận hôm nay")) Write(Math.Max(1, state.ClaimedCount), state.Today);

            using (new EditorGUILayout.HorizontalScope())
            {
                jumpToDay = EditorGUILayout.IntSlider("Nhảy tới ngày", jumpToDay, 1, Math.Max(1, cycleLength));

                // Claims so far = day - 1, last claim yesterday: that day is the one on offer.
                if (GUILayout.Button("Đặt", GUILayout.Width(60))) Write(jumpToDay - 1, state.Today - 1);
            }

            if (GUILayout.Button("Xoá dữ liệu Daily Reward (như máy mới cài)"))
            {
                PlayerPrefs.DeleteKey(PlayerPrefsDailyRewardService.ClaimedCountPrefsKey);
                PlayerPrefs.DeleteKey(PlayerPrefsDailyRewardService.LastClaimDayPrefsKey);
                PlayerPrefs.Save();
            }

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Scene này không chạy Daily Reward: số ghi ở đây có hiệu lực từ lần vào Home tới.",
                    MessageType.Info);
            }
        }

        void DrawPlayActions(IDailyRewardService live, IDailyRewardSchedule schedule)
        {
            EditorGUILayout.LabelField("Đang chạy (qua service)", EditorStyles.boldLabel);

            // Bypasses the once-per-launch rule of DailyRewardAutoOpen on purpose.
            if (GUILayout.Button("Mở popup"))
            {
                openCts ??= new CancellationTokenSource();
                Resolve<IPopupService>()?.OpenAsync(PopupId.DailyReward, ct: openCts.Token).Forget();
            }

            if (GUILayout.Button("Nhận quà hôm nay (phát quà thật)"))
            {
                var slot = ClaimDailyRewardUseCase.Execute(live, schedule, Resolve<IDailyRewardGranter>());
                ShowNotification(new GUIContent(slot == ClaimDailyRewardUseCase.NotClaimed
                    ? "Hôm nay đã nhận rồi"
                    : "Đã nhận " + SlotName(slot)));
            }

            EditorGUILayout.HelpBox("Đặt tiến độ tuỳ ý chỉ làm được lúc chưa Play: service đang chạy giữ số trong RAM " +
                "và sẽ ghi đè PlayerPrefs.", MessageType.Info);
        }

        static void DrawClock()
        {
            EditorGUILayout.LabelField("Tua giờ (dùng chung đồng hồ với Life Debug)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Lệch hiện tại: {SystemClockService.DebugOffset}", EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Sang ngày mai (+1 ngày)")) SystemClockService.DebugOffset += TimeSpan.FromDays(1);
                if (GUILayout.Button("Về giờ thật")) SystemClockService.DebugOffset = TimeSpan.Zero;
            }
        }

        void DrawJsonCheck()
        {
            EditorGUILayout.LabelField($"Kiểm tra JSON của '{DailyRewardRemoteConfig.Cycle.Name}'", EditorStyles.boldLabel);
            json = EditorGUILayout.TextArea(json, GUILayout.MinHeight(70));

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Kiểm tra")) jsonReport = Check(json, out jsonReportType);
                if (GUILayout.Button("Dán bộ mặc định")) json = DailyRewardRemoteConfig.Cycle.Default;
            }

            if (!string.IsNullOrEmpty(jsonReport)) EditorGUILayout.HelpBox(jsonReport, jsonReportType);
        }

        // Same parser and rules the game runs, plus the two ways a valid cycle still misbehaves: an id
        // Sort Ninja cannot pay out, and an id with no icon row.
        static string Check(string text, out MessageType type)
        {
            if (!RemoteDailyRewardSchedule.TryParse(text, out var days, out var unlockLevel))
            {
                type = MessageType.Error;
                return "Không hợp lệ: game sẽ giữ bộ quà cũ. Cần số ngày là bội của " + DailyRewardRules.DaysPerPage +
                    ", ngày nào cũng có ít nhất một món với id và amount > 0.";
            }

            var settings = LoadSettings();
            var problems = new StringBuilder();
            for (var slot = 0; slot < days.Count; slot++)
            {
                foreach (var item in days[slot].Items)
                {
                    if (!DailyRewardPayoutRules.TryToShopReward(item, out _))
                        problems.Append($"\n{SlotName(slot)}: id '{item.Id}' không phát được quà.");
                    else if (settings != null && settings.Icon(item.Id) == null)
                        problems.Append($"\n{SlotName(slot)}: id '{item.Id}' chưa có icon trong DailyRewardSettings.");
                }
            }

            type = problems.Length > 0 ? MessageType.Warning : MessageType.Info;
            return $"Hợp lệ: {days.Count} ngày, unlock_level {unlockLevel}." + problems;
        }

        void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode) CancelOpen();
        }

        void CancelOpen()
        {
            openCts?.Cancel();
            openCts?.Dispose();
            openCts = null;
        }

        static void Write(int claimedCount, int lastClaimDay)
        {
            PlayerPrefs.SetInt(PlayerPrefsDailyRewardService.ClaimedCountPrefsKey, Math.Max(0, claimedCount));
            PlayerPrefs.SetInt(PlayerPrefsDailyRewardService.LastClaimDayPrefsKey, lastClaimDay);
            PlayerPrefs.Save();
        }

        static IReadOnlyList<DailyRewardDay> ShippedCycle() =>
            RemoteDailyRewardSchedule.TryParse(DailyRewardRemoteConfig.Cycle.Default, out var days, out _)
                ? days
                : Array.Empty<DailyRewardDay>();

        static string SlotName(int slot) =>
            $"Tuần {slot / DailyRewardRules.DaysPerPage + 1}, ngày {slot % DailyRewardRules.DaysPerPage + 1}";

        static string DateOf(int dayNumber) => new DateTime(dayNumber * TimeSpan.TicksPerDay).ToString("yyyy-MM-dd");

        static string Describe(DailyRewardDay day)
        {
            var text = new StringBuilder();
            foreach (var item in day.Items)
            {
                if (text.Length > 0) text.Append(", ");
                text.Append(item.Id).Append(" x").Append(item.Amount);
            }

            return day.IsChest ? "[rương] " + text : text.ToString();
        }

        static T Resolve<T>() where T : class
        {
            if (!Application.isPlaying) return null;

            foreach (var scope in FindObjectsByType<LifetimeScope>(FindObjectsSortMode.None))
            {
                if (scope.Container != null && scope.Container.TryResolve<T>(out var resolved)) return resolved;
            }

            return null;
        }

        static DailyRewardSettingsSO LoadSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:DailyRewardSettingsSO"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<DailyRewardSettingsSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) return asset;
            }

            return null;
        }
    }
}
