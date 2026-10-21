using Game.Core.GameLogic;
using Game.Core.Services;

namespace Game.Game.Services
{
    // Wall clock rather than Time.unscaledTime: Unity resets that to 0 before Dispose runs on leaving Play Mode, and
    // level_exit logged a negative duration_play.
    public sealed class LevelPlayTimeService : I_LevelPlayTimeService
    {
        readonly IClockService _clock;
        readonly LevelPlayStopwatch _stopwatch = new LevelPlayStopwatch();

        public LevelPlayTimeService(IClockService clock)
        {
            _clock = clock;
        }

        public long PlayedMs => _stopwatch.ElapsedMs(_clock.UtcNow);

        public void Begin() => _stopwatch.Start(_clock.UtcNow);

        public void Pause() => _stopwatch.Pause(_clock.UtcNow);

        public void Resume() => _stopwatch.Resume(_clock.UtcNow);
    }
}
