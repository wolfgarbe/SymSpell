// SymSpell: 1 million times faster through Symmetric Delete spelling correction algorithm
//
// The Symmetric Delete spelling correction algorithm reduces the complexity of edit candidate generation and dictionary lookup 
// for a given Damerau-Levenshtein distance. It is six orders of magnitude faster and language independent.
// Opposite to other algorithms only deletes are required, no transposes + replaces + inserts.
// Transposes + replaces + inserts of the input term are transformed into deletes of the dictionary term.
// Replaces and inserts are expensive and language dependent: e.g. Chinese has 70,000 Unicode Han characters!
//
// Copyright (C) 2017 Wolf Garbe
// Version: 5.0
// Author: Wolf Garbe <wolf.garbe@faroo.com>
// Maintainer: Wolf Garbe <wolf.garbe@faroo.com>
// URL: https://github.com/wolfgarbe/symspell
// Description: http://blog.faroo.com/2012/06/07/improved-edit-distance-based-spelling-correction/
//
// License:
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License, 
// version 3.0 (LGPL-3.0) as published by the Free Software Foundation.
// http://www.opensource.org/licenses/LGPL-3.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using System.Collections;
using System.Collections.Specialized;
namespace Original
{
    public class SymSpell
    {
        const int defaultEditDistanceMax = 2;
        const int defaultPrefixLength = 7;

        private int defaultVerbose = 0;

        private int editDistanceMax;
        private int maxLength; //maximum dictionary term length
        private int lp; //prefix length  5..7

        //Dictionary that contains both the original words and the deletes derived from them. A term might be both word and delete from another word at the same time.
        //For space reduction a item might be either of type dictionaryItem or Int. 
        //A dictionaryItem is used for word, word/delete, and delete with multiple suggestions. Int is used for deletes with a single suggestion (the majority of entries).
        //A Dictionary with fixed value type (int) requires less memory than a Dictionary with variable value type (object)
        //To support two types with a Dictionary with fixed type (int), positive number point to one list of type 1 (string), and negative numbers point to a secondary list of type 2 (dictionaryEntry)
        private Dictionary<string, Int32> dictionary = new Dictionary<string, Int32>();                                                                                       
        private List<string> wordlist = new List<string>();  //List of unique words. By using the suggestions (Int) as index for this list they are translated into the original string.
        private List<DictionaryItem> itemlist = new List<DictionaryItem>();

        private class DictionaryItem
        {
            public List<Int32> suggestions = new List<Int32>(2);
            public Int64 count = 0;
        }

        public class SuggestItem
        {
            public string term = "";
            public int distance = 0;
            public Int64 count = 0;

            public override bool Equals(object obj)
            {
                return Equals(term, ((SuggestItem)obj).term);
            }

            public override int GetHashCode()
            {
                return term.GetHashCode();
            }
        }

        public int EditDistanceMax { get { return this.editDistanceMax; } }
        public int PrefixLenth { get { return this.lp; } }
        /// <summary>Length of longest word in the dictionary.</summary>
        public int MaxLength { get { return this.maxLength; } }

        //0: top suggestion
        //1: all suggestions of smallest edit distance   
        //2: all suggestions <= editDistanceMax (slower, no early termination)
        public int DefaultVerbose
        {
            get { return this.defaultVerbose; }
            set
            {
                if (value < 0 || value > 2) throw new ArgumentOutOfRangeException();
                this.defaultVerbose = value;
            }
        }

        /// <summary>Number of unique words in the dictionary.</summary>
        public int Count { get { return this.wordlist.Count; } }

        /// <summary>Number of words and intermediate word deletes encoded in the dictionary.</summary>
        public int EntryCount { get { return this.dictionary.Count; } }

        public SymSpell(int editDistanceMax = defaultEditDistanceMax, int prefixLength = defaultPrefixLength)
        {
            this.editDistanceMax = editDistanceMax;
            this.lp = prefixLength;
        }

        public void Clear()
        {
            this.dictionary = new Dictionary<string, Int32>(); //initialisierung
            this.wordlist = new List<string>();
            this.itemlist = new List<DictionaryItem>();
            this.maxLength = 0;
        }

