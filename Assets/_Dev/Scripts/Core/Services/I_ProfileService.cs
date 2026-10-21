using System;

namespace Game.Core.Services
{
    // The display name and the avatar the player picked for themselves.
    //
    // Core only ever knows the avatar as an index; which sprite that index means is a table on
    // the Game side (AvatarCatalogSO), the same split as PopupId -> prefab.
    public interface I_ProfileService
    {
        string DisplayName { get; }
        int AvatarIndex { get; }

        event Action<string> DisplayNameChanged;
        event Action<int> AvatarIndexChanged;

        // Both setters take raw input and store the sanitised value, so callers never have to
        // trim or clamp first — read the property back to see what was actually kept.
        void SetDisplayName(string displayName);
        void SetAvatarIndex(int index);
    }
}
