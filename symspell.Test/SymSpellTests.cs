using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace symspell.Test
{
    [TestFixture]
    public class SymSpellTests
    {
        [Test]
        public void AddAdditionalCountsShouldNotAddWordAgain()
        {
            var symSpell = new SymSpell();
            var word = "hello";
            symSpell.CreateDictionaryEntry(word, 11);
            Assert.AreEqual(1, symSpell.Count);
            symSpell.CreateDictionaryEntry(word, 3);
            Assert.AreEqual(1, symSpell.Count);
        }
        [Test]
        public void AddAdditionalCountsShouldIncreaseCount()
        {
            var symSpell = new SymSpell();
            var word = "hello";
            symSpell.CreateDictionaryEntry(word, 11);
            var result = symSpell.Lookup(word, 0);
            long count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.AreEqual(11, count);
            symSpell.CreateDictionaryEntry(word, 3);
            result = symSpell.Lookup(word, 0);
            count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.AreEqual(11 + 3, count);
        }
        [Test]
        public void AddAdditionalCountsShouldNotOverflow()
        {
            var symSpell = new SymSpell();
            var word = "hello";
            symSpell.CreateDictionaryEntry(word, long.MaxValue - 10);
            var result = symSpell.Lookup(word, 0);
            long count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.AreEqual(long.MaxValue - 10, count);
            symSpell.CreateDictionaryEntry(word, 11);
            result = symSpell.Lookup(word, 0);
            count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.AreEqual(long.MaxValue, count);
        }
        [Test]
        public void VerboseShouldControlLookupResults()
        {
            var symSpell = new SymSpell();
            symSpell.CreateDictionaryEntry("steam", 1);
            symSpell.CreateDictionaryEntry("steams", 2);
            symSpell.CreateDictionaryEntry("steem", 3);
            var result = symSpell.Lookup("steems", 2, 0);
            Assert.AreEqual(1, result.Count);
            result = symSpell.Lookup("steems", 2, 1);
            Assert.AreEqual(2, result.Count);
            result = symSpell.Lookup("steems", 2, 2);
            Assert.AreEqual(3, result.Count);
        }
        [Test]
        public void LookupShouldReturnMostFrequent()
        {
            var symSpell = new SymSpell();
            symSpell.CreateDictionaryEntry("steama", 4);
            symSpell.CreateDictionaryEntry("steamb", 6);
            symSpell.CreateDictionaryEntry("steamc", 2);
            var result = symSpell.Lookup("steam", 2, 0);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("steamb", result[0].term);
            Assert.AreEqual(6, result[0].count);
        }
        [Test]
        public void LookupShouldFindExactMatch()
        {
            var symSpell = new SymSpell();
            symSpell.CreateDictionaryEntry("steama", 4);
            symSpell.CreateDictionaryEntry("steamb", 6);
            symSpell.CreateDictionaryEntry("steamc", 2);
            var result = symSpell.Lookup("steama", 2, 0);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("steama", result[0].term);
        }
        [Test]
        public void ShouldReplicateNoisyResults()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            const int editDistanceMax = 2;
            const int prefixLength = 7;
            const int verbose = 1;
            var symSpell = new SymSpell(editDistanceMax, prefixLength);
            string path = dir + "../../../symspell/frequency_dictionary_en_82_765.txt";    //for spelling correction (genuine English words)
            symSpell.LoadDictionary(path, 0, 1);

            int resultSum = 0;
            string[] testList = new string[1000];
            List<SymSpell.SuggestItem> suggestions = null;

            //load 1000 terms with random spelling errors
            int i = 0;
            using (StreamReader sr = new StreamReader(File.OpenRead(dir + "../../../symspelldemo/test_data/noisy_query_en_1000.txt")))
            {
                String line;
                //process a single line at a time only for memory efficiency
                while ((line = sr.ReadLine()) != null)
                {
                    string[] lineParts = line.Split(null);
                    if (lineParts.Length >= 2)
                    {
                        string key = lineParts[0];
                        testList[i++] = key;
                    }
                }
            }
            for (i = 0; i < testList.Length; i++)
            {
                suggestions = symSpell.Lookup(testList[i], symSpell.MaxDictionaryEditDistance, verbose);
                resultSum += suggestions.Count;
            }
            Assert.AreEqual(4945, resultSum);
        }
    }
}
