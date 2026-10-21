using System;
using LitMotion;
using Game.Core.Services;
using Game.Game.Data;
using UnityEngine;

namespace Game.Game.Services
{
    // Translates Core's AudioCue into a real AudioSource. Same shape as SdkHapticService: volume belongs to
    // ISettingsService and this service listens for its changes, so a caller only says what to hear.
    //
    // Âm lượng cuối = mức trên asset (mix của game) nhân âm lượng người chơi chọn. Asset giữ vai
    // trò cân tiếng này to nhỏ hơn tiếng kia, thanh trượt chỉ kéo cả cụm lên xuống.
    //
    // Service này SỐNG THEO SCENE (scope chết là nó Dispose) nhưng cụm AudioSource thì KHÔNG —
    // xem AudioHost. Hai hệ quả bắt buộc:
    //   - Dispose chỉ gỡ listener, tuyệt đối không tắt nhạc và không huỷ host.
    //   - Vào scene mới, nhạc đang chạy đúng bản thì để nguyên; phát lại là nhạc nền tua về đầu
    //     mỗi lần đổi màn.
    public sealed class AudioService : IAudioService, IDisposable
    {
        static readonly int CueCount = Enum.GetValues(typeof(AudioCue)).Length;

        readonly ISettingsService settings;
        readonly AudioSettingsSO audioSettings;

        // Scene này có nhạc nền hay không. Tắt thì service vẫn phát hiệu ứng bình thường, chỉ
        // kéo nhạc về 0 — kể cả bản nhạc scene trước để lại trên AudioHost.
        readonly bool playsMusic;
        readonly AudioHost host;
        readonly AudioSettingsSO.CueEntry[] entriesByCue;
        readonly float[] lastPlayTimeByCue;

        // Số bậc pitch mà cue đang bị dồn. Đếm riêng từng cue: tiếng chém dồn lên không được kéo
        // theo tiếng popup.
        readonly int[] stackStepByCue;

        public AudioService(AudioSettingsSO audioSettings, ISettingsService settings, bool playsMusic = true)
        {
            this.audioSettings = audioSettings;
            this.settings = settings;
            this.playsMusic = playsMusic;

            // Thiếu asset thì service nằm im chứ không ném lỗi ở mỗi cue — cùng lối xử lý với
            // popupHost/poolCatalog ở LifetimeScope: mất tiếng còn hơn vỡ scene.
            if (audioSettings == null)
            {
                Debug.LogWarning($"[{nameof(AudioService)}] Chưa gán AudioSettings trên LifetimeScope " +
                    "-> game chạy không có tiếng nào.");
                return;
            }

            entriesByCue = new AudioSettingsSO.CueEntry[CueCount];
            lastPlayTimeByCue = new float[CueCount];
            stackStepByCue = new int[CueCount];

            // NegativeInfinity chứ không phải 0: để 0 thì mọi cue có minRepeatSeconds đều bị nuốt
            // trong những giây đầu của phiên chơi, đúng lúc màn hình Home vừa hiện ra.
            for (int i = 0; i < lastPlayTimeByCue.Length; i++) lastPlayTimeByCue[i] = float.NegativeInfinity;

            var entries = audioSettings.CueEntries;
            for (int i = 0; entries != null && i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry != null) entriesByCue[(int)entry.Cue] = entry;
            }

            host = AudioHost.GetOrCreate(audioSettings.SfxSourceCount);

            // Chỉ nghe nhạc: âm lượng hiệu ứng được đọc lại ở từng lần Play nên không cần sự kiện.
            settings.MusicVolumeChanged += OnMusicVolumeChanged;
            SyncMusic();
        }

