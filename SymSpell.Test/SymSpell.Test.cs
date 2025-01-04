using System;
using System.IO;
using NUnit.Framework;

namespace symspell.Test
{
    [TestFixture]
    public class SymSpellTests
    {
        [Test]
        public void WordsWithSharedPrefixShouldRetainCounts()
        {
            var symSpell = new SymSpell(16, 1, 3);
            symSpell.CreateDictionaryEntry("pipe", 5);
            symSpell.CreateDictionaryEntry("pips", 10);
            var result = symSpell.Lookup("pipe", SymSpell.Verbosity.All, 1);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].term, Is.EqualTo("pipe"));
            Assert.That(result[0].count, Is.EqualTo(5));
            Assert.That(result[1].term, Is.EqualTo("pips"));
            Assert.That(result[1].count, Is.EqualTo(10));
            result = symSpell.Lookup("pips", SymSpell.Verbosity.All, 1);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].term, Is.EqualTo("pips"));
            Assert.That(result[0].count, Is.EqualTo(10));
            Assert.That(result[1].term, Is.EqualTo("pipe"));
            Assert.That(result[1].count, Is.EqualTo(5));
            result = symSpell.Lookup("pip", SymSpell.Verbosity.All, 1);
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].term, Is.EqualTo("pips"));
            Assert.That(result[0].count, Is.EqualTo(10));
            Assert.That(result[1].term, Is.EqualTo("pipe"));
            Assert.That(result[1].count, Is.EqualTo(5));
        }

        [Test]
        public void AddAdditionalCountsShouldNotAddWordAgain()
        {
            var symSpell = new SymSpell();
            var word = "hello";
            symSpell.CreateDictionaryEntry(word, 11);
            Assert.That(symSpell.WordCount, Is.EqualTo(1));
            symSpell.CreateDictionaryEntry(word, 3);
            Assert.That(symSpell.WordCount, Is.EqualTo(1));
        }
        [Test]
        public void AddAdditionalCountsShouldIncreaseCount()
        {
            var symSpell = new SymSpell();
            var word = "hello";
            symSpell.CreateDictionaryEntry(word, 11);
            var result = symSpell.Lookup(word, SymSpell.Verbosity.Top);
            long count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.That(count, Is.EqualTo(11));
            symSpell.CreateDictionaryEntry(word, 3);
            result = symSpell.Lookup(word, SymSpell.Verbosity.Top);
            count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.That(count, Is.EqualTo(11+3));
        }
        [Test]
        public void AddAdditionalCountsShouldNotOverflow()
        {
            var symSpell = new SymSpell();
            var word = "hello";
            symSpell.CreateDictionaryEntry(word, long.MaxValue - 10);
            var result = symSpell.Lookup(word, SymSpell.Verbosity.Top);
            long count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.That(count, Is.EqualTo(long.MaxValue - 10));
            symSpell.CreateDictionaryEntry(word, 11);
            result = symSpell.Lookup(word, SymSpell.Verbosity.Top);
            count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.That(count, Is.EqualTo(long.MaxValue));
        }
        [Test]
        public void VerbosityShouldControlLookupResults()
        {
            var symSpell = new SymSpell();
            symSpell.CreateDictionaryEntry("steam", 1);
            symSpell.CreateDictionaryEntry("steams", 2);
            symSpell.CreateDictionaryEntry("steem", 3);
            var result = symSpell.Lookup("steems", SymSpell.Verbosity.Top, 2);
            Assert.That(result.Count, Is.EqualTo(1));
            result = symSpell.Lookup("steems", SymSpell.Verbosity.Closest, 2);
            Assert.That(result.Count, Is.EqualTo(2));
            result = symSpell.Lookup("steems", SymSpell.Verbosity.All, 2);
            Assert.That(result.Count, Is.EqualTo(3));
        }
        [Test]
        public void LookupShouldReturnMostFrequent()
        {
            var symSpell = new SymSpell();
            symSpell.CreateDictionaryEntry("steama", 4);
            symSpell.CreateDictionaryEntry("steamb", 6);
            symSpell.CreateDictionaryEntry("steamc", 2);
            var result = symSpell.Lookup("steam", SymSpell.Verbosity.Top, 2);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].term, Is.EqualTo("steamb"));
            Assert.That(result[0].count, Is.EqualTo(6));
        }
        [Test]
        public void LookupShouldFindExactMatch()
        {
            var symSpell = new SymSpell();
            symSpell.CreateDictionaryEntry("steama", 4);
            symSpell.CreateDictionaryEntry("steamb", 6);
            symSpell.CreateDictionaryEntry("steamc", 2);
            var result = symSpell.Lookup("steama", SymSpell.Verbosity.Top, 2);
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].term, Is.EqualTo("steama"));
        }
        [Test]
        public void LookupShouldNotReturnNonWordDelete()
        {
            var symSpell = new SymSpell(16, 2, 7, 10);
            symSpell.CreateDictionaryEntry("pawn", 10);
            var result = symSpell.Lookup("paw", SymSpell.Verbosity.Top, 0);
            Assert.That(result.Count, Is.EqualTo(0));
            result = symSpell.Lookup("awn", SymSpell.Verbosity.Top, 0);
             Assert.That(result.Count, Is.EqualTo(0));
        }
        [Test]
        public void LookupShouldNotReturnLowCountWord()
        {
            var symSpell = new SymSpell(16, 2, 7, 10);
            symSpell.CreateDictionaryEntry("pawn", 1);
            var result = symSpell.Lookup("pawn", SymSpell.Verbosity.Top, 0);
             Assert.That(result.Count, Is.EqualTo(0));
        }
        [Test]
        public void LookupShouldNotReturnLowCountWordThatsAlsoDeleteWord()
        {
            var symSpell = new SymSpell(16, 2, 7, 10);
            symSpell.CreateDictionaryEntry("flame", 20);
            symSpell.CreateDictionaryEntry("flam", 1);
            var result = symSpell.Lookup("flam", SymSpell.Verbosity.Top, 0);
            Assert.That(result.Count, Is.EqualTo(0));
        }
        //[Test]
        //public void DeleteInSuggestionPrefixEdgeCases()
        //{
        //    var symSpell = new SymSpell();
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("ab", "abcdef", 6));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("ab", "abcdef", 2));
        //    Assert.IsFalse(symSpell.DeleteInSuggestionPrefix("ab", "abcdef", 1));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("ab", "aaaaab", 6));
        //    Assert.IsFalse(symSpell.DeleteInSuggestionPrefix("ab", "aaaaab", 5));
        //    Assert.IsFalse(symSpell.DeleteInSuggestionPrefix("ab", "bacdef", 6));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("adf", "abcdef", 6));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("ef", "abcdef", 6));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("a", "abcdef", 6));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("f", "abcdef", 6));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("a", "a", 1));
        //    Assert.IsFalse(symSpell.DeleteInSuggestionPrefix("abc", "ab", 2));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("boo", "taboo", 5));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("boo", "broto", 5));
        //    Assert.IsTrue(symSpell.DeleteInSuggestionPrefix("abba", "cacbcbca", 8));
        //}
        [Test]
        public void LookupShouldReplicateNoisyResults()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;

            const int editDistanceMax = 2;
            const int prefixLength = 7;
            const SymSpell.Verbosity verbosity = SymSpell.Verbosity.Closest;
            var symSpell = new SymSpell(83000, editDistanceMax, prefixLength);
            string path = dir + "../../../../SymSpell/frequency_dictionary_en_82_765.txt";    //for spelling correction (genuine English words)

            symSpell.LoadDictionary(path, 0, 1); 

            //load 1000 terms with random spelling errors
            string[] testList = new string[1000];
            int i = 0;
            using (StreamReader sr = new StreamReader(File.OpenRead(dir + "../../../../SymSpell.Benchmark/test_data/noisy_query_en_1000.txt")))
            {
                String line;
                //process a single line at a time only for memory efficiency
                while ((line = sr.ReadLine()) != null)
                {
                    string[] lineParts = line.Split(null);
                    if (lineParts.Length >= 2)
                    {
                        testList[i++] = lineParts[0];
                    }
                }
            }

            int resultSum = 0;
            for (i = 0; i < testList.Length; i++)
            {
                resultSum += symSpell.Lookup(testList[i], verbosity, symSpell.MaxDictionaryEditDistance ).Count;
            }
            Assert.That(resultSum, Is.EqualTo(4955));
        }
    }
}
