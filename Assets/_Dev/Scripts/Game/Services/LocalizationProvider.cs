using System;
using Game.Game.Data;
using UnityEngine;

namespace Game.Game.Services
{
    // The language outlives every scene and LocalizedText sits on objects no container injects
    // (scene labels, pooled effects, the loading screen), so the one service is held statically and
    // each scope registers this same instance (LocalizationInstaller).
    public static class LocalizationProvider
    {
        static PlayerPrefsLocalizationService instance;

        // Raised once, when the first scope (or Splash) builds the service.
        public static event Action<PlayerPrefsLocalizationService> Created;

        // Null until a scope with a LocalizationSettingsSO has been built.
        public static PlayerPrefsLocalizationService Current => instance;

        // A null asset gets a pass-through service that is not kept, so a later scope that has the
        // asset still turns localization on.
        public static PlayerPrefsLocalizationService GetOrCreate(LocalizationSettingsSO settings)
        {
            if (instance != null) return instance;
            if (settings == null) return new PlayerPrefsLocalizationService(null);

            instance = new PlayerPrefsLocalizationService(settings);
            Created?.Invoke(instance);
            return instance;
        }

        // With domain reload off, the previous Play session's service would otherwise survive.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            instance = null;
            Created = null;
        }
    }
}
