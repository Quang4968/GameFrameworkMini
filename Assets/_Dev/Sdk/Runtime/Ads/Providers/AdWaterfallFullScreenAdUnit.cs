using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace MoodBoost.Sdk.Ads.Providers
{
    // Arranges several mediation networks into one waterfall for ONE format, falling through to the next network when
    // the one above cannot load.
    //
    // To AdService this is still an ordinary IFullScreenAdUnit - it does not know whether there are one or five
    // networks underneath. That is what keeps the frequency rules, the retry loop and the three Show* methods unchanged.
    //
    // HOW IT WORKS: loading is SEQUENTIAL, not parallel. Firing requests to every network at once makes each of them
    // count an "opportunity that did not show", and the fill rate of both accounts drops. So a lower network is only
    // asked once the one above has answered that it has no inventory.
    //
    // Wraps even a SINGLE network. An "if there is only one, return that unit directly" branch would create two
    // different paths for the same job, and the less-travelled path is the one nobody checks.
    sealed class AdWaterfallFullScreenAdUnit : IFullScreenAdUnit
    {
        const string Tag = "AdWaterfall";

        readonly List<Step> steps;
        readonly StringBuilder errors = new StringBuilder();

        int cursor;
        bool loading;

        public AdFormat Format { get; }

        // The id of the network currently holding inventory, for logs and for AdService to cross-check. When no network
        // has inventory, it reports the first one - the one that will be asked first on the next load.
        public string AdUnitId
        {
            get
            {
                var ready = FindReady();
                return ready?.Unit.AdUnitId ?? steps[0].Unit.AdUnitId;
            }
        }

        public bool IsReady => FindReady() != null;

        public event Action Loaded;
        public event Action<string> LoadFailed;
        public event Action<AdImpression> ImpressionRecorded;
        public event Action Clicked;

        public AdWaterfallFullScreenAdUnit(AdFormat format, IReadOnlyList<(MediationId Id, IFullScreenAdUnit Unit)> units)
        {
            Format = format;
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
            // In declaration order: an upper network wins even when a lower one also has inventory.
            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i].Unit.IsReady) return steps[i];
            }

            return null;
        }

        public void Load()
        {
            if (loading || IsReady) return;

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

                // If this network already has inventory, treat it as loaded. Calling Load() on a unit that already has
                // an ad is a no-op on every adapter, and it raises no event - leaving the waterfall stuck there.
                if (step.Unit.IsReady)
                {
                    loading = false;
                    Loaded?.Invoke();
                    return;
                }

                step.Unit.Load();
                return;
            }

            // Out of networks to ask. Return a combined message so the log can say which network failed and why -
            // a bare "no fill" gives no way to trace where the fix belongs.
            loading = false;
            LoadFailed?.Invoke(errors.Length == 0 ? "no mediation configured" : errors.ToString());
        }

        void OnStepLoaded(int index)
        {
            // Ignore late signals from a previous load pass: a network already passed over can still answer later, and
            // accepting it here makes the waterfall jump around.
            if (!loading || index != cursor) return;

            loading = false;
            Loaded?.Invoke();
        }

        void OnStepLoadFailed(int index, string error)
        {
            if (!loading || index != cursor) return;

            if (errors.Length > 0) errors.Append(" | ");
            errors.Append(steps[index].Id).Append(": ").Append(error ?? "unknown");

            SdkLog.Info(Tag, $"{Format} {steps[index].Id} has no inventory ({error}) - trying the next network");

            cursor++;
            LoadCursor();
        }

        public UniTask<AdResult> ShowAsync(string placement, CancellationToken ct)
        {
            var step = FindReady();
            if (step == null) return UniTask.FromResult(AdResult.NotReady);

            SdkLog.Info(Tag, $"{Format} shown by {step.Id}");
            return step.Unit.ShowAsync(placement, ct);
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
            LoadFailed = null;
            ImpressionRecorded = null;
            Clicked = null;
        }

        // One rung of the waterfall. Keeps the delegate around so the event can be unsubscribed - registering with a
        // lambda and losing the reference is the classic leak, and here it would keep a whole network ad unit alive.
        sealed class Step
        {
            readonly AdWaterfallFullScreenAdUnit owner;
            readonly int index;

            readonly Action onLoaded;
            readonly Action<string> onLoadFailed;
            readonly Action<AdImpression> onImpression;
            readonly Action onClicked;

            public readonly MediationId Id;
            public readonly IFullScreenAdUnit Unit;

            public Step(AdWaterfallFullScreenAdUnit owner, int index, MediationId id, IFullScreenAdUnit unit)
            {
                this.owner = owner;
                this.index = index;
                Id = id;
                Unit = unit;

                onLoaded = () => this.owner.OnStepLoaded(this.index);
                onLoadFailed = error => this.owner.OnStepLoadFailed(this.index, error);

                // Stamp the network name onto the impression HERE, not in each adapter. An adapter does not know which
                // rung of the waterfall it sits on, and revenue has to land in the right account - merging two networks
                // by mistake gives wrong ROAS data with no way to separate it again.
                onImpression = impression => this.owner.ImpressionRecorded?.Invoke(impression.WithMediation(Id));
                onClicked = () => this.owner.Clicked?.Invoke();
            }

            public void Subscribe()
            {
                Unit.Loaded += onLoaded;
                Unit.LoadFailed += onLoadFailed;
                Unit.ImpressionRecorded += onImpression;
                Unit.Clicked += onClicked;
            }

            public void Unsubscribe()
            {
                Unit.Loaded -= onLoaded;
                Unit.LoadFailed -= onLoadFailed;
                Unit.ImpressionRecorded -= onImpression;
                Unit.Clicked -= onClicked;
            }
        }
    }
}
