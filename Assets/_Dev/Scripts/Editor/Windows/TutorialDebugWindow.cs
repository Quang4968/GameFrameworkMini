using System;
using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Game.Data;
using Game.Game.Services;
using UnityEditor;
using UnityEngine;
using VContainer.Unity;

namespace Game.Editor.Windows
{
    // Bàn điều khiển để test tay tutorial booster: xem bài nào đã học, xoá tiến độ, và nhảy thẳng
    // tới màn dạy một booster.
    //
    // Lý do tồn tại: mỗi bài chỉ chạy MỘT lần trong đời máy, và cửa vào của nó là màn 6/9/12/15/18
    // — không có cửa sổ này thì mỗi lần thử lại phải xoá PlayerPrefs bằng tay rồi cày lại tới đúng
    // màn đó.
    //
    // Lúc CHƯA chạy game thì làm việc thẳng trên PlayerPrefs. Lúc ĐANG chạy thì tiến độ đi qua
    // I_TutorialStateService của scene, và service ấy CHỈ ĐI TỚI — lùi được một bài về chưa-học thì
    // phải thoát Play Mode, cùng lý do với BoosterDebugWindow.
    //
    // Một điểm phải nhớ: bài học chỉ được XÉT lúc scene MainGame mở. Sửa gì ở đây rồi cũng phải vào
    // lại màn mới thấy.
    public sealed class TutorialDebugWindow : EditorWindow
    {
        const string LevelKey = "Game.CurrentLevel";

        TutorialSettingsSO settings;
        TutorialSchedule schedule;

        [MenuItem("Game/Tutorial Debug")]
        public static void Open()
        {
            GetWindow<TutorialDebugWindow>("Tutorial Debug").minSize = new Vector2(360f, 380f);
        }

        // Tiến độ đổi được từ trong game (nhận quà, dùng booster) chứ không chỉ từ cửa sổ này.
        void OnEnable()
        {
            EditorApplication.update += Repaint;
            Reload();
        }

        void OnDisable() => EditorApplication.update -= Repaint;

        void Reload()
        {
            settings = LoadSettings();
            schedule = settings != null ? settings.BuildSchedule() : TutorialSchedule.Empty;
        }

        void OnGUI()
        {
            var state = RuntimeState();

            EditorGUILayout.LabelField("Trạng thái", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.TextField("Nguồn", state != null
                    ? "I_TutorialStateService (đang chạy)"
                    : "PlayerPrefs (chưa chạy game)");
                EditorGUILayout.TextField("Màn hiện tại", (PlayerPrefs.GetInt(LevelKey, 0) + 1).ToString());
            }

            if (settings == null)
            {
                EditorGUILayout.HelpBox("Không tìm thấy TutorialSettings.asset.", MessageType.Warning);
                if (GUILayout.Button("Tìm lại")) Reload();
                return;
            }

            EditorGUILayout.HelpBox(
                "Bài học chỉ được xét lúc scene MainGame MỞ. Sửa xong thì mở lại màn mới thấy.",
                MessageType.Info);

            EditorGUILayout.Space();
            if (GUILayout.Button("Chơi lại từ bài đầu (xoá hết tiến độ + về màn 6)", GUILayout.Height(28f)))
            {
                ResetAll(state);
            }

            for (int i = 0; i < schedule.Count; i++)
            {
                EditorGUILayout.Space();
                DrawTutorial(state, schedule[i]);
            }
        }

        void DrawTutorial(I_TutorialStateService state, TutorialDefinition tutorial)
        {
            var stage = state != null
                ? state.GetStage(tutorial.Id)
                : StageFromPrefs(tutorial.Id);

            // Bài không mở booster nào thì không in tên booster: trường đó không có nghĩa.
            var what = tutorial.UnlocksBooster ? $"{tutorial.Booster}, mở" : "chỉ dạy, chạy";

            EditorGUILayout.LabelField(
                $"{tutorial.Id} — {what} ở màn {tutorial.LevelIndex + 1}",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Describe(stage), EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                // Nhảy tới ĐÚNG màn dạy bài này, không phải màn trước: booster mở khoá ngay khi
                // người chơi bước vào màn đó.
                if (GUILayout.Button("Vào màn này"))
                {
                    PlayerPrefs.SetInt(LevelKey, tutorial.LevelIndex);
                    PlayerPrefs.Save();
                }

                using (new EditorGUI.DisabledScope(state != null))
                {
                    if (GUILayout.Button("Về chưa học")) Forget(tutorial.Id);
                }

                if (GUILayout.Button("Đánh dấu xong"))
                {
                    if (state != null) state.SetStage(tutorial.Id, E_TutorialStage.Done);
                    else PlayerPrefs.SetInt(KeyOf(tutorial.Id), (int)E_TutorialStage.Done);

                    PlayerPrefs.Save();
                }
            }
        }

        void ResetAll(I_TutorialStateService state)
        {
            foreach (E_TutorialId id in Enum.GetValues(typeof(E_TutorialId))) Forget(id);

            if (schedule.Count > 0) PlayerPrefs.SetInt(LevelKey, schedule[0].LevelIndex);
            PlayerPrefs.Save();

            if (state == null) return;

            EditorUtility.DisplayDialog("Tutorial Debug",
                "Đã xoá PlayerPrefs. Service đang chạy vẫn giữ tiến độ cũ trong RAM — thoát Play Mode " +
                "rồi mở lại MainGame để chạy bài học từ đầu.",
                "OK");
        }

        static void Forget(E_TutorialId id)
        {
            PlayerPrefs.DeleteKey(KeyOf(id));
            PlayerPrefs.Save();
        }

        static string Describe(E_TutorialStage stage) => stage switch
        {
            E_TutorialStage.RewardClaimed => "Đã nhận quà — sẽ KHÔNG hiện lại nữa.",
            E_TutorialStage.Done => "Đã nhận quà và đã dùng booster. Không hiện lại.",
            _ => "Chưa nhận quà — vào màn là hiện popup quà."
        };

        static E_TutorialStage StageFromPrefs(E_TutorialId id)
        {
            var stored = PlayerPrefs.GetInt(KeyOf(id), (int)E_TutorialStage.NotStarted);

            if (stored <= (int)E_TutorialStage.NotStarted) return E_TutorialStage.NotStarted;
            return stored >= (int)E_TutorialStage.Done ? E_TutorialStage.Done : E_TutorialStage.RewardClaimed;
        }

        static string KeyOf(E_TutorialId id) => PlayerPrefsTutorialStateService.PrefsKeyPrefix + id;

        static I_TutorialStateService RuntimeState()
        {
            if (!Application.isPlaying) return null;

            foreach (var scope in FindObjectsByType<LifetimeScope>(FindObjectsSortMode.None))
            {
                if (scope.Container == null) continue;

                try
                {
                    return (I_TutorialStateService)scope.Container.Resolve(typeof(I_TutorialStateService), null);
                }
                catch (Exception)
                {
                    // Scene này không đăng ký hệ tutorial (Home, Boot) — thử scope kế tiếp.
                }
            }

            return null;
        }

        static TutorialSettingsSO LoadSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:TutorialSettingsSO"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<TutorialSettingsSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) return asset;
            }

            return null;
        }
    }
}
