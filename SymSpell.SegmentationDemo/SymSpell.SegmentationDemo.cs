using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

// uses SymSpell.cs 
// *alternatively* use SymSpell as NuGet package from https://www.nuget.org/packages/symspell

// Usage: multiple words + Enter:  Display spelling suggestions
//        Enter without input:  Terminate the program

namespace symspell.SegmentationDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            //set parameters
            const int initialCapacity = 82765;
            const int maxEditDistance = 0;
            const int prefixLength = 7;
            SymSpell symSpell = new SymSpell(initialCapacity, maxEditDistance, prefixLength);

            Console.Write("Creating dictionary ...");
            long memSize = GC.GetTotalMemory(true);
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //Load a frequency dictionary
            //wordfrequency_en.txt  ensures high correction quality by combining two data sources: 
            //Google Books Ngram data  provides representative word frequencies (but contains many entries with spelling errors)  
            //SCOWL — Spell Checker Oriented Word Lists which ensures genuine English vocabulary (but contained no word frequencies)
            string path = AppDomain.CurrentDomain.BaseDirectory + "frequency_dictionary_en_82_765.txt"; //path referencing the SymSpell core project
            //string path = "../../frequency_dictionary_en_82_765.txt";  //path when using symspell nuget package (frequency_dictionary_en_82_765.txt is included in nuget package)
            if (!symSpell.LoadDictionary(path, 0, 1)) { Console.Error.WriteLine("\rFile not found: " + Path.GetFullPath(path)); Console.ReadKey(); return; }

            //Alternatively Create the dictionary from a text corpus (e.g. http://norvig.com/big.txt ) 
            //Make sure the corpus does not contain spelling errors, invalid terms and the word frequency is representative to increase the precision of the spelling correction.
            //The dictionary may contain vocabulary from different languages. 
            //If you use mixed vocabulary use the language parameter in Correct() and CreateDictionary() accordingly.
            //You may use SymSpellCompound.CreateDictionaryEntry() to update a (self learning) dictionary incrementally
            //To extend spelling correction beyond single words to phrases (e.g. correcting "unitedkingom" to "united kingdom") simply add those phrases with CreateDictionaryEntry().
            //string path = "big.txt"
            //if (!SymSpellCompound.CreateDictionary(path,"")) Console.Error.WriteLine("File not found: " + Path.GetFullPath(path));

            stopWatch.Stop();
            long memDelta = GC.GetTotalMemory(true) - memSize;
            Console.WriteLine("\rDictionary: " + symSpell.WordCount.ToString("N0") + " words, "
                + symSpell.EntryCount.ToString("N0") + " entries, edit distance=" + symSpell.MaxDictionaryEditDistance.ToString()
                + " in " + stopWatch.Elapsed.TotalMilliseconds.ToString("0.0") + "ms "
                + (memDelta / 1024 / 1024.0).ToString("N0") + " MB");

            //warm up
            var result = symSpell.WordSegmentation("isit");

            string input;
            Console.WriteLine("Type in a text and hit enter to get word segmentation and correction:");
            while (!string.IsNullOrEmpty(input = (Console.ReadLine() ?? "").Trim()))
            {
                Correct(input, symSpell);
            }
        }

        private static void Correct(string input, SymSpell symSpell)
        {
            //check if input term or similar terms within edit-distance are in dictionary, return results sorted by ascending edit distance, then by descending word frequency     
            var suggestion = symSpell.WordSegmentation(input);

            //display term and frequency
            Console.WriteLine(suggestion.correctedString + " " + suggestion.distanceSum.ToString("N0") + " " + suggestion.probabilityLogSum.ToString());
        }
    }
}
