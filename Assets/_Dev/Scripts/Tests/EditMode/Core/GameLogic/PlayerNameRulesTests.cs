using NUnit.Framework;
using Game.Core.GameLogic;

namespace Game.Core.Tests.GameLogic
{
    public class PlayerNameRulesTests
    {
        [Test]
        public void Sanitize_TrimsAndCollapsesWhitespace()
        {
            Assert.AreEqual("a b", PlayerNameRules.Sanitize("  a   b  "));
        }

        [Test]
        public void Sanitize_CutsAtMaxLength()
        {
            var typed = new string('x', PlayerNameRules.MaxLength + 6);

            Assert.AreEqual(PlayerNameRules.MaxLength, PlayerNameRules.Sanitize(typed).Length);
        }

        // A name cut mid-word must not keep the space that would have followed it.
        [Test]
        public void Sanitize_DoesNotEndOnASpaceWhenCut()
        {
            var typed = new string('x', PlayerNameRules.MaxLength - 1) + " yz";

            Assert.AreEqual(new string('x', PlayerNameRules.MaxLength - 1), PlayerNameRules.Sanitize(typed));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\t\n")]
        public void Sanitize_EmptyInput_FallsBack(string typed)
        {
            Assert.AreEqual(PlayerNameRules.FallbackName, PlayerNameRules.Sanitize(typed));
        }

        [Test]
        public void Sanitize_AlreadyCleanName_IsUnchanged()
        {
            Assert.AreEqual("Ninja Boss", PlayerNameRules.Sanitize("Ninja Boss"));
        }
    }
}
