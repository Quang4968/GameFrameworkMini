using NUnit.Framework;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class LevelLoopRulesTests
    {
        // 20 màn, đoạn lặp là 5 màn cuối [15, 19].
        const int LevelCount = 20;
        const int LoopStart = 15;
        const int LoopEnd = 19;

        static int Resolve(int levelShow) =>
            LevelLoopRules.ResolveLoadIndex(levelShow, LevelCount, LoopStart, LoopEnd);

        [Test]
        public void LevelsBeforeTheLoopLoadThemselves()
        {
            Assert.AreEqual(0, Resolve(0));
            Assert.AreEqual(14, Resolve(14));
        }

        [Test]
        public void LastLapOfTheLoopStillLoadsItself()
        {
            Assert.AreEqual(LoopEnd, Resolve(LoopEnd));
        }

        [Test]
        public void PastTheLoopEndComesBackToTheLoopStart()
        {
            Assert.AreEqual(LoopStart, Resolve(LoopEnd + 1));
            Assert.AreEqual(LoopStart + 1, Resolve(LoopEnd + 2));
        }

        [Test]
        public void SecondLapRepeatsTheFirstOne()
        {
            Assert.AreEqual(Resolve(LoopEnd + 1), Resolve(LoopEnd + 1 + 5));
        }

        [Test]
        public void LoopOfOneLevelStaysOnThatLevel()
        {
            Assert.AreEqual(19, LevelLoopRules.ResolveLoadIndex(30, LevelCount, 19, 19));
        }

        [Test]
        public void LoopOffFallsBackToWrappingTheWholeDatabase()
        {
            Assert.AreEqual(3, LevelLoopRules.ResolveLoadIndex(23, LevelCount, 0, -1));
        }

        [Test]
        public void LoopEndOutsideTheDatabaseIsIgnored()
        {
            Assert.AreEqual(3, LevelLoopRules.ResolveLoadIndex(23, LevelCount, 15, LevelCount));
        }

        [Test]
        public void EmptyDatabaseAlwaysLoadsZero()
        {
            Assert.AreEqual(0, LevelLoopRules.ResolveLoadIndex(7, 0, 0, -1));
        }
    }
}
