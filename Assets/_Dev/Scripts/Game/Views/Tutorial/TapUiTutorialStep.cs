using UnityEngine;
using UnityEngine.UI;

namespace Game.Game.Views.Tutorial
{
    // Done when the button fires — the same click the game listens to, so the lesson can never
    // disagree with what actually happened.
    public sealed class TapUiTutorialStep : TutorialStep
    {
        [Tooltip("Nút người chơi phải bấm. Bước xong ngay khi nút này bắn onClick.")]
        [SerializeField] Button target;

        RectTransform targetRect;
        Canvas targetCanvas;

        protected override E_TutorialAction TrackedAction => E_TutorialAction.Click;

        void Awake()
        {
            if (target == null) return;

            targetRect = target.transform as RectTransform;

            // The button's OWN canvas: converting through the wrong one misplaces the hole.
            targetCanvas = target.GetComponentInParent<Canvas>();
        }

        protected override void OnBegin()
        {
            if (target != null) target.onClick.AddListener(OnTapped);
        }

        protected override void OnEnd()
        {
            if (target != null) target.onClick.RemoveListener(OnTapped);
        }

        protected override bool TryGetFocus(out Rect screenRect, out E_HandApproach approach)
        {
            approach = E_HandApproach.FromAbove;
            return TutorialScreenRect.TryFromUiTarget(targetRect, targetCanvas, out screenRect);
        }

        void OnTapped() => Finish();
    }
}
