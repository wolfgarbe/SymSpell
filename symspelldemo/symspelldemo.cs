using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

// uses SymSpell.cs 
// *alternatively* use SymSpell as NuGet package from https://www.nuget.org/packages/symspell

// Usage: single word + Enter:  Display spelling suggestions
//        Enter without input:  Terminate the program

namespace symspelldemo
{
    class Program
    {
        //Load a frequency dictionary or create a frequency dictionary from a text corpus
        public static void Main(string[] args)
        {
            //set global parameters
            const int verbose = 0;
            const int editDistanceMax = 2;
            const int prefixLength = 7;

            Console.Write("Creating dictionary ...");
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var symSpell = new SymSpell(editDistanceMax, prefixLength);
            symSpell.DefaultVerbose = verbose;

            //Load a frequency dictionary
            //wordfrequency_en.txt  ensures high correction quality by combining two data sources: 
            //Google Books Ngram data  provides representative word frequencies (but contains many entries with spelling errors)  
            //SCOWL — Spell Checker Oriented Word Lists which ensures genuine English vocabulary (but contained no word frequencies)       
            //string path = "../../../symspelldemo/test_data/frequency_dictionary_en_30_000.txt"; //for benchmark only (contains also non-genuine English words)
            //string path = "../../../symspelldemo/test_data/frequency_dictionary_en_500_000.txt"; //for benchmark only (contains also non-genuine English words)
            string path = "../../../symspell/frequency_dictionary_en_82_765.txt";    //for spelling correction (genuine English words)
                                                                                     //string path = "../../frequency_dictionary_en_82_765.txt";  //path when using symspell nuget package (frequency_dictionary_en_82_765.txt is included in nuget package)
            if (!symSpell.LoadDictionary(path, 0, 1)) Console.Error.WriteLine("File not found: " + Path.GetFullPath(path)); //path when using symspell.cs

            //Alternatively Create the dictionary from a text corpus (e.g. http://norvig.com/big.txt ) 
            //Make sure the corpus does not contain spelling errors, invalid terms and the word frequency is representative to increase the precision of the spelling correction.
            //You may use SymSpell.CreateDictionaryEntry() to update a (self learning) dictionary incrementally
            //To extend spelling correction beyond single words to phrases (e.g. correcting "unitedkingom" to "united kingdom") simply add those phrases with CreateDictionaryEntry(). or use  https://github.com/wolfgarbe/SymSpellCompound
            //string path = "big.txt";
            //if (!symSpell.CreateDictionary(path)) Console.Error.WriteLine("File not found: " + Path.GetFullPath(path));

            stopWatch.Stop();
            Console.WriteLine("\rDictionary: " + symSpell.Count.ToString("N0") + " words, "
                + symSpell.EntryCount.ToString("N0") + " entries, edit distance=" + symSpell.MaxDictionaryEditDistance.ToString()
                + " in " + stopWatch.Elapsed.TotalMilliseconds.ToString("0.0") + "ms "
                + (Process.GetCurrentProcess().PrivateMemorySize64 / 1000000).ToString("N0") + " MB");

            Benchmark("../../../symspelldemo/test_data/noisy_query_en_1000.txt", 1000, symSpell);

            string input;
            while (!string.IsNullOrEmpty(input = (Console.ReadLine() ?? "").Trim()))
            {
                Correct(input, symSpell);
            }
        }

        public static void Benchmark(string path, int testNumber, SymSpell symSpell)
        {
            int resultSum = 0; 
            string[] testList = new string[testNumber];
            List<SymSpell.SuggestItem> suggestions = null;

            //load 1000 terms with random spelling errors
            int i = 0;
            using (StreamReader sr = new StreamReader(File.OpenRead(path)))
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

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //perform n rounds of Lookup of 1000 terms with random spelling errors
            int rounds = 10;
            for (int j = 0; j < rounds; j++)
            {
                resultSum = 0;
                //spellcheck strings
                for (i = 0; i < testNumber; i++)
                {
                    suggestions = symSpell.Lookup(testList[i], symSpell.MaxDictionaryEditDistance); 
                    resultSum += suggestions.Count;
                }
            }
            stopWatch.Stop();
            Console.WriteLine(resultSum.ToString("N0")+" results in "+(stopWatch.Elapsed.TotalMilliseconds/rounds).ToString("0.000") + " ms");
        }

        public static void Correct(string input, SymSpell symSpell)
        {
            List<SymSpell.SuggestItem> suggestions = null;

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //check if input term or similar terms within edit-distance are in dictionary, return results sorted by ascending edit distance, then by descending word frequency     
            suggestions = symSpell.Lookup(input, 2);// symSpell.EditDistanceMax);

            stopWatch.Stop();
            Console.WriteLine(stopWatch.Elapsed.TotalMilliseconds.ToString("0.000")+" ms");

            //display term and frequency
            foreach (var suggestion in suggestions)
            {
                Console.WriteLine( suggestion.term + " " + suggestion.distance.ToString() + " " + suggestion.count.ToString("N0"));
            }
            if (symSpell.DefaultVerbose != 0) Console.WriteLine(suggestions.Count.ToString() + " suggestions");
        }
    }
}
