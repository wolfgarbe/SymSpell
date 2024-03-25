using NUnit.Framework;
using System.Text.RegularExpressions;

namespace symspell.Test
{
    [TestFixture]
    public class SymSpellLookupCompoundTests
    {
        private SymSpell _symSpell;

        [OneTimeSetUp]
        public void Init()
        {
            _symSpell = new SymSpell();
            _symSpell.CreateDictionaryEntry("in", 5);
            _symSpell.CreateDictionaryEntry("the", 10);
            _symSpell.CreateDictionaryEntry("third", 10);
            _symSpell.CreateDictionaryEntry("quarter", 10);
            _symSpell.CreateDictionaryEntry("of", 10);
            _symSpell.CreateDictionaryEntry("last", 10);
            _symSpell.CreateDictionaryEntry("visit", 10);
            _symSpell.CreateDictionaryEntry("our", 10);
            _symSpell.CreateDictionaryEntry("offices", 10);
            _symSpell.CreateDictionaryEntry("last", 10);
            _symSpell.CreateDictionaryEntry("last", 10);
            _symSpell.CreateDictionaryEntry("a", 10);
        }
        
        [Test]
        public void SuggestWordsInDictionary_ReturnsCorrectedText()
        {
            var result = _symSpell.LookupCompound("in te dhird qarter oflast");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("in the third quarter of last", result[0].term);
        }

        [Test]
        public void NoSuggestForWord_ReturnsUnchanged()
        {
            var result = _symSpell.LookupCompound("in te dhird qarter oflast jear", 1);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("in the third quarter of last jear", result[0].term);
        }

        [Test]
        public void SplittedWord_ReturnsCorrectedWord()
        {
            var result = _symSpell.LookupCompound("in te dhird quar ter oflast");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("in the third quarter of last", result[0].term);
        }

        [Test]
        public void DigitsWithoutSkipFunction_Replaced()
        {
            var result = _symSpell.LookupCompound("visit our offices 24/7");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("visit our offices of a", result[0].term);
        }

        [Test]
        public void SkipDigitWords_ReturnsDigits()
        {
            var digitRegex = new Regex("^\\d+$", RegexOptions.Compiled);
            var result = _symSpell.LookupCompound("visit our offices 24/7", 2, digitRegex.IsMatch);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("visit our offices 24 7", result[0].term);
        }

        [Test]
        public void SplittedWordAndFirstPartSkiped_ReturnsSplitted()
        {
            var result = _symSpell.LookupCompound("in te dhird quar ter oflast", 2, (term) => term == "quar");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("in the third quar the of last", result[0].term);
        }

        [Test]
        public void SplittedWordAndSecondPartSkiped_ReturnsSplitted()
        {
            var result = _symSpell.LookupCompound("in te dhird quar ter oflast", 2, (term) => term == "ter");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("in the third our ter of last", result[0].term);
        }
    }
}