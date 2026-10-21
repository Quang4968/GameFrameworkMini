using System;
using System.Collections.Generic;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Data
{
    // Thông số âm thanh dùng CHUNG cho cả game — chỉnh ở asset là mọi scene đổi theo, không scene
    // nào được tự khai clip hay âm lượng riêng.
    [CreateAssetMenu(menuName = "Game/Audio Settings", fileName = "AudioSettings")]
    public sealed class AudioSettingsSO : ScriptableObject
    {
        [Tooltip("Mỗi AudioCue một dòng. Cue chưa có dòng nào — hoặc có dòng nhưng chưa gán clip — " +
            "bị bỏ qua im lặng, nên gọi cue trong code trước khi có tiếng vẫn chạy bình thường.")]
        [SerializeField] CueEntry[] cueEntries;

        [Tooltip("Nhạc nền chạy lặp. Bỏ trống thì thanh trượt nhạc và nhịp fade vẫn hoạt động, " +
            "chỉ là không có gì để phát.")]
        [SerializeField] AudioClip musicClip;

        [Tooltip("Mức nhạc trong bản mix, cân so với tiếng hiệu ứng. Thanh trượt của người chơi " +
            "nhân thêm vào đây chứ không thay thế, nên hạ số này là nhạc nhỏ đi ở mọi nấc trượt.")]
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.5f;

        [Tooltip("Số giây nhạc fade vào lúc bật và fade ra lúc tắt. Cắt thẳng một bản đang lặp " +
            "nghe như lỗi chứ không như đổi cài đặt. Để 0 là cắt thẳng.")]
        [SerializeField, Min(0f)] float musicFadeSeconds = 0.6f;

        [Tooltip("Số AudioSource giữ sẵn cho tiếng one-shot. Pitch là thuộc tính của source chứ " +
            "không phải của PlayOneShot, nên hai cue chồng nhau phải nằm ở hai source mới giữ " +
            "được pitch riêng. Chém + khối vỡ + popup là ba tiếng cùng lúc, bốn là dư một nhịp.")]
        [SerializeField, Range(1, 8)] int sfxSourceCount = 4;

        public IReadOnlyList<CueEntry> CueEntries => cueEntries;
        public AudioClip MusicClip => musicClip;
        public float MusicVolume => musicVolume;
        public float MusicFadeSeconds => musicFadeSeconds;
        public int SfxSourceCount => sfxSourceCount;

        [Serializable]
        public sealed class CueEntry
        {
            [SerializeField] AudioCue cue;

            [Tooltip("Một clip thì phát nguyên bản; nhiều clip thì bốc ngẫu nhiên, để tiếng nghe " +
                "hàng trăm lần mỗi ván không mòn thành một vệt.")]
            [SerializeField] AudioClip[] clips;

            [SerializeField, Range(0f, 1f)] float volume = 1f;

            [Tooltip("Độ lệch pitch ngẫu nhiên về cả hai phía quanh 1. 0.05 gần như không nhận ra " +
                "nhưng đủ để các lần lặp không nghe như đóng dấu; 0 là tắt.")]
            [SerializeField, Range(0f, 0.5f)] float pitchVariance;

            [Tooltip("Số giây cue này im sau mỗi lần phát. Dành cho tiếng bắn liên tục như nhát " +
                "burst - five hits at once would smear into one sound; 0 turns it off.")]
            [SerializeField, Min(0f)] float minRepeatSeconds;

            [Tooltip("Mỗi lần cue này phát nối tiếp trong cửa sổ dồn thì pitch cộng thêm bấy " +
                "nhiêu, kiểu chuỗi combo lên giọng dần thay vì lặp y hệt. 0.05-0.1 là nghe rõ " +
                "mà chưa chói; 0 là tắt, cue phát nguyên pitch gốc.")]
            [SerializeField, Range(0f, 0.5f)] float stackPitchStep;

            [Tooltip("Trần pitch của chuỗi dồn. Chặn ở đây chứ không đếm số bậc, để chỉnh " +
                "stackPitchStep không phải tính lại xem cao nhất thành bao nhiêu.")]
            [SerializeField, Range(1f, 3f)] float stackMaxPitch = 2f;

            [Tooltip("Cách nhau quá bấy nhiêu giây thì chuỗi dồn coi như đứt và pitch về gốc. " +
                "Để dài quá thì hai nhát chém rời rạc vẫn bị cộng dồn thành tiếng chói.")]
            [SerializeField, Min(0f)] float stackResetSeconds = 0.35f;

            public AudioCue Cue => cue;
            public IReadOnlyList<AudioClip> Clips => clips;
            public float Volume => volume;
            public float PitchVariance => pitchVariance;
            public float MinRepeatSeconds => minRepeatSeconds;
            public float StackPitchStep => stackPitchStep;
            public float StackMaxPitch => stackMaxPitch;
            public float StackResetSeconds => stackResetSeconds;
        }
    }
}
