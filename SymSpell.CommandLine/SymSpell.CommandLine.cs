using System;
using System.Diagnostics;

namespace symspell.CommandLine
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length>=2)
            { 
                Console.Error.Write("Creating dictionary ...");
                long memSize = GC.GetTotalMemory(true);
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                //parameters
                int initialCapacity = 82765;

                int maxEditDistanceDictionary = 2; //maximum edit distance per dictionary precalculation
                if (args.Length > 2) if (!int.TryParse(args[2], out maxEditDistanceDictionary)) {Console.Error.WriteLine("Error in parameter 3");return; }
                int maxEditDistanceLookup = maxEditDistanceDictionary; //max edit distance per lookup 

                var suggestionVerbosity = SymSpell.Verbosity.Top; //Top, Closest, All
                if (args.Length > 3) if (!Enum.TryParse(args[3], out suggestionVerbosity)) { Console.Error.WriteLine("Error in parameter 4"); return; }

                int prefixLength = 7;
                if (args.Length > 4) if (!int.TryParse(args[4], out prefixLength)) { Console.Error.WriteLine("Error in parameter 5"); return; }

                string dictionaryPath = AppDomain.CurrentDomain.BaseDirectory + args[1];// "../../../../SymSpell/frequency_dictionary_en_82_765.txt";
                int termIndex = 0; //column of the term in the dictionary text file
                int countIndex = 1; //column of the term frequency in the dictionary text file

                //create object
                var symSpell = new SymSpell(initialCapacity, maxEditDistanceDictionary, prefixLength);

                //load dictionary
                switch (args[0].ToLower())
                {
                    case "load":
                        if (!symSpell.LoadDictionary(dictionaryPath, termIndex, countIndex))
                        {
                            Console.Error.WriteLine("File not found!");
                            return;
                        }
                        break;

                    case "create":
                        if (!symSpell.CreateDictionary(dictionaryPath))
                        {
                            Console.Error.WriteLine("File not found!");
                            return;
                        }
                        break;

                    default:
                        break;
                }

                stopWatch.Stop();
                long memDelta = GC.GetTotalMemory(true) - memSize;

                //not to stdout, but to Console.Error: status info will alway be on console, but not redirected or piped
                Console.Error.WriteLine("\rDictionary: " + symSpell.WordCount.ToString("N0") + " words, "
                + symSpell.EntryCount.ToString("N0") + " entries, edit distance=" + symSpell.MaxDictionaryEditDistance.ToString()
                + " in " + stopWatch.Elapsed.TotalMilliseconds.ToString("0.0") + "ms "
                + (memDelta / 1024 / 1024.0).ToString("N0") + " MB");

                //warm up
                var result = symSpell.Lookup("warmup", SymSpell.Verbosity.All, 1);

                //lookup suggestions for single-word input strings
                string inputTerm;
                while (!string.IsNullOrEmpty(inputTerm = (Console.ReadLine() ?? "").Trim()))
                {
                    var suggestions = symSpell.Lookup(inputTerm, suggestionVerbosity, maxEditDistanceLookup,true);

                    //display suggestions, edit distance and term frequency
                    foreach (var suggestion in suggestions)
                    {
                        Console.WriteLine(suggestion.term + " " + suggestion.distance.ToString() + " " + suggestion.count.ToString("N0"));
                    }
                }

            }
            else
            {
                //help
                Console.WriteLine("SymSpell.CommandLine load   Path [MaxEditDistance] [Verbosity] [PrefixLength]");
                Console.WriteLine("SymSpell.CommandLine create Path [MaxEditDistance] [Verbosity] [PrefixLength]");
                Console.WriteLine();
                Console.WriteLine("load: load dictionary from dictionary file");
                Console.WriteLine("create: create dictionary from text corpus");
                Console.WriteLine("MaxEditDistance: default=2");
                Console.WriteLine("Verbosity=Top|Closest|All (case-sensitive)");
                Console.WriteLine("PrefixLength: default=7 (5:low memory; 7:fast lookup)");
                Console.WriteLine();
            }
        }
        
    }
}
