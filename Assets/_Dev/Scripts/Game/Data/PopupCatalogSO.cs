using System;
using Game.Game.Views.Popup;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Game.Game.Data
{
    // Bảng tra PopupId -> địa chỉ prefab. Đây là chỗ DUY NHẤT biết popup nào ứng với prefab nào,
    // nhờ vậy code gọi popup chỉ cần cầm cái enum.
    //
    // Giữ AssetReference chứ KHÔNG giữ prefab trực tiếp: tham chiếu cứng sẽ kéo mọi popup trong
    // catalog vào RAM ngay khi scene load, kể cả popup không bao giờ mở. AssetReference chỉ là
    // cái GUID, prefab thật do PopupHost nạp lúc mở lần đầu.
    [CreateAssetMenu(menuName = "Game/Popup Catalog", fileName = "PopupCatalog")]
    public sealed class PopupCatalogSO : ScriptableObject
    {
        [SerializeField] Entry[] popups = new Entry[0];

        [Serializable]
        public struct Entry
        {
            public PopupId id;

            [Tooltip("Prefab popup. Phải được đánh dấu Addressable, và root prefab phải có PopupView.")]
            public AssetReferenceGameObject prefab;
        }

        public AssetReferenceGameObject GetReference(PopupId id)
        {
            foreach (var entry in popups)
            {
                if (entry.id == id) return entry.prefab;
            }

            return null;
        }
    }
}
