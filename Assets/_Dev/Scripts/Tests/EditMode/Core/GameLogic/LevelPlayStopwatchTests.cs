using System;
using NUnit.Framework;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class LevelPlayStopwatchTests
    {
        static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static DateTime At(int seconds) => T0.AddSeconds(seconds);

        [Test]
        public void NotStarted_IsZero()
        {
            Assert.AreEqual(0, new LevelPlayStopwatch().ElapsedMs(At(10)));
        }

        [Test]
        public void Running_CountsWallTime()
        {
            var stopwatch = new LevelPlayStopwatch();
            stopwatch.Start(At(0));

            Assert.AreEqual(5000, stopwatch.ElapsedMs(At(5)));
        }

        [Test]
        public void PausedSpan_IsExcluded()
        {
            var stopwatch = new LevelPlayStopwatch();
            stopwatch.Start(At(0));
            stopwatch.Pause(At(2));
            stopwatch.Resume(At(10));

            Assert.AreEqual(5000, stopwatch.ElapsedMs(At(13)));
        }

        [Test]
        public void NestedPauses_StayPausedUntilTheLastResume()
        {
            var stopwatch = new LevelPlayStopwatch();
            stopwatch.Start(At(0));
            stopwatch.Pause(At(1));
            stopwatch.Pause(At(2));
            stopwatch.Resume(At(3));

            Assert.AreEqual(1000, stopwatch.ElapsedMs(At(9)), "An ad closing over the lose popup must not restart the clock.");

            stopwatch.Resume(At(9));

            Assert.AreEqual(2000, stopwatch.ElapsedMs(At(10)));
        }

        [Test]
        public void ResumeWithoutPause_IsIgnored()
        {
            var stopwatch = new LevelPlayStopwatch();
            stopwatch.Start(At(0));
            stopwatch.Resume(At(1));
            stopwatch.Pause(At(2));

            Assert.AreEqual(2000, stopwatch.ElapsedMs(At(5)));
        }

        [Test]
        public void PauseHeldBeforeStart_KeepsTheClockStopped()
        {
            var stopwatch = new LevelPlayStopwatch();
            stopwatch.Pause(At(0));
            stopwatch.Start(At(1));

            Assert.AreEqual(0, stopwatch.ElapsedMs(At(4)));

            stopwatch.Resume(At(4));

            Assert.AreEqual(2000, stopwatch.ElapsedMs(At(6)));
        }

        [Test]
        public void ClockGoingBackwards_AddsNothing()
        {
            var stopwatch = new LevelPlayStopwatch();
            stopwatch.Start(At(10));

            Assert.AreEqual(0, stopwatch.ElapsedMs(At(0)));
        }
    }
}
