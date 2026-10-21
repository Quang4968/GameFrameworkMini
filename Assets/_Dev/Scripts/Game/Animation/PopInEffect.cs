using System.Threading;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

namespace Game.Game.Animation
{
    // Bung một object từ cỡ bé lên cỡ thật: nhích chậm, vọt ra, vượt qua cỡ thật rồi lùi về.
    //
    // Chạy hai pha, vì một ease không làm được cả hai việc: pha bung cần nhích chậm rồi vọt, còn cú nảy
    // cuối cần đi từ cỡ vọt quá lùi về cỡ thật. Ease Out* có sẵn cú nảy (OutBack) nhưng nó nhanh ngay
    // từ đầu, đúng thứ không muốn ở đây.
    //
    // Chỗ nối hai pha là chỗ dễ thấy cứng nhất: pha bung mà kết thúc lúc đang lao nhanh nhất, còn pha
    // lùi lại bắt đầu bằng vận tốc lớn nhất theo chiều ngược, thì vận tốc gãy góc ngay tại đỉnh. Hai ease
    // mặc định (InOutQuart / InOutSine) đều về gần 0 ở chỗ nối, nên object treo lại một nhịp rất ngắn ở
    // đỉnh rồi mới lùi.
    //
    // Chạy trên giờ thật (UpdateIgnoreTimeScale) để vẫn bung bình thường khi game đang dừng.
    [DisallowMultipleComponent]
    public sealed class PopInEffect : MonoBehaviour
    {
        [Tooltip("Object được phóng. Bỏ trống thì phóng chính object đang gắn script này.")]
        [SerializeField] Transform target;

        [Tooltip("Tổng số giây của cả hai pha. 0 = nhảy thẳng về cỡ thật, không phóng.")]
        [SerializeField] float duration = 0.8f;

        [Tooltip("Cỡ lúc bắt đầu, theo tỉ lệ cỡ thật. Đây CŨNG là cỡ object nằm chờ trước khi phóng, nên " +
            "để 0: nó vô hình cho tới đúng lúc bung ra.")]
        [SerializeField, Range(0f, 1f)] float startScale;

        [Tooltip("Ease của pha bung. InOut* nhích chậm lúc đầu, vọt ở giữa, rồi phanh lại khi tới đỉnh — " +
            "phanh này là thứ làm chỗ đổi chiều mượt. In* thuần (InQuart, InExpo) bung dữ hơn nhưng đang " +
            "lao nhanh nhất thì đổi chiều, nhìn cứng.")]
        [SerializeField] Ease popEase = Ease.InOutQuart;

        [Tooltip("Ease của nhịp lùi từ đỉnh về cỡ thật. InOutSine mềm cả hai đầu; OutQuad thì giật ngược " +
            "lại ngay.")]
        [SerializeField] Ease settleEase = Ease.InOutSine;

        [Tooltip("Cỡ vọt quá trước khi lùi về cỡ thật, theo tỉ lệ cỡ thật. 1 = không nảy, dừng phắt ở cỡ " +
            "thật.")]
        [SerializeField, Range(1f, 1.5f)] float overshoot = 1.15f;

        [Tooltip("Phần thời lượng dành cho pha bung, phần còn lại là nhịp lùi về cỡ thật.")]
        [SerializeField, Range(0.5f, 1f)] float burstRatio = 0.72f;

        MotionHandle motion;

        Transform Target => target != null ? target : transform;

        void OnDestroy()
        {
            motion.TryCancel();
        }

        // Đợi được: chỗ gọi thường cần biết lúc nào bung xong để làm việc nặng tiếp theo.
        public async UniTask PlayAsync(CancellationToken ct = default)
        {
            motion.TryCancel();

            if (duration <= 0f)
            {
                Target.localScale = Vector3.one;
                return;
            }

            var hasOvershoot = overshoot > 1f;
            var burstDuration = hasOvershoot ? duration * burstRatio : duration;

            await ScaleAsync(startScale, hasOvershoot ? overshoot : 1f, burstDuration, popEase, ct);

            if (!hasOvershoot) return;

            await ScaleAsync(overshoot, 1f, duration - burstDuration, settleEase, ct);
        }

        // Đưa về cỡ chờ. Chỗ dùng gọi khi ẩn đi, để lần bung sau chạy lại từ đầu chứ không đứng sẵn ở cỡ
        // thật rồi giật.
        public void ResetToStart()
        {
            motion.TryCancel();
            Target.localScale = Vector3.one * startScale;
        }

        UniTask ScaleAsync(float from, float to, float seconds, Ease ease, CancellationToken ct)
        {
            motion = LMotion.Create(from, to, seconds)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .BindToLocalScaleXYZ(Target)
                .AddTo(gameObject);

            return UniTask.WaitWhile(() => motion.IsActive(), cancellationToken: ct);
        }
    }
}