        //for every word there all deletes with an edit distance of 1..editDistanceMax created and added to the dictionary
        //every delete entry has a suggestions list, which points to the original term(s) it was created from
        //The dictionary may be dynamically updated (word frequency and new words) at any time by calling createDictionaryEntry
        public bool CreateDictionaryEntry(string key, string language, Int64 count)
        {
            //a treshold might be specifid, when a term occurs so frequently in the corpus that it is considered a valid word for spelling correction
            int countTreshold = 1;
            Int64 countPrevious = 0;
            bool result = false;
            DictionaryItem value = null;
            //Int32 valueo;
            if (dictionary.TryGetValue(language + key, out int valueo))
            {
                //new word, but identical single delete existed before
                //+ = single delete = index auf worlist 
                //- = !single delete (word / word + delete(s) / deletes) = index to dictionaryItem list
                if (valueo >= 0)
                {
                    Int32 tmp = valueo;
                    value = new DictionaryItem();
                    value.suggestions.Add(tmp);
                    itemlist.Add(value);
                    dictionary[language + key] = -itemlist.Count;
                }
                //existing word (word appears several times)
                else
                {
                    value = itemlist[-valueo - 1];
                }

                countPrevious = value.count;
                //summarizes multiple frequency entries of a word (prevents overflow)
                value.count = (Int64.MaxValue - value.count > count) ? value.count + count : Int64.MaxValue;
            }
            else
            {
                //new word
                value = new DictionaryItem()
                {
                    count = count
                };
                itemlist.Add(value);
                dictionary[language + key] = -itemlist.Count;

                if (key.Length > maxLength) maxLength = key.Length;
            }

            //edits/suggestions are created only once, no matter how often word occurs
            //edits/suggestions are created only as soon as the word occurs in the corpus, 
            //even if the same term existed before in the dictionary as an edit from another word
            if ((value.count >= countTreshold) && (countPrevious < countTreshold))
            {
                //word2index
                wordlist.Add(key);
                Int32 keyint = (Int32)(wordlist.Count - 1);

                result = true;

                //create deletes
                foreach (string delete in EditsPrefix(key))
                {
                    //Int32 value2;
                    DictionaryItem di;
                    if (dictionary.TryGetValue(language + delete, out int value2))
                    {
                        //already exists:
                        //1. word1==deletes(word2) 
                        //2. deletes(word1)==deletes(word2) 
                        //int or dictionaryItem? single delete existed before!
                        if (value2 >= 0)
                        {
                            //transformes int to dictionaryItem
                            di = new DictionaryItem(); di.suggestions.Add(value2); itemlist.Add(di); dictionary[language + delete] = -itemlist.Count;
                            if (!di.suggestions.Contains(keyint)) di.suggestions.Add(keyint);
                        }
                        else
                        {
                            di = itemlist[-value2 - 1];
                            if (!di.suggestions.Contains(keyint)) di.suggestions.Add(keyint);
                        }
                    }
                    else
                    {
                        dictionary.Add(language + delete, keyint);
                    }

                }
            }
            return result;
        }

        //load a frequency dictionary (merges with any dictionary data already loaded)
        public bool LoadDictionary(string corpus, string language, int termIndex, int countIndex)
        {
            if (!File.Exists(corpus)) return false;

            using (StreamReader sr = new StreamReader(File.OpenRead(corpus)))
            {
                String line;

                //process a single line at a time only for memory efficiency
                while ((line = sr.ReadLine()) != null)
                {
                    string[] lineParts = line.Split(null);
                    if (lineParts.Length >= 2)
                    {
                        string key = lineParts[termIndex];
                        //Int64 count;
                        if (Int64.TryParse(lineParts[countIndex], out Int64 count))
                        {
                            CreateDictionaryEntry(key, language, count);
                        }
                    }
                }
            }
            return true;
        }

        //create a frequency dictionary from a corpus (merges with any dictionary data already loaded) 
        public bool CreateDictionary(string corpus, string language)
        {
            if (!File.Exists(corpus)) return false;

            using (StreamReader sr = new StreamReader(File.OpenRead(corpus)))
            {
                String line;
                //process a single line at a time only for memory efficiency
                while ((line = sr.ReadLine()) != null)
                {
                    foreach (string key in ParseWords(line))
                    {
                        CreateDictionaryEntry(key, language, 1);
                    }
                }
            }

            return true;
        }

        public List<SuggestItem> Lookup(string input, string language, int editDistanceMax)
        {
            return Lookup(input, language, editDistanceMax, this.defaultVerbose);
        }