        public void Play(AudioCue cue)
        {
            if (host == null) return;

            // Không có cờ bật/tắt riêng: 0 là tắt hẳn.
            float soundVolume = settings.SoundVolume;
            if (soundVolume <= 0f) return;

            int index = (int)cue;
            var entry = entriesByCue[index];
            var clips = entry != null ? entry.Clips : null;
            if (clips == null || clips.Count == 0) return;

            // unscaledTime: nhịp chặn lặp không được giãn ra khi game dừng hoặc chạy chậm.
            float now = Time.unscaledTime;
            float sinceLast = now - lastPlayTimeByCue[index];
            if (entry.MinRepeatSeconds > 0f && sinceLast < entry.MinRepeatSeconds) return;

            var clip = clips[UnityEngine.Random.Range(0, clips.Count)];
            if (clip == null) return;

            int step = NextStackStep(entry, index, sinceLast);
            stackStepByCue[index] = step;
            lastPlayTimeByCue[index] = now;

            // Trần chặn ở pitch chứ không ở số bậc, và cộng biến thiên ngẫu nhiên SAU khi chặn để
            // các lần lặp ở đỉnh chuỗi vẫn không nghe như đóng dấu.
            float pitch = Mathf.Min(1f + step * entry.StackPitchStep, entry.StackMaxPitch);

            var source = host.NextSfxSource();
            source.pitch = pitch + UnityEngine.Random.Range(-entry.PitchVariance, entry.PitchVariance);
            source.PlayOneShot(clip, entry.Volume * soundVolume);
        }

        // Nhiều luồng đập vào cùng một cue trong cửa sổ dồn thì mỗi lần lên một bậc; rời ra quá
        // cửa sổ là về gốc. Bậc bị chặn ngay ở đây theo trần pitch, nếu không bộ đếm cứ trôi lên
        // trong lúc tiếng đã kịch trần và phải chờ đúng chừng ấy nhịp im mới tụt xuống lại.
        int NextStackStep(AudioSettingsSO.CueEntry entry, int index, float sinceLast)
        {
            if (entry.StackPitchStep <= 0f || sinceLast > entry.StackResetSeconds) return 0;

            int maxStep = Mathf.CeilToInt((entry.StackMaxPitch - 1f) / entry.StackPitchStep);
            return Mathf.Min(stackStepByCue[index] + 1, maxStep);
        }

        public void Dispose()
        {
            if (settings != null) settings.MusicVolumeChanged -= OnMusicVolumeChanged;

            // Fade đang dở thì CỨ ĐỂ CHẠY: huỷ ở đây thì tắt nhạc đúng lúc đổi scene sẽ để âm
            // lượng kẹt lại lưng chừng. Handle của nó nằm ở host nên service của scene sau vẫn
            // huỷ được nếu cần.
        }

        void OnMusicVolumeChanged(float volume)
        {
            SyncMusic();
        }

        void SyncMusic()
        {
            if (host == null) return;

            var music = host.Music;

            // Scene không có nhạc — hoặc asset chưa gán bản nào — thì đích là 0, và nhánh dưới lo
            // fade tắt bản mà scene trước để lại.
            float target = playsMusic && audioSettings.MusicClip != null
                ? audioSettings.MusicVolume * settings.MusicVolume
                : 0f;

            if (target <= 0f)
            {
                if (music.isPlaying) FadeMusicTo(0f, stopWhenDone: true);
                return;
            }

            // Đang chạy đúng bản rồi thì chỉ chỉnh âm lượng, không phát lại — đây là chỗ giữ cho
            // nhạc liền mạch qua đổi scene. Đặt thẳng chứ không fade: người chơi kéo thanh trượt
            // phải nghe đổi ngay dưới ngón tay, và mỗi nấc kéo mà mở một fade 0.6s thì âm lượng
            // luôn chạy đuổi phía sau. Nhánh này cũng bắt được lúc kéo ngược lên giữa chừng một
            // fade-out: huỷ fade nên bản nhạc không bị Stop oan.
            if (music.isPlaying && music.clip == audioSettings.MusicClip)
            {
                host.CancelMusicFade();
                music.volume = target;
                return;
            }

            music.clip = audioSettings.MusicClip;
            music.volume = 0f;
            music.Play();
            FadeMusicTo(target, stopWhenDone: false);
        }

        void FadeMusicTo(float targetVolume, bool stopWhenDone)
        {
            host.CancelMusicFade();

            var music = host.Music;

            if (audioSettings.MusicFadeSeconds <= 0f)
            {
                music.volume = targetVolume;
                if (stopWhenDone) music.Stop();
                return;
            }

            host.SetMusicFade(LMotion.Create(music.volume, targetVolume, audioSettings.MusicFadeSeconds)
                // Nhạc vẫn phải fade khi game đang dừng (timeScale = 0) — tắt nhạc trong popup
                // pause là đúng tình huống đó.
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    if (stopWhenDone) music.Stop();
                })
                .Bind(v => music.volume = v)
                .AddTo(host.gameObject));
        }
    }
}
