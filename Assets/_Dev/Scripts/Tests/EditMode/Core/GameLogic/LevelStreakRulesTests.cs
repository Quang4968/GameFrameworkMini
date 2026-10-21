using NUnit.Framework;
using Game.Core.Domain;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class LevelStreakRulesTests
    {
        [Test]
        public void OnWin_FromZero_GivesOneWin()
        {
            var streak = LevelStreakRules.OnWin(LevelStreak.Zero);

            Assert.AreEqual(1, streak.Win);
            Assert.AreEqual(0, streak.Lose);
        }

        [Test]
        public void OnWin_AfterLosses_ClearsTheLoseStreak()
        {
            var streak = LevelStreakRules.OnWin(new LevelStreak(0, 3));

            Assert.AreEqual(1, streak.Win);
            Assert.AreEqual(0, streak.Lose, "Thắng một màn là kết thúc chuỗi thua, không phải cộng dồn song song.");
        }

        [Test]
        public void OnLose_AfterWins_ClearsTheWinStreak()
        {
            var streak = LevelStreakRules.OnLose(new LevelStreak(5, 0));

            Assert.AreEqual(0, streak.Win);
            Assert.AreEqual(1, streak.Lose);
        }

        [Test]
        public void ConsecutiveWins_Accumulate()
        {
            var streak = LevelStreakRules.OnWin(LevelStreakRules.OnWin(LevelStreakRules.OnWin(LevelStreak.Zero)));

            Assert.AreEqual(3, streak.Win);
        }

        [Test]
        public void ConsecutiveLosses_Accumulate()
        {
            var streak = LevelStreakRules.OnLose(LevelStreakRules.OnLose(LevelStreak.Zero));

            Assert.AreEqual(2, streak.Lose);
        }

        [Test]
        public void WinAndLose_AreNeverBothNonZero()
        {
            var streak = LevelStreak.Zero;

            // Xen kẽ thắng/thua bao nhiêu lần thì cũng chỉ một trong hai vế được khác 0.
            for (var i = 0; i < 6; i++)
            {
                streak = i % 2 == 0 ? LevelStreakRules.OnWin(streak) : LevelStreakRules.OnLose(streak);
                Assert.IsTrue(streak.Win == 0 || streak.Lose == 0,
                    "Hai chuỗi cùng khác 0 là trạng thái người chơi chưa từng ở trong.");
            }
        }

        [Test]
        public void RevivedLoss_ThenWin_ContinuesTheWinStreak()
        {
            // Đúng đường ILevelOutcomeSource đi: thắng 3 màn, thua màn thứ tư, hồi sinh (khôi phục
            // ảnh chụp trước khi thua), rồi thắng chính màn đó.
            var beforeLoss = new LevelStreak(3, 0);
            var afterLoss = LevelStreakRules.OnLose(beforeLoss);
            Assert.AreEqual(1, afterLoss.Lose);

            var afterRevive = beforeLoss;
            var afterWin = LevelStreakRules.OnWin(afterRevive);

            Assert.AreEqual(4, afterWin.Win, "Hồi sinh rồi thắng phải nối tiếp chuỗi thắng cũ.");
            Assert.AreEqual(0, afterWin.Lose, "Màn được cứu không được để lại một lần thua.");
        }

        [Test]
        public void Streak_ClampsNegativeStoredValuesToZero()
        {
            // PlayerPrefs sửa tay được, nên giá trị âm phải bị kẹp ngay trong kiểu.
            var streak = new LevelStreak(-4, -1);

            Assert.AreEqual(0, streak.Win);
            Assert.AreEqual(0, streak.Lose);
        }
    }
}
