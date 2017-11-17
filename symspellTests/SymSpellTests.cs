using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace symspellTests
{
    [TestFixture]
    public class SymSpellTests
    {
        [Test]
        public void AddAdditionCountsShouldNotOverflow()
        {
            var word = "***************";
            SymSpell.CreateDictionaryEntry(word, "", long.MaxValue - 10);
            var result = SymSpell.Lookup(word, "", 0);
            long count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.AreEqual(long.MaxValue - 10, count);
            SymSpell.CreateDictionaryEntry(word, "", 11);
            result = SymSpell.Lookup(word, "", 0);
            count = 0;
            if (result.Count == 1) count = result[0].count;
            Assert.AreEqual(long.MaxValue, count);
        }
        [Test]
        public void ShouldReplicateNoisyResults()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            SymSpell.lp = 7;
            SymSpell.maxlength = 2;
            SymSpell.verbose = 1;
            string path = dir + "../../../symspell/frequency_dictionary_en_82_765.txt";    //for spelling correction (genuine English words)
            SymSpell.LoadDictionary(path, "", 0, 1);

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
                suggestions = SymSpell.Lookup(testList[i], "", SymSpell.editDistanceMax);
                resultSum += suggestions.Count;
            }
            Assert.AreEqual(4945, resultSum);
        }
    }
}
