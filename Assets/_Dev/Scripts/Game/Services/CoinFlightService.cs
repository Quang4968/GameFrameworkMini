using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Services;
using Game.Game.Animation;
using Game.Game.Data;
using UnityEngine;

namespace Game.Game.Services
{
    // Coins fly in a layer canvas of their own, built lazily and torn down with the scope, so any
    // screen can throw a burst without authoring a container above its UI.
    public sealed class CoinFlightService : I_CoinFlightService, IDisposable
    {
        readonly CoinFlightSettingsSO settings;
        readonly IObjectPoolService pool;
        readonly IAudioService audio;
        readonly List<I_CoinFlightTarget> targets = new();
        readonly Action<FlyingCoin> landed;

        RectTransform layer;
        int holds;
        bool warnedMissingPool;
        bool disposed;

        public CoinFlightService(CoinFlightSettingsSO settings, IObjectPoolService pool, IAudioService audio)
        {
            this.settings = settings;
            this.pool = pool;
            this.audio = audio;
            landed = Land;
        }

        public async UniTask PlayAsync(RectTransform source, int multiplier, CancellationToken ct)
        {
            if (settings == null || settings.CoinPrefab == null || settings.LayerPrefab == null) return;

            var target = CurrentTarget();
            if (target == null || !EnsureLayer()) return;

            var origin = ToLayer(source != null ? source : target.Anchor);
            var destination = ToLayer(target.Anchor);

            var count = settings.CoinCount(multiplier);
            var spread = settings.Spread(count);

            using (HoldCounters())
            {
                audio?.Play(AudioCue.CoinAppear);

                for (var i = 0; i < count; i++)
                {
                    var departure = settings.ScatterDuration + (count > 1 ? spread * i / (count - 1) : 0f);
                    Launch(new CoinThrow(origin, destination, settings.RollVelocity(), settings.RollTumble(),
                        departure, settings.FlyDuration, settings.Gravity, settings.Drag, settings.FlyDip,
                        settings.SpawnScale, settings.HoverScale, settings.EndScale, settings.ScatterDuration * 0.35f));
                }

                // Unscaled: the win popup stops the level clock while the coins are in the air.
                await UniTask.Delay(TimeSpan.FromSeconds(settings.TotalDuration(count)),
                    DelayType.UnscaledDeltaTime, cancellationToken: ct).SuppressCancellationThrow();
            }
        }

        public IDisposable HoldCounters()
        {
            if (holds++ == 0) SetHeld(true);
            return new Hold(this);
        }

        public void AddTarget(I_CoinFlightTarget target)
        {
            if (target == null) return;

            targets.Remove(target);
            targets.Add(target);
            target.SetHeld(holds > 0);
        }

        public void RemoveTarget(I_CoinFlightTarget target) => targets.Remove(target);

        public void Dispose()
        {
            if (disposed) return;

            disposed = true;
            if (layer != null) UnityEngine.Object.Destroy(layer.gameObject);
        }

        void Launch(in CoinThrow path)
        {
            var prefab = settings.CoinPrefab.gameObject;
            Transform instance = null;

            if (pool != null && pool.IsReady && pool.Contains(prefab))
            {
                instance = pool.Spawn(prefab, Vector3.zero, Quaternion.identity);
            }
            else if (!warnedMissingPool)
            {
                warnedMissingPool = true;
                Debug.LogWarning($"[{nameof(CoinFlightService)}] '{prefab.name}' is not in this scene's " +
                    "PoolCatalog; falling back to Instantiate/Destroy.");
            }

            if (instance == null) instance = UnityEngine.Object.Instantiate(prefab).transform;

            instance.SetParent(layer, false);
            instance.GetComponent<FlyingCoin>().Launch(path, landed);
        }

        void Land(FlyingCoin coin)
        {
            if (coin == null) return;

            audio?.Play(AudioCue.CoinCollect);
            if (pool == null || !pool.Despawn(coin.transform)) UnityEngine.Object.Destroy(coin.gameObject);
        }

        void Release()
        {
            if (holds > 0 && --holds == 0) SetHeld(false);
        }

        void SetHeld(bool held)
        {
            for (var i = targets.Count - 1; i >= 0; i--) targets[i]?.SetHeld(held);
        }

        I_CoinFlightTarget CurrentTarget()
        {
            for (var i = targets.Count - 1; i >= 0; i--)
            {
                var anchor = targets[i]?.Anchor;
                if (anchor != null && anchor.gameObject.activeInHierarchy) return targets[i];
            }

            return null;
        }

        bool EnsureLayer()
        {
            if (disposed) return false;
            if (layer != null) return true;

            var instance = UnityEngine.Object.Instantiate(settings.LayerPrefab);
            instance.name = settings.LayerPrefab.name;
            layer = (RectTransform)instance.transform;
            return true;
        }

        // Any canvas mode to the layer: world centre -> screen through the source canvas camera -> layer local.
        Vector2 ToLayer(RectTransform rect)
        {
            var canvas = rect.GetComponentInParent<Canvas>();
            var root = canvas != null ? canvas.rootCanvas : null;
            var camera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;

            var screen = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(layer, screen, null, out var local);
            return local;
        }

        sealed class Hold : IDisposable
        {
            CoinFlightService owner;

            public Hold(CoinFlightService owner) => this.owner = owner;

            public void Dispose()
            {
                owner?.Release();
                owner = null;
            }
        }
    }
}
