using NUnit.Framework;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class CoinFormatTests
    {
        [TestCase(0, "0")]
        [TestCase(999, "999")]
        [TestCase(1000, "1K")]
        [TestCase(1500, "1.5K")]
        [TestCase(10000, "10K")]
        [TestCase(12345, "12.3K")]
        [TestCase(123456, "123K")]
        [TestCase(1000000, "1M")]
        [TestCase(2500000, "2.5M")]
        [TestCase(1000000000, "1B")]
        public void Short_ShortensPerStep(int coins, string expected)
        {
            Assert.AreEqual(expected, CoinFormat.Short(coins));
        }

        [Test]
        public void Short_TruncatesInsteadOfRoundingUp()
        {
            Assert.AreEqual("999K", CoinFormat.Short(999999));
        }

        [Test]
        public void Short_KeepsTheSignOnNegatives()
        {
            Assert.AreEqual("-1.5K", CoinFormat.Short(-1500));
        }
    }
}
