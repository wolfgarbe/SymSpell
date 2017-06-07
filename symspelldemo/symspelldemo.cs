using System;
using System.Collections.Generic;
using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
using System.Diagnostics;

// uses SymSpell.cs 
// *alternatively* use SymSpell as NuGet package:
// 1. build NuGet package "SymSpell" from project "symspell"
// 2. add   NuGet package "SymSpell" to   project "symspelldemo" (or to your own project where you want to use symspell)
// 3. build and run project "symspelldemo"

// Usage: single word + Enter:  Display spelling suggestions
//        Enter without input:  Terminate the program

namespace symspelldemo
{
    class Program
    {
        public static void Correct(string input, string language)
        {
            List<SymSpell.suggestItem> suggestions = null;

            //Benchmark: 1000 x Lookup
            /*
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            for (int i = 0; i < 1000; i++)
            {
                suggestions = SymSpell.Lookup(input,language, SymSpell.editDistanceMax);
            }
            stopWatch.Stop();
            Console.WriteLine(stopWatch.ElapsedMilliseconds.ToString());
            */

            //check if input term or similar terms within edit-distance are in dictionary, return results sorted by ascending edit distance, then by descending word frequency     
            suggestions = SymSpell.Lookup(input, language, SymSpell.editDistanceMax);

            //display term and frequency
            foreach (var suggestion in suggestions)
            {
                Console.WriteLine( suggestion.term + " " + suggestion.distance.ToString() + " " + suggestion.count.ToString("N0"));
            }
            if (SymSpell.verbose != 0) Console.WriteLine(suggestions.Count.ToString() + " suggestions");
        }

        //Load a frequency dictionary or create a frequency dictionary from a text corpus
        public static void Main(string[] args)
        {
            //set global parameters
            SymSpell.verbose = 0;
            SymSpell.editDistanceMax = 2;

            Console.Write("Creating dictionary ...");//neuneu
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //Load a frequency dictionary
            //wordfrequency_en.txt  ensures high correction quality by combining two data sources: 
            //Google Books Ngram data  provides representative word frequencies (but contains many entries with spelling errors)  
            //SCOWL — Spell Checker Oriented Word Lists which ensures genuine English vocabulary (but contained no word frequencies)
            //SymSpell.LoadDictionary("../../wordfrequency_en.txt", "", 0, 1);           //path when using symspell nuget package (wordfrequency_en.txt is included in nuget package)
            string path = "../../../symspell/wordfrequency_en.txt";
            if (!SymSpell.LoadDictionary(path, "", 0, 1)) Console.Error.WriteLine("File not found: " + Path.GetFullPath(path)); //path when using symspell.cs

            //Alternatively Create the dictionary from a text corpus (e.g. http://norvig.com/big.txt ) 
            //Make sure the corpus does not contain spelling errors, invalid terms and the word frequency is representative to increase the precision of the spelling correction.
            //The dictionary may contain vocabulary from different languages. 
            //If you use mixed vocabulary use the language parameter in Correct() and CreateDictionary() accordingly.
            //You may use SymSpell.CreateDictionaryEntry() to update a (self learning) dictionary incrementally
            //To extend spelling correction beyond single words to phrases (e.g. correcting "unitedkingom" to "united kingdom") simply add those phrases with CreateDictionaryEntry().
            //string path = "big.txt"
            //if (!SymSpell.CreateDictionary(path,"")) Console.Error.WriteLine("File not found: " + Path.GetFullPath(path));

            Console.WriteLine("\rDictionary: " + SymSpell.wordlist.Count.ToString("N0") + " words, " + SymSpell.dictionary.Count.ToString("N0") + " entries, edit distance=" + SymSpell.editDistanceMax.ToString() + " in " + stopWatch.ElapsedMilliseconds.ToString() + "ms "/*+ (Process.GetCurrentProcess().PrivateMemorySize64/1000000).ToString("N0")+ " MB"*/);//neuneu

            string input;
            while (!string.IsNullOrEmpty(input = (Console.ReadLine() ?? "").Trim()))
            {
                Correct(input, "");
            }
        }
    }
}
