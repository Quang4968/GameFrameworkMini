using TMPro;
using UnityEngine;

namespace Game.Game.Views.UI
{

    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class TextMirror : MonoBehaviour
    {
        [Tooltip("Chữ chính cần bám theo. Text trên object này bị ghi đè, đừng gõ tay vào nó.")]
        [SerializeField] private TMP_Text source;

        private TMP_Text mirror;

        private void Awake()
        {
            mirror = GetComponent<TMP_Text>();
            if (source == mirror)
            {
                Debug.LogError($"{nameof(TextMirror)} trên '{name}' đang lấy chính nó làm nguồn.", this);
                source = null;
            }
        }
       
        private void LateUpdate()
        {
            if (source == null) return;
            var current = source.text;
            source.text = current;
            mirror.text = current;
        }
    }
}
