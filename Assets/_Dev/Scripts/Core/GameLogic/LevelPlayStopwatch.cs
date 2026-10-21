using System;

namespace Game.Core.GameLogic
{
    // Play time of one level with every paused span cut out.
    //
    // Pauses are counted, not flagged: a revive ad plays while the lose popup already holds the clock, and releasing
    // the ad must not restart it. A clock that jumps backwards adds nothing instead of subtracting.
    public sealed class LevelPlayStopwatch
    {
        TimeSpan _played;
        DateTime _runningSince;
        bool _started;
        int _pauses;

        public bool IsRunning => _started && _pauses == 0;

        // Pauses already held stay held, so a popup that opened before the level started still counts.
        public void Start(DateTime nowUtc)
        {
            _played = TimeSpan.Zero;
            _runningSince = nowUtc;
            _started = true;
        }

        public void Pause(DateTime nowUtc)
        {
            if (IsRunning) _played += Since(nowUtc);
            _pauses++;
        }

        public void Resume(DateTime nowUtc)
        {
            if (_pauses == 0) return;

            _pauses--;
            if (IsRunning) _runningSince = nowUtc;
        }

        public long ElapsedMs(DateTime nowUtc)
        {
            var played = IsRunning ? _played + Since(nowUtc) : _played;
            return (long)played.TotalMilliseconds;
        }

        TimeSpan Since(DateTime nowUtc) => nowUtc > _runningSince ? nowUtc - _runningSince : TimeSpan.Zero;
    }
}
