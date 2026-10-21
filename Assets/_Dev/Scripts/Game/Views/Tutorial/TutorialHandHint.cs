using System;
using TMPro;
using UnityEngine;

namespace Game.Game.Views.Tutorial
{
    // The pointing hand and its one line of text. Follows its target every frame - the piece it
    // points at can be moving, and a hand left behind is worse than no hand.
    //
    // It never sits ON the target: each approach is aimed so the whole art clears the target rect
    // and only the fingertip reaches it. From above the hand hangs over the target and pokes down;
    // from below it reaches up, which is what a button needs — a hand dropping in from above would
    // cover the board the player is being asked to read.
    //
    // Anchor, pivot and size belong to the prefab; angle and position come from here, because the
    // two approaches need two different angles. The two Aim rows are the ONLY knobs for lining the
    // fingertip up with the target — new art means retuning them, never touching a pivot.
    public sealed class TutorialHandHint : MonoBehaviour
    {
        [Serializable]
        struct Aim
        {
            [Tooltip("Góc xoay quanh trục Z của art, tính bằng độ.")]
            public float angle;

            [Tooltip("Lệch của bàn tay so với điểm ngắm giữa mép vùng mục tiêu, tính bằng pixel, " +
                "đo ở lúc chọc sâu nhất.")]
            public Vector2 offset;
        }

        [Tooltip("Bàn tay. Neo vào giữa container, góc và vị trí do code đặt mỗi frame.")]
        [SerializeField] RectTransform hand;

        [Tooltip("Tuỳ chọn: dòng chữ đi kèm, vd \"Tap to Select\". Bỏ trống thì chỉ có bàn tay. " +
            "Phải là ANH EM của bàn tay, không phải con — chữ nằm giữa màn hình và không nhấp nhô " +
            "theo nhịp chọc.")]
        [SerializeField] TMP_Text messageLabel;

        [Tooltip("Trỏ từ TRÊN xuống, ngắm vào mép trên vùng mục tiêu.")]
        [SerializeField] Aim fromAbove = new() { angle = 150f, offset = new Vector2(-28f, 199f) };

        [Tooltip("Trỏ từ DƯỚI lên, ngắm vào mép dưới vùng mục tiêu.")]
        [SerializeField] Aim fromBelow = new() { angle = -40f, offset = new Vector2(-3f, -201f) };

        [Tooltip("Tay lùi ra XA mục tiêu bao nhiêu pixel giữa hai lần chọc, nên điểm chạm luôn " +
            "đúng chỗ đã ngắm dù trỏ từ phía nào.")]
        [SerializeField] float pokeDistance = 45f;

        [Tooltip("Tốc độ nhịp chọc.")]
        [SerializeField] float pokeSpeed = 4f;

        [Tooltip("Khoảng hở giữa dòng chữ và thứ nằm sát nó, tính bằng pixel.")]
        [SerializeField] float messageGap = 40f;

        readonly Vector3[] handCorners = new Vector3[4];

        RectTransform self;
        Canvas canvas;
        float pokeTime;

        public bool IsShowing => gameObject.activeSelf;

        // Resolved on demand: disabled in the prefab, so Awake does not run until shown.
        RectTransform Self => self != null ? self : self = (RectTransform)transform;

        RectTransform MessageRect => messageLabel != null ? (RectTransform)messageLabel.transform : null;

        Canvas ParentCanvas => canvas != null ? canvas : canvas = GetComponentInParent<Canvas>(true);

        public void Show(string message)
        {
            if (messageLabel != null)
            {
                messageLabel.gameObject.SetActive(!string.IsNullOrWhiteSpace(message));
                messageLabel.text = message;
            }

            pokeTime = 0f;
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        public void PointAt(Rect screenRect, E_HandApproach approach)
        {
            if (hand == null) return;
            if (!TutorialScreenRect.TryToLocalRect(screenRect, Self, ParentCanvas, out var local)) return;

            // Unscaled: the tutorial runs while the game sits paused behind it.
            pokeTime += Time.unscaledDeltaTime * pokeSpeed;

            var pointsDown = approach == E_HandApproach.FromAbove;
            var aim = pointsDown ? fromAbove : fromBelow;
            var touch = new Vector2(local.center.x, pointsDown ? local.yMax : local.yMin) + aim.offset;
            var retreat = pointsDown ? pokeDistance : -pokeDistance;

            // Nửa cosin nên đi 0 -> sâu nhất -> 0 liền mạch và bắt đầu từ tư thế nghỉ, không giật
            // một cái ngay frame đầu như dùng sin.
            var press = 0.5f * (1f - Mathf.Cos(pokeTime));

            hand.localRotation = Quaternion.Euler(0f, 0f, aim.angle);
            hand.localPosition = new Vector3(touch.x, touch.y + retreat * (1f - press), 0f);

            // Chữ đo theo lúc tay lùi xa nhất nên nó đứng yên thay vì nhấp nhô theo nhịp chọc.
            PlaceMessage(local, touch.y + retreat + HandTopExtent());
        }

        // Above whichever reaches higher, the target or the hand — then below the target when there
        // is no room up top. Never attached to the hand: the line would bob along with every poke.
        //
        // Offsets by the message's OWN pivot instead of forcing one, so the prefab keeps control.
        void PlaceMessage(Rect local, float handTop)
        {
            var message = MessageRect;
            if (message == null) return;

            var bounds = Self.rect;
            var height = message.rect.height;
            var clear = Mathf.Max(local.yMax, handTop) + messageGap;
            var fitsAbove = clear + height <= bounds.yMax;

            var y = fitsAbove
                ? clear + height * message.pivot.y
                : local.yMin - messageGap - height * (1f - message.pivot.y);

            message.localPosition = new Vector3(0f, y, 0f);
        }

        // How far the art reaches above its own position. Measured from the corners because the
        // hand is turned right over between the two approaches — no single edge is always on top.
        float HandTopExtent()
        {
            hand.GetLocalCorners(handCorners);

            var top = float.NegativeInfinity;
            foreach (var corner in handCorners)
            {
                top = Mathf.Max(top, (hand.localRotation * Vector3.Scale(corner, hand.localScale)).y);
            }

            return top;
        }
    }
}