        public List<SuggestItem> Lookup(string input, string language, int editDistanceMax, int verbose)
        {
            // editDistanceMax used in Lookup can't be bigger than the editDistanceMax use to construct
            // the underlying dictionary structure.
            //if (editDistanceMax > this.editDistanceMax) throw new ArgumentOutOfRangeException();

            //save some time
            if (input.Length - editDistanceMax > maxLength) return new List<SuggestItem>();

            List<string> candidates = new List<string>();
            HashSet<string> hashset1 = new HashSet<string>();

            List<SuggestItem> suggestions = new List<SuggestItem>();
            HashSet<string> hashset2 = new HashSet<string>();

            int editDistanceMax2 = editDistanceMax;

            int candidatePointer = 0;

            //add original term
            candidates.Add(input);

            //add original prefix
            if (input.Length > lp) candidates.Add(input.Substring(0, lp));

            var distanceComparer = new EditDistance(input, EditDistance.DistanceAlgorithm.Damerau);
            while (candidatePointer < candidates.Count)
            {
                string candidate = candidates[candidatePointer++];
                int lengthDiff = Math.Min(input.Length, lp) - candidate.Length;

                //save some time
                //early termination
                //suggestion distance=candidate.distance... candidate.distance+editDistanceMax                
                //if canddate distance is already higher than suggestion distance, than there are no better suggestions to be expected
                if ((verbose < 2) && (suggestions.Count > 0) && (lengthDiff > suggestions[0].distance)) goto sort;

                //read candidate entry from dictionary
                if (dictionary.TryGetValue(language + candidate, out int valueo))
                {
                    DictionaryItem value = new DictionaryItem();
                    if (valueo >= 0) value.suggestions.Add((Int32)valueo); else value = itemlist[-valueo - 1];

                    //if count>0 then candidate entry is correct dictionary term, not only delete item
                    if (value.count > 0)
                    {
                        int distance = input.Length - candidate.Length;

                        //save some time
                        //do not process higher distances than those already found, if verbose<2      
                        if ((distance <= editDistanceMax)
                        && ((verbose == 2) || (suggestions.Count == 0) || (distance <= suggestions[0].distance))
                        && (hashset2.Add(candidate)))
                        {
                            //Fix: previously not allways all suggestons within editdistance (verbose=1) or the best suggestion (verbose=0) were returned : e.g. elove did not return love
                            //suggestions.Clear() was not executed in this branch, if a suggestion with lower edit distance was added here (for verbose<2). 
                            //Then possibly suggestions with higher edit distance remained on top, the suggestion with lower edit distance were added to the end. 
                            //All of them where deleted later once a suggestion with a lower distance than the first item in the list was later added in the other branch. 
                            //Therefore returned suggestions were not always complete for verbose<2.
                            //remove all existing suggestions of higher distance, if verbose<2
                            if ((verbose < 2) && (suggestions.Count > 0) && (suggestions[0].distance > distance)) suggestions.Clear();

                            //add correct dictionary term term to suggestion list
                            SuggestItem si = new SuggestItem()
                            {
                                term = candidate,
                                count = value.count,
                                distance = distance
                            };
                            suggestions.Add(si);
                            //early termination
                            if ((verbose < 2) && (distance == 0)) goto sort;
                        }
                    }

                    //iterate through suggestions (to other correct dictionary items) of delete item and add them to suggestion list
                    foreach (int suggestionint in value.suggestions)
                    {
                        //save some time 
                        //skipping double items early: different deletes of the input term can lead to the same suggestion
                        //index2word
                        string suggestion = wordlist[suggestionint];

                        //True Damerau-Levenshtein Edit Distance: adjust distance, if both distances>0
                        //We allow simultaneous edits (deletes) of editDistanceMax on on both the dictionary and the input term. 
                        //For replaces and adjacent transposes the resulting edit distance stays <= editDistanceMax.
                        //For inserts and deletes the resulting edit distance might exceed editDistanceMax.
                        //To prevent suggestions of a higher edit distance, we need to calculate the resulting edit distance, if there are simultaneous edits on both sides.
                        //Example: (bank==bnak and bank==bink, but bank!=kanb and bank!=xban and bank!=baxn for editDistanceMaxe=1)
                        //Two deletes on each side of a pair makes them all equal, but the first two pairs have edit distance=1, the others edit distance=2.
                        int distance = 0;// editDistanceMax+1;
                        if (suggestion != input)
                        {
                            int min = 0;
                            if (Math.Abs(suggestion.Length - input.Length) > editDistanceMax2)
                            {
                                continue;
                            }
                            else if (candidate.Length == 0)
                            {
                                //suggestions which have no common chars with input (input.length<=editDistanceMax && suggestion.length<=editDistanceMax)
                                if (!hashset2.Add(suggestion)) continue; distance = Math.Max(input.Length, suggestion.Length);
                            }
                            else
                            //number of edits in prefix ==maxediddistance  AND no identic suffix, then editdistance>editdistancemax and no need for Levenshtein calculation  
                            //                                                 (input.Length >= lp) && (suggestion.Length >= lp) 
                            if ((lp - editDistanceMax == candidate.Length) && (((min = Math.Min(input.Length, suggestion.Length) - lp) > 1) && (input.Substring(input.Length + 1 - min) != suggestion.Substring(suggestion.Length + 1 - min))) || ((min > 0) && (input[input.Length - min] != suggestion[suggestion.Length - min]) && ((input[input.Length - min - 1] != suggestion[suggestion.Length - min]) || (input[input.Length - min] != suggestion[suggestion.Length - min - 1]))))
                            {
                                continue;
                            }
                            else
                            //edit distance of remaining string (after prefix)
                            if ((suggestion.Length == candidate.Length) && (input.Length <= lp)) { if (!hashset2.Add(suggestion)) continue; distance = input.Length - candidate.Length; }
                            else if ((input.Length == candidate.Length) && (suggestion.Length <= lp)) { if (!hashset2.Add(suggestion)) continue; distance = suggestion.Length - candidate.Length; }
                            else if (hashset2.Add(suggestion))
                            {
                                distance = distanceComparer.Compare(suggestion, editDistanceMax2);
                                if (distance < 0) distance = editDistanceMax + 1;
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else if (!hashset2.Add(suggestion)) continue;

                        //save some time
                        //do not process higher distances than those already found, if verbose<2
                        if ((verbose < 2) && (suggestions.Count > 0) && (distance > suggestions[0].distance)) continue;
                        if (distance <= editDistanceMax)
                        {
                            if (dictionary.TryGetValue(language + suggestion, out int value2))
                            {
                                SuggestItem si = new SuggestItem()
                                {
                                    term = suggestion,
                                    count = itemlist[-value2 - 1].count,
                                    distance = distance
                                };

                                //we will calculate DamLev distance only to the smallest found distance sof far
                                if (verbose < 2) editDistanceMax2 = distance;

                                //remove all existing suggestions of higher distance, if verbose<2
                                if ((verbose < 2) && (suggestions.Count > 0) && (suggestions[0].distance > distance)) suggestions.Clear();
                                suggestions.Add(si);
                            }
                        }

                    }//end foreach
                }//end if         

                //add edits 
                //derive edits (deletes) from candidate (input) and add them to candidates list
                //this is a recursive process until the maximum edit distance has been reached
                if ((lengthDiff < editDistanceMax) && (candidate.Length <= lp))
                {
                    //save some time
                    //do not create edits with edit distance smaller than suggestions already found
                    if ((verbose < 2) && (suggestions.Count > 0) && (lengthDiff >= suggestions[0].distance)) continue;

                    for (int i = 0; i < candidate.Length; i++)
                    {
                        string delete = candidate.Remove(i, 1);

                        if (hashset1.Add(delete)) { candidates.Add(delete); }
                    }
                }
            }//end while

            //sort by ascending edit distance, then by descending word frequency
            sort: if (verbose < 2) suggestions.Sort((x, y) => -x.count.CompareTo(y.count)); else suggestions.Sort((x, y) => 2 * x.distance.CompareTo(y.distance) - x.count.CompareTo(y.count));
            if ((verbose == 0) && (suggestions.Count > 1)) return suggestions.GetRange(0, 1); else return suggestions;
        }

        //create a non-unique wordlist from sample text
        //language independent (e.g. works with Chinese characters)
        private string[] ParseWords(string text)
        {
            // \w Alphanumeric characters (including non-latin characters, umlaut characters and digits) plus "_" 
            // \d Digits
            // Compatible with non-latin characters, does not split words at apostrophes
            MatchCollection mc = Regex.Matches(text.ToLower(), @"['’\w-[_]]+");

            //for benchmarking only: with CreateDictionary("big.txt","") and the text corpus from http://norvig.com/big.txt  the Regex below provides the exact same number of dictionary items as Norvigs regex "[a-z]+" (which splits words at apostrophes & incompatible with non-latin characters)     
            //MatchCollection mc = Regex.Matches(text.ToLower(), @"[\w-[\d_]]+");

            var matches = new string[mc.Count];
            for (int i = 0; i < matches.Length; i++) matches[i] = mc[i].ToString();
            return matches;
        }

        //inexpensive and language independent: only deletes, no transposes + replaces + inserts
        //replaces and inserts are expensive and language dependent (Chinese has 70,000 Unicode Han characters)
        private HashSet<string> Edits(string word, int editDistance, HashSet<string> deletes)
        {
            editDistance++;
            if (word.Length > 1)
            {
                for (int i = 0; i < word.Length; i++)
                {
                    string delete = word.Remove(i, 1);
                    if (deletes.Add(delete))
                    {
                        //recursion, if maximum edit distance not yet reached
                        if (editDistance < editDistanceMax) Edits(delete, editDistance, deletes);
                    }
                }
            }
            return deletes;
        }

        private HashSet<string> EditsPrefix(string key)
        {
            HashSet<string> hashSet = new HashSet<string>();
            if (key.Length <= editDistanceMax) hashSet.Add("");

            if (key.Length > lp)
            {
                hashSet.Add(key.Substring(0, lp));
                return Edits(key.Substring(0, lp), 0, hashSet);
            }
            else
            {
                return Edits(key, 0, hashSet);
            }
        }
    }
}
