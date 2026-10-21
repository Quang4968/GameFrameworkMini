using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Services;
using UnityEngine;

namespace Game.Game.Services
{
    public sealed class PlayerPrefsFeatureIntroductionStateService : IFeatureIntroductionStateService
    {
        public const string PrefsKeyPrefix = "Game.FeatureIntroduction.";

        readonly HashSet<GameFeature> shown = new();

        public event Action<GameFeature> Shown;

        public PlayerPrefsFeatureIntroductionStateService()
        {
            foreach (GameFeature feature in Enum.GetValues(typeof(GameFeature)))
            {
                if (PlayerPrefs.GetInt(KeyOf(feature), 0) != 0) shown.Add(feature);
            }
        }

        public bool HasShown(GameFeature feature) => shown.Contains(feature);

        public void MarkShown(GameFeature feature)
        {
            if (!shown.Add(feature)) return;

            PlayerPrefs.SetInt(KeyOf(feature), 1);
            PlayerPrefs.Save();
            Shown?.Invoke(feature);
        }

        static string KeyOf(GameFeature feature) => PrefsKeyPrefix + feature;
    }
}
