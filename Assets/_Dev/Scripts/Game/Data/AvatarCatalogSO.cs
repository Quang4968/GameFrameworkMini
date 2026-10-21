using UnityEngine;

namespace Game.Game.Data
{
    // Which sprite each avatar index means. This is the only place that mapping exists, so the
    // profile popup and the Home chip can never disagree about what avatar 3 looks like.
    //
    // The order is what gets persisted, so new avatars go at the END: inserting one shifts every
    // player already on a later index onto a different face, silently.
    [CreateAssetMenu(menuName = "Game/Avatar Catalog", fileName = "AvatarCatalog")]
    public sealed class AvatarCatalogSO : ScriptableObject
    {
        [Tooltip("Avatar faces in pick order. Appending is safe; reordering repaints every saved profile.")]
        [SerializeField] Sprite[] _avatars = new Sprite[0];

        public int Count => _avatars.Length;

        public Sprite GetSprite(int index)
        {
            if (_avatars.Length == 0) return null;

            return _avatars[Mathf.Clamp(index, 0, _avatars.Length - 1)];
        }
    }
}
