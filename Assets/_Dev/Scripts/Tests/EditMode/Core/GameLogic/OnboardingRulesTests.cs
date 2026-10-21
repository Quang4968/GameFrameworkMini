using NUnit.Framework;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class OnboardingRulesTests
    {
        // Level 10 on screen is index 9; level 11 is index 10.
        const int LastLevelIndex = OnboardingRules.LastLevelNumber - 1;

        [Test]
        public void IsOnboarding_CoversTheFirstTenLevelsAndStops()
        {
            Assert.IsTrue(OnboardingRules.IsOnboarding(0));
            Assert.IsTrue(OnboardingRules.IsOnboarding(LastLevelIndex));
            Assert.IsFalse(OnboardingRules.IsOnboarding(LastLevelIndex + 1));
        }

        [Test]
        public void SkipsHome_OnlyWhileTheRunIsOn()
        {
            Assert.IsTrue(OnboardingRules.SkipsHome(0));
            Assert.IsFalse(OnboardingRules.SkipsHome(LastLevelIndex + 1));
        }

        [Test]
        public void AllowsHome_IsClosedForTheWholeRun()
        {
            for (int levelShow = 0; levelShow <= LastLevelIndex; levelShow++)
            {
                Assert.IsFalse(OnboardingRules.AllowsHome(levelShow), $"Level {levelShow + 1} must have no exit.");
            }

            Assert.IsTrue(OnboardingRules.AllowsHome(LastLevelIndex + 1));
        }

        [Test]
        public void ShowsTopHud_HiddenOnTheFirstLevelOnly()
        {
            Assert.IsFalse(OnboardingRules.ShowsTopHud(0));
            Assert.IsTrue(OnboardingRules.ShowsTopHud(1));
            Assert.IsTrue(OnboardingRules.ShowsTopHud(LastLevelIndex));
        }

        [Test]
        public void ChargesLife_StartsOnlyAfterTheRun()
        {
            Assert.IsFalse(OnboardingRules.ChargesLife(0));
            Assert.IsFalse(OnboardingRules.ChargesLife(LastLevelIndex));
            Assert.IsTrue(OnboardingRules.ChargesLife(LastLevelIndex + 1));
        }

        [Test]
        public void OffersRewardedRevive_OnlyOnceTheRunIsOver()
        {
            Assert.IsFalse(OnboardingRules.OffersRewardedRevive(0));
            Assert.IsFalse(OnboardingRules.OffersRewardedRevive(LastLevelIndex));
            Assert.IsTrue(OnboardingRules.OffersRewardedRevive(LastLevelIndex + 1),
                "Level 11 is where the normal loop, ads included, takes over.");
        }

        [Test]
        public void ReturnsHomeAfterWin_OnTheLastLevelOnly()
        {
            Assert.IsFalse(OnboardingRules.ReturnsHomeAfterWin(LastLevelIndex - 1));
            Assert.IsTrue(OnboardingRules.ReturnsHomeAfterWin(LastLevelIndex));
            Assert.IsFalse(OnboardingRules.ReturnsHomeAfterWin(LastLevelIndex + 1));
        }
    }
}
