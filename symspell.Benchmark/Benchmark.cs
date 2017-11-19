using SoftWx.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;

namespace symspell.Benchmark
{
    class Benchmark
    {
        static readonly string Path = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string Dict82k = Path+"../../../symspell/frequency_dictionary_en_82_765.txt";
        static readonly string Dict500k = Path+"../../../symspelldemo/test_data/frequency_dictionary_en_500_000.txt";
        static readonly string Dict30k = Path+"../../../symspelldemo/test_data/frequency_dictionary_en_30_000.txt";
        static readonly string Query1k = Path+"../../../symspelldemo/test_data/noisy_query_en_1000.txt";
        static string timeResults;
        static string sizeResults;

        static void Main(string[] args)
        {
            //benchmark time
            BenchTime();
            Console.WriteLine();

            //benchmark size
            BenchSize();
            Console.WriteLine();

            Console.Write("complete, press any key...");
            Console.ReadKey();
        }
        private class TimeTest
        {
            public string Name;
            public Action TestAction;
            public int MinRuns;
            public TimeTest(string name, Action testAction, int minRuns = 3)
            {
                Name = name;
                TestAction = testAction;
                MinRuns = minRuns;
            }
        }
        static void BenchTime()
        {
            string[] query1k = BuildQuery1K();

            // Build Test suite
            var tests = new List<TimeTest>();

            tests.Add(new TimeTest("current LoadDictionary 82k dist=1 prefixLen=5",
                new Action(() => (new SymSpell(1, 5)).LoadDictionary(Dict82k, 0, 1))));
            tests.Add(new TimeTest("original LoadDictionary 82k dist=1 prefixLen=5",
                new Action(() => (new Original.SymSpell(1, 5)).LoadDictionary(Dict82k, "", 0, 1))));

            tests.Add(new TimeTest("current LoadDictionary 82k dist=1 prefixLen=7",
                new Action(() => (new SymSpell(1, 7)).LoadDictionary(Dict82k, 0, 1))));
            tests.Add(new TimeTest("original LoadDictionary 82k dist=1 prefixLen=7",
                new Action(() => (new Original.SymSpell(1, 7)).LoadDictionary(Dict82k, "", 0, 1))));

            tests.Add(new TimeTest("current LoadDictionary 82k dist=2 prefixLen=5",
                new Action(() => (new SymSpell(2, 5)).LoadDictionary(Dict82k, 0, 1))));
            tests.Add(new TimeTest("original LoadDictionary 82k dist=2 prefixLen=5",
                new Action(() => (new Original.SymSpell(2, 5)).LoadDictionary(Dict82k, "", 0, 1))));

            tests.Add(new TimeTest("current LoadDictionary 82k dist=2 prefixLen=7",
                new Action(() => (new SymSpell(2, 7)).LoadDictionary(Dict82k, 0, 1))));
            tests.Add(new TimeTest("original LoadDictionary 82k dist=2 prefixLen=7",
                new Action(() => (new Original.SymSpell(2, 7)).LoadDictionary(Dict82k, "", 0, 1))));

            SymSpell dict = new SymSpell(2, 7);
            dict.LoadDictionary(Dict82k, 0, 1);
            Original.SymSpell dictOrig = new Original.SymSpell(2, 7);
            dictOrig.LoadDictionary(Dict82k, "", 0, 1);

            tests.Add(new TimeTest("current Lookup exact 82k dist=2 prefixLen=7 verbose=0",
                new Action(() => dict.Lookup("different", 2, 0))));
            tests.Add(new TimeTest("original Lookup exact 82k dist=2 prefixLen=7 verbose=0",
                new Action(() => dictOrig.Lookup("different", "", 2, 0))));

            tests.Add(new TimeTest("current Lookup non-exact 82k dist=2 prefixLen=7 verbose=0",
                new Action(() => dict.Lookup("hockie", 2, 0))));
            tests.Add(new TimeTest("original Lookup non-exact 82k dist=2 prefixLen=7 verbose=0",
                new Action(() => dictOrig.Lookup("hockie", "", 2, 0))));

            tests.Add(new TimeTest("current Lookup exact 82k dist=2 prefixLen=7 verbose=1",
                new Action(() => dict.Lookup("different", 2, 1))));
            tests.Add(new TimeTest("original Lookup exact 82k dist=2 prefixLen=7 verbose=1",
                new Action(() => dictOrig.Lookup("different", "", 2, 1))));

            tests.Add(new TimeTest("current Lookup non-exact 82k dist=2 prefixLen=7 verbose=1",
                new Action(() => dict.Lookup("hockie", 2, 1))));
            tests.Add(new TimeTest("original Lookup non-exact 82k dist=2 prefixLen=7 verbose=1",
                new Action(() => dictOrig.Lookup("hockie", "", 2, 1))));

            tests.Add(new TimeTest("current Query1000 82k dist=2 prefixLen=7 verbose=1",
                new Action(() => { foreach (var word in query1k) dict.Lookup(word, 2, 1); })));
            tests.Add(new TimeTest("original Query1000 82k dist=2 prefixLen=7 verbose=1",
                new Action(() => { foreach (var word in query1k) dictOrig.Lookup(word, "", 2, 1); })));

            tests.Add(new TimeTest("current LoadDictionary 500k dist=2 prefixLen=7",
                new Action(() => (dict = new SymSpell(2, 7)).LoadDictionary(Dict500k, 0, 1)), 1));
            tests.Add(new TimeTest("original LoadDictionary 500k dist=2 prefixLen=7",
                new Action(() => (dictOrig = new Original.SymSpell(2, 7)).LoadDictionary(Dict500k, "", 0, 1)), 1));

            tests.Add(new TimeTest("current Lookup exact 500k dist=2 prefixLen=7 verbose=0",
                new Action(() => dict.Lookup("different", 2, 0))));
            tests.Add(new TimeTest("original Lookup exact 500k dist=2 prefixLen=7 verbose=0",
                new Action(() => dictOrig.Lookup("different", "", 2, 0))));

            tests.Add(new TimeTest("current Lookup non-exact 500k dist=2 prefixLen=7 verbose=0",
                new Action(() => dict.Lookup("hockie", 2, 0))));
            tests.Add(new TimeTest("original Lookup non-exact 500k dist=2 prefixLen=7 verbose=0",
                new Action(() => dictOrig.Lookup("hockie", "", 2, 0))));

            tests.Add(new TimeTest("current Lookup exact 500k dist=2 prefixLen=7 verbose=1",
                new Action(() => dict.Lookup("different", 2, 1))));
            tests.Add(new TimeTest("original Lookup exact 500k dist=2 prefixLen=7 verbose=1",
                new Action(() => dictOrig.Lookup("different", "", 2, 1))));

            tests.Add(new TimeTest("current Lookup non-exact 500k dist=2 prefixLen=7 verbose=1",
                new Action(() => dict.Lookup("hockie", 2, 1))));
            tests.Add(new TimeTest("original Lookup non-exact 500k dist=2 prefixLen=7 verbose=1",
                new Action(() => dictOrig.Lookup("hockie", "", 2, 1))));

            tests.Add(new TimeTest("current Query1000 500k dist=2 prefixLen=7 verbose=1",
                new Action(() => { foreach (var word in query1k) dict.Lookup(word, 2, 1); })));
            tests.Add(new TimeTest("original Query1000 500k dist=2 prefixLen=7 verbose=1",
                new Action(() => { foreach (var word in query1k) dictOrig.Lookup(word, "", 2, 1); })));

            //run tests
            var bench = new Bench(3, 1000, false);
            Bench.TimeResult result;
            timeResults = "ms per op, test description" + Environment.NewLine;
            Console.Write(timeResults);
            foreach(var test in tests)
            {
                bench.MinIterations = test.MinRuns;
                result = bench.Time(test.Name, test.TestAction);
                LogTime(result.MillisecondsPerOperation, test.Name);
            }
        }
        static string[] BuildQuery1K()
        {
            string[] testList = new string[1000];
            //load 1000 terms with random spelling errors
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
        static void LogTime(double milliseconds, string name)
        {
            string txt = milliseconds.ToString("0.000000") + "," + name;
            Console.WriteLine(txt);
            sizeResults += txt + Environment.NewLine;
        }
        private class SizeTest
        {
            public string Name;
            public Func<object> TestFunc;
            public int MinRuns;
            public SizeTest(string name, Func<object> testFunc, int minRuns = 3)
            {
                Name = name;
                TestFunc = testFunc;
                MinRuns = minRuns;
            }
        }
        static void BenchSize()
        {
            // Build Test suite
            var tests = new List<SizeTest>();

            tests.Add(new SizeTest("current 82k dist=1 prefixLen=5", new Func<object>(() =>
            {
                var d = new SymSpell(1, 5);
                d.LoadDictionary(Dict82k, 0, 1);
                return d;
            })));
            tests.Add(new SizeTest("original 82k dist=1 prefixLen=5", new Func<object>(() =>
            {
                var d = new Original.SymSpell(1, 5);
                d.LoadDictionary(Dict82k, "", 0, 1);
                return d;
            })));

            tests.Add(new SizeTest("current 82k dist=2 prefixLen=7", new Func<object>(() =>
            {
                var d = new SymSpell(2, 7);
                d.LoadDictionary(Dict82k, 0, 1);
                return d;
            })));
            tests.Add(new SizeTest("original 82k dist=2 prefixLen=7", new Func<object>(() =>
            {
                var d = new Original.SymSpell(2, 7);
                d.LoadDictionary(Dict82k, "", 0, 1);
                return d;
            })));

            tests.Add(new SizeTest("current 500k dist=2 prefixLen=7", new Func<object>(() =>
            {
                var d = new SymSpell(2, 7);
                d.LoadDictionary(Dict500k, 0, 1);
                return d;
            }), 1));
            tests.Add(new SizeTest("original 500k dist=2 prefixLen=7", new Func<object>(() =>
            {
                var d = new Original.SymSpell(2, 7);
                d.LoadDictionary(Dict500k, "", 0, 1);
                return d;
            }), 1));

            //run tests
            var bench = new Bench(3, 1000, false);
            timeResults = "size in megabytes, test description" + Environment.NewLine;
            Console.Write(timeResults);
            foreach (var test in tests)
            {
                bench.MinIterations = test.MinRuns;
                LogSize(bench.ByteSize(test.TestFunc), test.Name);
            }
        }
        static void LogSize(long bytes, string name)
        {
            string txt = (bytes/(1024*1024.0)).ToString("0.000") + "," + name;
            Console.WriteLine(txt);
            sizeResults += txt + Environment.NewLine;
        }
    }
}
