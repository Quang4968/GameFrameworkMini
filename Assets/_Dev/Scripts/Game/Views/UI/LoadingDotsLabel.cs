using TMPro;
using UnityEngine;

namespace Game.Game.Views.UI
{
    // Chạy dấu chấm sau chữ "Loading": Loading -> Loading. -> Loading.. -> Loading... rồi quay lại.
    //
    // Tách rời khỏi màn loading của transition để gắn được ở bất kỳ đâu cần chữ chờ — scene Boot chẳng
    // hạn. Nó chỉ biết mỗi việc đổi chữ; ai bật/tắt nó là việc của chỗ dùng.
    //
    // Chữ đổ bóng là hai text chồng nhau nên `labels` là một mảng: cả hai phải đổi trong cùng một frame,
    // không thì bóng lệch chữ.
    [DisallowMultipleComponent]
    public sealed class LoadingDotsLabel : MonoBehaviour
    {
        [Tooltip("Các nhãn cùng hiện một chuỗi. Chữ đổ bóng thì kéo cả chữ chính lẫn chữ bóng vào đây. " +
            "Phần dấu chấm do script nối thêm, đừng gõ sẵn vào text.")]
        [SerializeField] TMP_Text[] labels;

        [Tooltip("Chữ đứng yên phía trước dấu chấm.")]
        [SerializeField] string text = "Loading";

        [Tooltip("Số dấu chấm tối đa chạy sau chữ. 0 = nhãn đứng yên.")]
        [SerializeField, Range(0, 5)] int dotCount = 3;

        [Tooltip("Số giây giữa hai lần thêm một dấu chấm.")]
        [SerializeField] float intervalSeconds = 0.3f;

        string[] frames;
        string builtText;
        float elapsed;
        int shownDots;

        // Bật component lên là chạy lại từ chữ trần, nên mỗi lần màn loading hiện ra đều thấy đúng một
        // vòng chấm giống nhau.
        void OnEnable()
        {
            Restart();
        }

        void Update()
        {
            if (dotCount <= 0 || intervalSeconds <= 0f) return;

            // unscaledDeltaTime: chữ chờ vẫn phải sống khi game đang dừng (timeScale = 0) — ví dụ bấm
            // nút thoát trong popup pause.
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < intervalSeconds) return;

            // Đếm LẠI TỪ 0, không giữ phần dư. Giữ dư thì một frame dài (frame kích hoạt scene mới dài
            // cả trăm ms tới cả giây) đẩy bộ đếm lên sát ngay dưới mốc kế tiếp: chấm này vừa hiện, chấm
            // sau đã bật ra ở frame ngay sau đó, cách nhau vài ms. Đếm lại từ 0 thì mỗi chấm luôn được
            // nhìn thấy trọn một nhịp, dù frame trước có dài bao nhiêu.
            elapsed = 0f;
            Show(shownDots + 1);
        }

        public void Restart()
        {
            elapsed = 0f;
            Show(0);
        }

        void Show(int dots)
        {
            // Chỉnh Dot Count hay Text ngay trong Play Mode là chuyện thường khi căn nhịp, nên dựng lại
            // khung chữ nếu chúng đã khác.
            EnsureFrames();
            shownDots = dots % frames.Length;

            if (labels == null) return;

            var frame = frames[shownDots];
            foreach (var label in labels)
            {
                if (label == null) continue;

                // Dấu chấm ẩn đi bằng thẻ rich text, tắt richText là lòi thẻ ra thành chữ.
                label.richText = true;
                label.text = frame;
            }
        }

        // Dựng sẵn từng khung chữ: nhịp đổi chữ hay rơi đúng vào lúc đang load scene, không phải chỗ để
        // cấp phát chuỗi mới.
        //
        // Dấu chấm chưa tới lượt vẫn được vẽ nhưng trong suốt — nhờ vậy bề rộng dòng chữ không đổi và
        // chữ đứng yên, thay vì nhích qua lại mỗi lần thêm một chấm.
        void EnsureFrames()
        {
            if (frames != null && frames.Length == dotCount + 1 && builtText == text) return;

            builtText = text;
            frames = new string[dotCount + 1];
            for (var i = 0; i <= dotCount; i++)
            {
                frames[i] = text + new string('.', i) + "<alpha=#00>" + new string('.', dotCount - i);
            }
        }
    }
}
