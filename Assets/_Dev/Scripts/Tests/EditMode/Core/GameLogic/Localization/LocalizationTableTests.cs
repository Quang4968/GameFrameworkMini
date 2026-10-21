using System.Collections.Generic;
using NUnit.Framework;
using Game.Core.GameLogic.Localization;

namespace Game.Core.Tests.GameLogic.Localization
{
    public class LocalizationTableTests
    {
        static KeyValuePair<string, string> Pair(string key, string value) => new KeyValuePair<string, string>(key, value);

        static LocalizationTable Build()
        {
            var table = new LocalizationTable("en");
            table.SetTable("en", new[] { Pair("play", "Play"), Pair("level", "Level {0}"), Pair("only_en", "English") });
            table.SetTable("pt-BR", new[] { Pair("play", "Jogar"), Pair("level", "Nível {0}"), Pair("only_en", "") });
            return table;
        }

        [Test]
        public void Get_CurrentLanguageHasKey_ReturnsItsValue()
        {
            var table = Build();
            table.TrySetCurrent("pt-BR");
            Assert.AreEqual("Jogar", table.Get("play"));
        }

        [Test]
        public void Get_EmptyTranslation_FallsBackToSource()
        {
            var table = Build();
            table.TrySetCurrent("pt-BR");
            Assert.AreEqual("English", table.Get("only_en"));
        }

        [Test]
        public void Get_UnknownKey_ReturnsTheKey()
        {
            Assert.AreEqual("missing.key", Build().Get("missing.key"));
        }

        [Test]
        public void Get_EmptyKey_ReturnsEmpty()
        {
            Assert.AreEqual(string.Empty, Build().Get(""));
        }

        [Test]
        public void Format_WithArguments_FillsPlaceholders()
        {
            var table = Build();
            table.TrySetCurrent("pt-BR");
            Assert.AreEqual("Nível 12", table.Format("level", 12));
        }

        [Test]
        public void Format_MissingArgument_ReturnsUnformattedValue()
        {
            var table = new LocalizationTable("en");
            table.SetTable("en", new[] { Pair("pair", "{0} of {1}") });
            Assert.AreEqual("{0} of {1}", table.Format("pair", 3));
        }

        [Test]
        public void TrySetCurrent_SameOrUnknownLanguage_ReturnsFalse()
        {
            var table = Build();
            Assert.IsFalse(table.TrySetCurrent("en"));
            Assert.IsFalse(table.TrySetCurrent("fr"));
            Assert.IsTrue(table.TrySetCurrent("pt-BR"));
            Assert.AreEqual("pt-BR", table.Current);
        }

        [Test]
        public void Languages_KeepTheOrderTablesWereAdded()
        {
            CollectionAssert.AreEqual(new[] { "en", "pt-BR" }, Build().Languages);
        }
    }
}
