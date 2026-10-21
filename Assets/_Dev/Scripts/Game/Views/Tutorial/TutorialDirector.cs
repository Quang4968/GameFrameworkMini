using Cysharp.Threading.Tasks;
using MoodBoost.Sdk.Tracking;
using Game.Core.Domain;
using Game.Core.GameLogic;
using Game.Core.Services;
using Game.Core.UseCases;
using Game.Game.Data;
using Game.Game.Services;
using Game.Game.Views.Popup;
using System;
using UnityEngine;
using VContainer;

namespace Game.Game.Views.Tutorial
{
    // Decides whether this level owes the player a lesson, and runs it.
    //
    // The WHOLE lesson runs here at the start of the unlock level, reward popup included: the free
    // boosters must fly into the booster bar, and that bar only exists in this HUD.
    public sealed class TutorialDirector : MonoBehaviour
    {
        [Tooltip("Lớp phủ khoét lỗ dùng chung cho mọi bước có chặn thao tác.")]
        [SerializeField] TutorialSpotlight spotlight;

        [Tooltip("Bàn tay chỉ dẫn dùng chung cho mọi bước.")]
        [SerializeField] TutorialHandHint hand;

        [Tooltip("Mỗi bài học một sequence. Bài học không có sequence nào thì coi như đã học xong " +
            "— quà vẫn được tặng, chỉ là không có phần hướng dẫn.")]
        [SerializeField] TutorialSequence[] sequences;

        [Tooltip("Số frame chờ trước khi bắt đầu, để board và bục kịp dựng xong. Bước đầu tiên trỏ " +
            "vào một nút UI nên không cần lâu.")]
        [SerializeField] int startDelayFrames = 2;

        TutorialSchedule schedule;
        ILevelProgressService levelProgress;
        I_TutorialStateService state;
        TutorialRewardGate rewardGate;
        TutorialSettingsSO settings;
        IGameplayPauseService pause;
        ITrackingService tracking;
        GameRemoteConfig remoteConfig;

        TutorialSequence running;
        IDisposable pauseHold;

        // True while a lesson that covers the screen runs; the game locks its own free-look
        // controls on it, since the spotlight cannot stop input read straight from the device.
        public event Action<bool> LessonRunningChanged;

        [Inject]
        public void Construct(TutorialSchedule tutorialSchedule, ILevelProgressService levelProgressService,
            I_TutorialStateService tutorialState, TutorialRewardGate gate, TutorialSettingsSO tutorialSettings,
            IGameplayPauseService gameplayPause,
            ITrackingService trackingService, GameRemoteConfig gameRemoteConfig)
        {
            schedule = tutorialSchedule;
            levelProgress = levelProgressService;
            state = tutorialState;
            rewardGate = gate;
            settings = tutorialSettings;
            pause = gameplayPause;
            tracking = trackingService;
            remoteConfig = gameRemoteConfig;
        }

        void Start() => BeginAsync().Forget();

        void OnDestroy()
        {
            if (running != null) running.Stop();

            ReleasePause();
            ReportLesson(false);
        }

        void ReleasePause()
        {
            pauseHold?.Dispose();
            pauseHold = null;
        }

        void ReportLesson(bool running) => LessonRunningChanged?.Invoke(running);

        async UniTaskVoid BeginAsync()
        {
            if (!GetDueTutorialUseCase.Execute(schedule, levelProgress, state, out var tutorial)) return;

            var sequence = FindSequence(tutorial.Id);

            // Only a lesson that covers the screen freezes the model; one that merely points must
            // leave the player free to turn it.
            ReportLesson(sequence == null || sequence.BlocksInput);

            // The HUD is built the frame the scene opens; the board it points at is not.
            await UniTask.DelayFrame(Mathf.Max(0, startDelayFrames),
                cancellationToken: this.GetCancellationTokenOnDestroy());

            // The gift popup is already part of the lesson, so start is logged before it opens.
            var tracker = new TutorialActionTracker(tracking, tutorial.Booster);
            tracker.Start();

            // A lesson that unlocks nothing goes straight to its steps: the unlock popup IS the
            // gift, so an empty one would be a panel with nothing on it — and with no popup to
            // read behind, there is nothing to hold the game still for either.
            if (!tutorial.UnlocksBooster)
            {
                PlayLesson(tutorial, sequence, tracker);
                return;
            }

            // A due tutorial is always unclaimed: GetDueTutorialUseCase drops everything from
            // RewardClaimed on, so the gift always comes first and the lesson always follows it.
            //
            // The game holds still behind the gift, or the level plays itself while the player reads
            // a popup they cannot dismiss.
            pauseHold = pause != null ? pause.TryHold() : null;

            rewardGate.Open(tutorial, () =>
            {
                // Runs only on the claim press, the first thing the player does in this lesson.
                tracker.Report(E_TutorialAction.Click);
                ReleasePause();
                PlayLesson(tutorial, sequence, tracker);
            });
        }

        void PlayLesson(TutorialDefinition tutorial, TutorialSequence sequence, TutorialActionTracker tracker)
        {
            if (sequence == null)
            {
                // Nothing built yet: mark it learned rather than strand the player on this level
                // with a reward they already took.
                state.SetStage(tutorial.Id, E_TutorialStage.Done);
                ReportLesson(false);
                tracker.Finish();
                return;
            }

            running = sequence;

            var context = new TutorialRunContext(spotlight, hand, pause,
                settings != null ? settings.UsageMessage(tutorial.Id) : string.Empty,
                remoteConfig != null ? remoteConfig.TutorialHintDelaySeconds :
                    settings != null ? settings.HintDelaySeconds(tutorial.Id) : 0f, tracker);

            sequence.Play(context, () =>
            {
                running = null;
                state.SetStage(tutorial.Id, E_TutorialStage.Done);
                ReportLesson(false);
                tracker.Finish();
            });
        }

        TutorialSequence FindSequence(E_TutorialId id)
        {
            if (sequences == null) return null;

            foreach (var sequence in sequences)
            {
                if (sequence != null && sequence.Id == id) return sequence;
            }

            return null;
        }
    }
}
