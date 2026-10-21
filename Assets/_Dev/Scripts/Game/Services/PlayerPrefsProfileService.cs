using System;
using Game.Core.GameLogic;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    // Stores the display name and the picked avatar in PlayerPrefs.
    //
    // The avatar count comes in from the installer instead of the catalog itself: the service is
    // plain state and must not depend on a ScriptableObject. A count of 0 means no catalog was
    // wired, so the index stays pinned at 0 rather than pointing at a sprite that is not there.
    public sealed class PlayerPrefsProfileService : I_ProfileService
    {
        public const string DisplayNamePrefsKey = "Game.DisplayName";
        public const string AvatarIndexPrefsKey = "Game.AvatarIndex";

        readonly int _avatarCount;

        public string DisplayName { get; private set; }
        public int AvatarIndex { get; private set; }

        public event Action<string> DisplayNameChanged;
        public event Action<int> AvatarIndexChanged;

        public PlayerPrefsProfileService(int avatarCount)
        {
            _avatarCount = Mathf.Max(0, avatarCount);

            DisplayName = PlayerNameRules.Sanitize(PlayerPrefs.GetString(DisplayNamePrefsKey, string.Empty));
            AvatarIndex = Clamp(PlayerPrefs.GetInt(AvatarIndexPrefsKey, 0));
        }

        public void SetDisplayName(string displayName)
        {
            var clean = PlayerNameRules.Sanitize(displayName);
            if (clean == DisplayName) return;

            DisplayName = clean;
            PlayerPrefs.SetString(DisplayNamePrefsKey, clean);
            PlayerPrefs.Save();
            DisplayNameChanged?.Invoke(clean);
        }

        public void SetAvatarIndex(int index)
        {
            var clamped = Clamp(index);
            if (clamped == AvatarIndex) return;

            AvatarIndex = clamped;
            PlayerPrefs.SetInt(AvatarIndexPrefsKey, clamped);
            PlayerPrefs.Save();
            AvatarIndexChanged?.Invoke(clamped);
        }

        int Clamp(int index) => _avatarCount <= 0 ? 0 : Mathf.Clamp(index, 0, _avatarCount - 1);
    }
}
