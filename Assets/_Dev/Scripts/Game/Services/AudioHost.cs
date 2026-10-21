using LitMotion;
using UnityEngine;

namespace Game.Game.Services
{
    // Cụm AudioSource thật của game. Sống XUYÊN SCENE (static + DontDestroyOnLoad) vì mỗi scene
    // có LifetimeScope riêng và scope bị huỷ khi rời scene — để container giữ hộ thì nhạc nền bị
    // cắt ngang mỗi lần đổi màn, và tiếng đang phát dở bị chém cụt.
    //
    // Cùng lối với SceneTransitionProvider: dựng lười ở lần cần đầu tiên, nên vào thẳng PlayMode
    // từ scene nào cũng có tiếng, không cần kéo sẵn object vào từng scene.
    [DisallowMultipleComponent]
    public sealed class AudioHost : MonoBehaviour
    {
        const string HostName = "Audio (runtime)";

        static AudioHost instance;

        AudioSource[] sfxSources;
        int nextSfxIndex;

        // Fade nhạc đang chạy. Nằm ở host chứ KHÔNG ở AudioService: service chết theo scene còn
        // fade thì không, nên để ở service thì scene sau không huỷ được fade của scene trước —
        // cú OnComplete muộn của một lần fade-out sẽ Stop mất bản nhạc vừa được bật lại.
        MotionHandle musicFade;

        // Source riêng cho nhạc nền: nó loop và bị fade âm lượng, không dùng chung với one-shot được.
        public AudioSource Music { get; private set; }

        // sfxSourceCount chỉ có tác dụng ở LẦN DỰNG ĐẦU TIÊN. Host sống lâu hơn mọi scene nên
        // scene sau đổi con số này cũng không dựng lại — đằng nào nó cũng là thông số dùng chung
        // trên một asset AudioSettings duy nhất.
        public static AudioHost GetOrCreate(int sfxSourceCount)
        {
            // So sánh null kiểu UnityEngine.Object: bắt được cả trường hợp tắt Domain Reload,
            // instance cũ đã bị huỷ nhưng static vẫn còn trỏ tới.
            if (instance != null) return instance;

            var go = new GameObject(HostName);
            DontDestroyOnLoad(go);

            instance = go.AddComponent<AudioHost>();
            instance.Build(sfxSourceCount);
            return instance;
        }

        // Xoay vòng qua từng source. Bắt buộc phải xoay: pitch nằm trên source chứ không đi kèm
        // PlayOneShot, nên hai cue chồng nhau dùng chung một source sẽ nghe chung một pitch.
        public AudioSource NextSfxSource()
        {
            var source = sfxSources[nextSfxIndex];
            nextSfxIndex = (nextSfxIndex + 1) % sfxSources.Length;
            return source;
        }

        // Huỷ fade đang dở, ai gọi cũng được — kể cả service của scene sau.
        public void CancelMusicFade()
        {
            musicFade.TryCancel();
        }

        public void SetMusicFade(MotionHandle handle)
        {
            musicFade = handle;
        }

        void Build(int sfxSourceCount)
        {
            sfxSources = new AudioSource[Mathf.Max(1, sfxSourceCount)];
            for (int i = 0; i < sfxSources.Length; i++) sfxSources[i] = CreateSource(loop: false);

            Music = CreateSource(loop: true);
        }

        AudioSource CreateSource(bool loop)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;

            // 2D hoàn toàn: không tiếng nào của game cần định vị trong không gian, mà host thì
            // đứng yên ở gốc toạ độ nên để 3D là tiếng sẽ nhỏ dần theo vị trí camera.
            source.spatialBlend = 0f;
            return source;
        }
    }
}
