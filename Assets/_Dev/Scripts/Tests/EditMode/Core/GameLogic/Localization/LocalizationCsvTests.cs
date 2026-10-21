using System.Linq;
using NUnit.Framework;
using Game.Core.GameLogic.Localization;

namespace Game.Core.Tests.GameLogic.Localization
{
    public class LocalizationCsvTests
    {
        const string Header = "key,note,en,pt-BR\n";

        [Test]
        public void Parse_LanguageColumns_SkipNoteColumn()
        {
            var sheet = LocalizationCsv.Parse(Header + "play,Home button,Play,Jogar\n", "en");
            CollectionAssert.AreEqual(new[] { "en", "pt-BR" }, sheet.Languages);
            Assert.AreEqual("Jogar", sheet.Table("pt-BR")["play"]);
            Assert.IsEmpty(sheet.Issues);
        }

        [Test]
        public void Parse_QuotedCell_KeepsCommasQuotesAndLineBreaks()
        {
            var sheet = LocalizationCsv.Parse(Header + "msg,,\"Wait, \"\"what\"\"?\nOk\",\"Espere\"\n", "en");
            Assert.AreEqual("Wait, \"what\"?\nOk", sheet.Table("en")["msg"]);
        }

        [Test]
        public void Parse_CrLfLineEndings_AreHandled()
        {
            var sheet = LocalizationCsv.Parse("key,en\r\na,A\r\nb,B\r\n", "en");
            Assert.AreEqual("B", sheet.Table("en")["b"]);
        }

        [Test]
        public void Parse_DuplicateKey_KeepsFirstAndReports()
        {
            var sheet = LocalizationCsv.Parse(Header + "play,,Play,Jogar\nplay,,Again,De novo\n", "en");
            Assert.AreEqual("Play", sheet.Table("en")["play"]);
            Assert.IsTrue(sheet.Issues.Any(i => i.Contains("duplicate")));
        }

        [Test]
        public void Parse_MissingTranslation_IsReported()
        {
            var sheet = LocalizationCsv.Parse(Header + "play,,Play,\n", "en");
            Assert.IsTrue(sheet.Issues.Any(i => i.StartsWith("pt-BR") && i.Contains("without a value")));
        }

        [Test]
        public void Parse_PlaceholderMismatch_IsReported()
        {
            var sheet = LocalizationCsv.Parse(Header + "level,,Level {0},Nível\n", "en");
            Assert.IsTrue(sheet.Issues.Any(i => i.Contains("placeholders")));
        }

        [Test]
        public void Parse_CommentRowsAndExtraLanguage_AreHandled()
        {
            var sheet = LocalizationCsv.Parse("key,en,pt-BR,vi\n# Home,,,\nplay,Play,Jogar,Chơi\n", "en");
            CollectionAssert.AreEqual(new[] { "en", "pt-BR", "vi" }, sheet.Languages);
            Assert.AreEqual(1, sheet.Entries("vi").Count());
        }

        [Test]
        public void Parse_WrongFirstHeader_IsRejected()
        {
            var sheet = LocalizationCsv.Parse("id,en\nplay,Play\n", "en");
            Assert.IsEmpty(sheet.Languages);
            Assert.IsNotEmpty(sheet.Issues);
        }

        [Test]
        public void PlaceholderCount_UsesHighestIndex()
        {
            Assert.AreEqual(3, LocalizationCsv.PlaceholderCount("{0} of {2:N0}"));
            Assert.AreEqual(0, LocalizationCsv.PlaceholderCount("Play"));
        }
    }
}
