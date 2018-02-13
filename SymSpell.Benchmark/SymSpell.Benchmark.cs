using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace symspell.Benchmark
{
    class Benchmark
    {
        static readonly string Path = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string Query1k = Path + "../../../../SymSpell.Benchmark/test_data/noisy_query_en_1000.txt";

        static readonly string[] DictionaryPath = {
            Path+"../../../../SymSpell.Benchmark/test_data/frequency_dictionary_en_30_000.txt",
            Path+"../../../../SymSpell/frequency_dictionary_en_82_765.txt",
            Path+"../../../../SymSpell.Benchmark/test_data/frequency_dictionary_en_500_000.txt" };

        static readonly string[] DictionaryName = {
            "30k",
            "82k",
            "500k" };
        
        static readonly int[] DictionarySize = {
            29159,
            82765,
            500000 };

        //load 1000 terms with random spelling errors
        static string[] BuildQuery1K()
        {
            string[] testList = new string[1000];
            int i = 0;
            using (StreamReader sr = new StreamReader(File.OpenRead(Query1k)))
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
            return testList;
        }

        static void Main(string[] args)
        {
            Console.WindowWidth = Math.Min(160, Console.LargestWindowWidth);
            Console.WindowHeight = Math.Min(80, Console.LargestWindowHeight);
            Console.BufferWidth = 800;
            Console.BufferHeight = 10000;

            WarmUp();

            BenchmarkPrecalculationLookup();

            Console.WriteLine();
            Console.Write("complete, press any key...");
            Console.ReadKey();
        }

        // pre-run to ensure code has executed once before timing benchmarks
        static void WarmUp()
        {
            SymSpell dict = new SymSpell(16, 2, 7);
            dict.LoadDictionary(DictionaryPath[0], 0, 1);
            var result = dict.Lookup("hockie", SymSpell.Verbosity.All, 1);

            Original.SymSpell dictOrig = new Original.SymSpell(2, 7);
            dictOrig.LoadDictionary(DictionaryPath[0], "", 0, 1);
            var resultOrig = dictOrig.Lookup("hockie", "", 1, 2);
        }

        static void BenchmarkPrecalculationLookup()
        {
            string[] query1k = BuildQuery1K();
            int resultNumber = 0;
            int repetitions = 1000;
            int totalLoopCount = 0;
            long totalMatches = 0;
            long totalOrigMatches = 0;
            double totalLoadTime, totalMem, totalLookupTime, totalOrigLoadTime, totalOrigMem, totalOrigLookupTime;
            totalLoadTime = totalMem = totalLookupTime = totalOrigLoadTime = totalOrigMem = totalOrigLookupTime = 0;
            long totalRepetitions = 0;

            Stopwatch stopWatch = new Stopwatch();
            for (int maxEditDistance = 1; maxEditDistance <= 3; maxEditDistance++)
            {
                for (int prefixLength = 5; prefixLength <= 7; prefixLength++)
                {

                    //benchmark dictionary precalculation size and time 
                    //maxEditDistance=1/2/3; prefixLength=5/6/7;  dictionary=30k/82k/500k; class=instantiated/static
                    for (int i = 0; i < DictionaryPath.Length; i++)
                    {
                        totalLoopCount++;

                        //instantiated dictionary        
                        long memSize = GC.GetTotalMemory(true);
                        stopWatch.Restart();
                        SymSpell dict = new SymSpell(DictionarySize[i], maxEditDistance, prefixLength);
                        dict.LoadDictionary(DictionaryPath[i], 0, 1);
                        stopWatch.Stop();
                        long memDelta = GC.GetTotalMemory(true) - memSize;
                        totalLoadTime += stopWatch.Elapsed.TotalSeconds;
                        totalMem += memDelta / 1024.0 / 1024.0;
                        Console.WriteLine("Precalculation instance " + stopWatch.Elapsed.TotalSeconds.ToString("N3") + "s " + (memDelta / 1024.0 / 1024.0).ToString("N1") + "MB " + dict.WordCount.ToString("N0") + " words " + dict.EntryCount.ToString("N0") + " entries  MaxEditDistance=" + maxEditDistance.ToString() + " prefixLength=" + prefixLength.ToString() + " dict=" + DictionaryName[i]);

                        //static dictionary 
                        memSize = GC.GetTotalMemory(true);
                        stopWatch.Restart();
                        Original.SymSpell dictOrig = new Original.SymSpell(maxEditDistance, prefixLength);
                        dictOrig.LoadDictionary(DictionaryPath[i], "", 0, 1);
                        stopWatch.Stop();
                        memDelta = GC.GetTotalMemory(true) - memSize;
                        totalOrigLoadTime += stopWatch.Elapsed.TotalSeconds;
                        totalOrigMem += memDelta / 1024.0 / 1024.0;
                        Console.WriteLine("Precalculation static   " + stopWatch.Elapsed.TotalSeconds.ToString("N3") + "s " + (memDelta / 1024 / 1024.0).ToString("N1") + "MB " + dictOrig.Count.ToString("N0") + " words " + dictOrig.EntryCount.ToString("N0") + " entries  MaxEditDistance=" + maxEditDistance.ToString() + " prefixLength=" + prefixLength.ToString() + " dict=" + DictionaryName[i]);

                        //benchmark lookup result number and time
                        //maxEditDistance=1/2/3; prefixLength=5/6/7; dictionary=30k/82k/500k; verbosity=0/1/2; query=exact/non-exact/mix; class=instantiated/static
                        foreach (SymSpell.Verbosity verbosity in Enum.GetValues(typeof(SymSpell.Verbosity)))
                        {
                            //instantiated exact
                            stopWatch.Restart();
                            for (int round = 0; round < repetitions; round++) resultNumber = dict.Lookup("different", verbosity, maxEditDistance).Count;
                            stopWatch.Stop();
                            totalLookupTime += stopWatch.Elapsed.TotalMilliseconds;
                            totalMatches += resultNumber;
                            Console.WriteLine("Lookup instance " + resultNumber.ToString("N0") + " results " + (stopWatch.Elapsed.TotalMilliseconds / repetitions).ToString("N6") + "ms/op verbosity=" + verbosity.ToString() + " query=exact");
                            //static exact
                            stopWatch.Restart();
                            for (int round = 0; round < repetitions; round++) resultNumber = dictOrig.Lookup("different", "", maxEditDistance, (int)verbosity).Count;
                            stopWatch.Stop();
                            totalOrigLookupTime += stopWatch.Elapsed.TotalMilliseconds;
                            totalOrigMatches += resultNumber;
                            Console.WriteLine("Lookup static   " + resultNumber.ToString("N0") + " results " + (stopWatch.Elapsed.TotalMilliseconds / repetitions).ToString("N6") + "ms/op verbosity=" + verbosity.ToString() + " query=exact");
                            Console.WriteLine();
                            totalRepetitions += repetitions;

                            //instantiated non-exact
                            stopWatch.Restart();
                            for (int round = 0; round < repetitions; round++) resultNumber = dict.Lookup("hockie", verbosity, maxEditDistance).Count;
                            stopWatch.Stop();
                            totalLookupTime += stopWatch.Elapsed.TotalMilliseconds;
                            totalMatches += resultNumber;
                            Console.WriteLine("Lookup instance " + resultNumber.ToString("N0") + " results " + (stopWatch.Elapsed.TotalMilliseconds / repetitions).ToString("N6") + "ms/op verbosity=" + verbosity.ToString() + " query=non-exact");
                            //static non-exact
                            stopWatch.Restart();
                            for (int round = 0; round < repetitions; round++) resultNumber = dictOrig.Lookup("hockie", "", maxEditDistance, (int)verbosity).Count;
                            stopWatch.Stop();
                            totalOrigLookupTime += stopWatch.Elapsed.TotalMilliseconds;
                            totalOrigMatches += resultNumber;
                            Console.WriteLine("Lookup static   " + resultNumber.ToString("N0") + " results " + (stopWatch.Elapsed.TotalMilliseconds / repetitions).ToString("N6") + "ms/op verbosity=" + verbosity.ToString() + " query=non-exact");
                            Console.WriteLine();
                            totalRepetitions += repetitions;

                            //instantiated mix                           
                            stopWatch.Restart();
                            resultNumber = 0; foreach (var word in query1k) resultNumber += dict.Lookup(word, verbosity, maxEditDistance).Count;
                            stopWatch.Stop();
                            totalLookupTime += stopWatch.Elapsed.TotalMilliseconds;
                            totalMatches += resultNumber;
                            Console.WriteLine("Lookup instance " + resultNumber.ToString("N0") + " results " + (stopWatch.Elapsed.TotalMilliseconds / query1k.Length).ToString("N6") + "ms/op verbosity=" + verbosity.ToString() + " query=mix");
                            //static mix                           
                            stopWatch.Restart();
                            resultNumber = 0; foreach (var word in query1k) resultNumber += dictOrig.Lookup(word, "", maxEditDistance, (int)verbosity).Count;
                            stopWatch.Stop();
                            totalOrigLookupTime += stopWatch.Elapsed.TotalMilliseconds;
                            totalOrigMatches += resultNumber;
                            Console.WriteLine("Lookup static   " + resultNumber.ToString("N0") + " results " + (stopWatch.Elapsed.TotalMilliseconds / query1k.Length).ToString("N6") + "ms/op verbosity=" + verbosity.ToString() + " query=mix");
                            Console.WriteLine();
                            totalRepetitions += query1k.Length;
                        }
                        Console.WriteLine();
                        
                        dict = null;
                        dictOrig = null;
                    }
                }
            }
            Console.WriteLine("Average Precalculation time instance " + (totalLoadTime / totalLoopCount).ToString("N3") + "s   " + ((totalLoadTime / totalOrigLoadTime) - 1).ToString("P1"));
            Console.WriteLine("Average Precalculation time static   " + (totalOrigLoadTime / totalLoopCount).ToString("N3") + "s");
            Console.WriteLine("Average Precalculation memory instance " + (totalMem / totalLoopCount).ToString("N1") + "MB " + ((totalMem / totalOrigMem) - 1).ToString("P1"));
            Console.WriteLine("Average Precalculation memory static   " + (totalOrigMem / totalLoopCount).ToString("N1") + "MB");
            Console.WriteLine("Average Lookup time instance " + (totalLookupTime / totalRepetitions).ToString("N3") + "ms          " + ((totalLookupTime / totalOrigLookupTime) - 1).ToString("P1"));
            Console.WriteLine("Average Lookup time static   " + (totalOrigLookupTime / totalRepetitions).ToString("N3") + "ms");
            Console.WriteLine("Total Lookup results instance " + totalMatches.ToString("N0") + "      " + (totalMatches - totalOrigMatches) + " differences");
            Console.WriteLine("Total Lookup results static   " + totalOrigMatches.ToString("N0"));
        }
    }
}
