using System;
using System.Collections.Generic;
using System.Text;

namespace MoodBoost.Sdk.Ads.Providers
{
    // The banner/MREC version of the mediation waterfall. The same fall-through rules as the full-screen version, plus
    // exactly one constraint the other does not have:
    //
    // ONLY ONE FRAME MAY BE VISIBLE AT A TIME. Two banners from two networks overlapping is both a display bug and a
    // policy violation on both networks, and it happens very easily: a lower rung finishes loading late and shows
    // itself while an upper rung is already visible. So Show() always hides every other rung.
    sealed class AdWaterfallBannerAdUnit : IBannerAdUnit
    {
        const string Tag = "AdWaterfall";

        readonly AdFormat format;
        readonly List<Step> steps;
        readonly StringBuilder errors = new StringBuilder();

        int cursor;
        bool loading;
        bool visible;

        public string AdUnitId
        {
            get
            {
                var ready = FindReady();
                return ready?.Unit.AdUnitId ?? steps[0].Unit.AdUnitId;
            }
        }

        public bool IsLoaded => FindReady() != null;

        public event Action Loaded;
        public event Action<AdImpression> ImpressionRecorded;
        public event Action<string> LoadFailed;

        public AdWaterfallBannerAdUnit(AdFormat format, IReadOnlyList<(MediationId Id, IBannerAdUnit Unit)> units)
        {
            this.format = format;
            steps = new List<Step>(units.Count);

            for (var i = 0; i < units.Count; i++)
            {
                var step = new Step(this, i, units[i].Id, units[i].Unit);
                steps.Add(step);
                step.Subscribe();
            }
        }

        Step FindReady()
        {
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i].Unit.IsLoaded) return steps[i];
            }

            return null;
        }

        public void Load()
        {
            if (loading || IsLoaded) return;

            loading = true;
            cursor = 0;
            errors.Clear();

            LoadCursor();
        }

        void LoadCursor()
        {
            while (cursor < steps.Count)
            {
                var step = steps[cursor];

                if (step.Unit.IsLoaded)
                {
                    loading = false;
                    ApplyVisibility();
                    return;
                }

                step.Unit.Load();
                return;
            }

            loading = false;
            LoadFailed?.Invoke(errors.Length == 0 ? "no mediation configured" : errors.ToString());
        }

        void OnStepLoaded(int index)
        {
            if (loading && index == cursor) loading = false;

            // Not filtered by cursor like the full-screen version: a rung that loads late still has to be forced into
            // the correct shown/hidden state, otherwise it pops up on top of the rung currently showing.
            ApplyVisibility();

            Loaded?.Invoke();
        }

        void OnStepLoadFailed(int index, string error)
        {
            if (!loading || index != cursor) return;

            if (errors.Length > 0) errors.Append(" | ");
            errors.Append(steps[index].Id).Append(": ").Append(error ?? "unknown");

            SdkLog.Info(Tag, $"{format} {steps[index].Id} has no inventory ({error}) - trying the next network");

            cursor++;
            LoadCursor();
        }

        public void Show()
        {
            visible = true;
            ApplyVisibility();

            // If no network has inventory yet, start the waterfall. Show being called before Load is normal:
            // AdService builds the unit and then shows it immediately.
            if (!IsLoaded) Load();
        }

        public void Hide()
        {
            visible = false;
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            var winner = visible ? FindReady() : null;

            foreach (var step in steps)
            {
                if (step == winner) step.Unit.Show();
                else step.Unit.Hide();
            }
        }

        public void Dispose()
        {
            foreach (var step in steps)
            {
                step.Unsubscribe();
                step.Unit.Dispose();
            }

            steps.Clear();

            Loaded = null;
            ImpressionRecorded = null;
            LoadFailed = null;
        }

        sealed class Step
        {
            readonly AdWaterfallBannerAdUnit owner;
            readonly int index;

            readonly Action onLoaded;
            readonly Action<AdImpression> onImpression;
            readonly Action<string> onLoadFailed;

            public readonly MediationId Id;
            public readonly IBannerAdUnit Unit;

            public Step(AdWaterfallBannerAdUnit owner, int index, MediationId id, IBannerAdUnit unit)
            {
                this.owner = owner;
                this.index = index;
                Id = id;
                Unit = unit;

                onLoaded = () => this.owner.OnStepLoaded(this.index);
                onImpression = impression => this.owner.ImpressionRecorded?.Invoke(impression.WithMediation(Id));
                onLoadFailed = error => this.owner.OnStepLoadFailed(this.index, error);
            }

            public void Subscribe()
            {
                Unit.Loaded += onLoaded;
                Unit.ImpressionRecorded += onImpression;
                Unit.LoadFailed += onLoadFailed;
            }

            public void Unsubscribe()
            {
                Unit.Loaded -= onLoaded;
                Unit.ImpressionRecorded -= onImpression;
                Unit.LoadFailed -= onLoadFailed;
            }
        }
    }
}
