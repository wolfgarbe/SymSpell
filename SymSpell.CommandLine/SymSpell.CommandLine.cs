using System;
using System.Diagnostics;

namespace symspell.CommandLine
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length>2)
            { 
                Console.Error.Write("Creating dictionary ...");
                long memSize = GC.GetTotalMemory(true);
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                //parameters
                int initialCapacity = 82765;
                int termIndex = 0; //column of the term in the dictionary text file
                int countIndex = 1; //column of the term frequency in the dictionary text file

                //dictionaryType
                string dictionaryType = args[0].ToLower();
                if ("load.create".IndexOf(dictionaryType) == -1) { Console.Error.WriteLine("Error in parameter 1"); return; }

                //dictionaryPath
                string dictionaryPath = AppDomain.CurrentDomain.BaseDirectory + args[1];

                //prefix length (optional parameter)
                int offset = 0;
                string lookupType = "";
                int prefixLength = 7;
                if (!int.TryParse(args[2], out prefixLength)) prefixLength = 7; else offset = 1;

                //lookupType
                if (args.Length > 2 + offset)
                { 
                    lookupType = args[2+offset].ToLower();
                    if ("lookup.lookupcompound.wordsegment".IndexOf(lookupType)==-1) { Console.Error.WriteLine("Error in parameter "+(3+offset).ToString()); return; }
                }

                //maxEditDistance
                int maxEditDistanceDictionary = 2; //maximum edit distance per dictionary precalculation
                if (args.Length > 3 + offset) if (!int.TryParse(args[3 + offset], out maxEditDistanceDictionary)) {Console.Error.WriteLine("Error in parameter " + (4 + offset).ToString());return; }

                //output stats
                bool outputStats = false;//false, true
                if (args.Length > 4 + offset) if (!bool.TryParse(args[4 + offset], out outputStats)) { Console.Error.WriteLine("Error in parameter " + (5 + offset).ToString()); return; }

                //verbosity
                var suggestionVerbosity = SymSpell.Verbosity.Top; //Top, Closest, All
                if (args.Length > 5 + offset) if (!Enum.TryParse(args[5 + offset], true, out suggestionVerbosity)) { Console.Error.WriteLine("Error in parameter " + (6 + offset).ToString()); return; }

                //create object
                var symSpell = new SymSpell(initialCapacity, maxEditDistanceDictionary, prefixLength);

                //load dictionary
                switch (dictionaryType)
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
                var result = symSpell.Lookup("warmup", SymSpell.Verbosity.All);

                //lookup suggestions for single-word input strings
                string inputTerm;
                while (!string.IsNullOrEmpty(inputTerm = (Console.ReadLine() ?? "").Trim()))
                {
                    switch (lookupType)
                    {
                        case "lookup":
                            var suggestions = symSpell.Lookup(inputTerm, suggestionVerbosity, maxEditDistanceDictionary, true);
                            //display suggestions, edit distance and term frequency
                            foreach (var suggestion in suggestions)
                            {
                                if (outputStats) Console.WriteLine(suggestion.term + " " + suggestion.distance.ToString() + " " + suggestion.count.ToString("N0"));
                                else Console.WriteLine(suggestion.term);
                            }
                            break;

                        case "lookupcompound":
                            var suggestions2 = symSpell.LookupCompound(inputTerm);
                            //display suggestions, edit distance and term frequency
                            foreach (var suggestion in suggestions2)
                            {
                                if (outputStats) Console.WriteLine(suggestion.term + " " + suggestion.distance.ToString() + " " + suggestion.count.ToString("N0"));
                                else Console.WriteLine(suggestion.term);
                            }
                            break;

                        case "wordsegment":
                            var suggestion3 = symSpell.WordSegmentation(inputTerm);
                            //display suggestions, edit distance and term frequency
                            if (outputStats) Console.WriteLine(suggestion3.correctedString + " " + suggestion3.distanceSum.ToString("N0") + " " + suggestion3.probabilityLogSum.ToString());
                            else Console.WriteLine(suggestion3.correctedString);
                          
                            break;

                        default:
                            break;
                    }

                }

            }
            else
            {
                //PrefixLength is number

                //help
                Console.WriteLine("SymSpell.CommandLine DictionaryType DictionaryPath [PrefixLength] LookupType [MaxEditDistance] [OutputStats] [Verbosity]");
                Console.WriteLine();
                Console.WriteLine("DictionaryType=load|create");
                Console.WriteLine("   load: load dictionary from dictionary file");
                Console.WriteLine("   create: create dictionary from text corpus");
                Console.WriteLine("DictionaryPath: path to dictionary/corpus file");
                Console.WriteLine("PrefixLength: default=7 (speed/memory consumption trade-off)");  //dictionary param
                Console.WriteLine("   5: low memory, slow lookup");
                Console.WriteLine("   6: medium memory, medium lookup");
                Console.WriteLine("   7: high memory, fast lookup");
                //lookup intended for correction of single word
                //lookupcompound intended for correction of multiple words, it can insert only a single space per token, faster than wordsegmentation
                //wordsegmentation intended for segmentation and correction of multiple words, it can insert multiple spaces per token, slower than lookupcompound
                Console.WriteLine("LookupType=lookup|lookupcompound|wordsegment");
                Console.WriteLine("   lookup: correct single word");
                Console.WriteLine("   lookupcompound: correct multiple-word string (supports splitting/merging)");
                Console.WriteLine("   wordsegment: word segment and correct input string");
                Console.WriteLine("MaxEditDistance: default=2 (0: no correction, word segmentation only)");
                Console.WriteLine("OutputStats=false|true");
                Console.WriteLine("   false: only corrected string");
                Console.WriteLine("   true: corrected string, edit distance, word frequency/probability");
                Console.WriteLine("Verbosity=top|closest|all"); //no effect for lookupcompound and wordsegment
                Console.WriteLine("   top: Top suggestion");
                Console.WriteLine("   closest: All suggestions of smallest edit distance found");
                Console.WriteLine("   all: All suggestions within maxEditDistance");
                Console.WriteLine();
            }
        }
        
    }
}
