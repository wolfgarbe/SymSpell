// SymSpell: 1 million times faster through Symmetric Delete spelling correction algorithm
//
// The Symmetric Delete spelling correction algorithm reduces the complexity of edit candidate generation and dictionary lookup
// for a given Damerau-Levenshtein distance. It is six orders of magnitude faster and language independent.
// Opposite to other algorithms only deletes are required, no transposes + replaces + inserts.
// Transposes + replaces + inserts of the input term are transformed into deletes of the dictionary term.
// Replaces and inserts are expensive and language dependent: e.g. Chinese has 70,000 Unicode Han characters!
//
// SymSpell supports compound splitting / decompounding of multi-word input strings with three cases:
// 1. mistakenly inserted space into a correct word led to two incorrect terms 
// 2. mistakenly omitted space between two correct words led to one incorrect combined term
// 3. multiple independent input terms with/without spelling errors

// Copyright (C) 2022 Wolf Garbe
// Version: 6.7.2
// Author: Wolf Garbe wolf.garbe@seekstorm.com
// Maintainer: Wolf Garbe wolf.garbe@seekstorm.com
// URL: https://github.com/wolfgarbe/symspell
// Description: https://seekstorm.com/blog/1000x-spelling-correction/
//
// MIT License
// Copyright (c) 2022 Wolf Garbe
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// https://opensource.org/licenses/MIT

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
public class SymSpell
{
    /// <summary>Controls the closeness/quantity of returned spelling suggestions.</summary>
    public enum Verbosity
    {
        /// <summary>Top suggestion with the highest term frequency of the suggestions of smallest edit distance found.</summary>
        Top,
        /// <summary>All suggestions of smallest edit distance found, suggestions ordered by term frequency.</summary>
        Closest,
        /// <summary>All suggestions within maxEditDistance, suggestions ordered by edit distance
        /// , then by term frequency (slower, no early termination).</summary>
        All
    };

    const int defaultMaxEditDistance = 2;
    const int defaultPrefixLength = 7;
    const int defaultCountThreshold = 1;
    const int defaultInitialCapacity = 16;
    const int defaultCompactLevel = 5;
    const char[] defaultSeparatorChars = (char[])null;

    private readonly int initialCapacity;
    private readonly int maxDictionaryEditDistance;
    private readonly int prefixLength; //prefix length  5..7
    private readonly Int64 countThreshold; //a treshold might be specifid, when a term occurs so frequently in the corpus that it is considered a valid word for spelling correction
    private readonly uint compactMask;
    private readonly EditDistance.DistanceAlgorithm distanceAlgorithm = EditDistance.DistanceAlgorithm.DamerauOSA;
    private int maxDictionaryWordLength; //maximum dictionary term length

    // Dictionary that contains a mapping of lists of suggested correction words to the hashCodes
    // of the original words and the deletes derived from them. Collisions of hashCodes is tolerated,
    // because suggestions are ultimately verified via an edit distance function.
    // A list of suggestions might have a single suggestion, or multiple suggestions. 
    private Dictionary<int, string[]> deletes;
    // Dictionary of unique correct spelling words, and the frequency count for each word.
    private readonly Dictionary<string, Int64> words;
    // Dictionary of unique words that are below the count threshold for being considered correct spellings.
    private Dictionary<string, Int64> belowThresholdWords = new Dictionary<string, long>();

    /// <summary>Spelling suggestion returned from Lookup.</summary>
    public class SuggestItem : IComparable<SuggestItem>
    {
        /// <summary>The suggested correctly spelled word.</summary>
        public string term = "";
        /// <summary>Edit distance between searched for word and suggestion.</summary>
        public int distance = 0;
        /// <summary>Frequency of suggestion in the dictionary (a measure of how common the word is).</summary>
        public Int64 count = 0;

        /// <summary>Create a new instance of SuggestItem.</summary>
        /// <param name="term">The suggested word.</param>
        /// <param name="distance">Edit distance from search word.</param>
        /// <param name="count">Frequency of suggestion in dictionary.</param>
        public SuggestItem()
        {
        }
        public SuggestItem(string term, int distance, Int64 count)
        {
            this.term = term;
            this.distance = distance;
            this.count = count;
        }
        public int CompareTo(SuggestItem other)
        {
            // order by distance ascending, then by frequency count descending
            if (this.distance == other.distance) return other.count.CompareTo(this.count);
            return this.distance.CompareTo(other.distance);
        }
        public override bool Equals(object obj)
        {
            return Equals(term, ((SuggestItem)obj).term);
        }

        public override int GetHashCode()
        {
            return term.GetHashCode();
        }
        public override string ToString()
        {
            return "{" + term + ", " + distance + ", " + count + "}";
        }

        public SuggestItem ShallowCopy()
        {
            return (SuggestItem)MemberwiseClone();
        }
    }

    /// <summary>Maximum edit distance for dictionary precalculation.</summary>
    public int MaxDictionaryEditDistance { get { return this.maxDictionaryEditDistance; } }

    /// <summary>Length of prefix, from which deletes are generated.</summary>
    public int PrefixLength { get { return this.prefixLength; } }

    /// <summary>Length of longest word in the dictionary.</summary>
    public int MaxLength { get { return this.maxDictionaryWordLength; } }

    /// <summary>Count threshold for a word to be considered a valid word for spelling correction.</summary>
    public long CountThreshold { get { return this.countThreshold; } }

    /// <summary>Number of unique words in the dictionary.</summary>
    public int WordCount { get { return this.words.Count; } }

    /// <summary>Number of word prefixes and intermediate word deletes encoded in the dictionary.</summary>
    public int EntryCount { get { return this.deletes.Count; } }

    /// <summary>Create a new instanc of SymSpell.</summary>
    /// <remarks>Specifying ann accurate initialCapacity is not essential, 
    /// but it can help speed up processing by aleviating the need for 
    /// data restructuring as the size grows.</remarks>
    /// <param name="initialCapacity">The expected number of words in dictionary.</param>
    /// <param name="maxDictionaryEditDistance">Maximum edit distance for doing lookups.</param>
    /// <param name="prefixLength">The length of word prefixes used for spell checking.</param>
    /// <param name="countThreshold">The minimum frequency count for dictionary words to be considered correct spellings.</param>
    /// <param name="compactLevel">Degree of favoring lower memory use over speed (0=fastest,most memory, 16=slowest,least memory).</param>
    public SymSpell(int initialCapacity = defaultInitialCapacity, int maxDictionaryEditDistance = defaultMaxEditDistance
        , int prefixLength = defaultPrefixLength, int countThreshold = defaultCountThreshold
        , byte compactLevel = defaultCompactLevel)
    {
        if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
        if (maxDictionaryEditDistance < 0) throw new ArgumentOutOfRangeException(nameof(maxDictionaryEditDistance));
        if (prefixLength < 1 || prefixLength <= maxDictionaryEditDistance) throw new ArgumentOutOfRangeException(nameof(prefixLength));
        if (countThreshold < 0) throw new ArgumentOutOfRangeException(nameof(countThreshold));
        if (compactLevel > 16) throw new ArgumentOutOfRangeException(nameof(compactLevel));

        this.initialCapacity = initialCapacity;
        this.words = new Dictionary<string, Int64>(initialCapacity);
        this.maxDictionaryEditDistance = maxDictionaryEditDistance;
        this.prefixLength = prefixLength;
        this.countThreshold = countThreshold;
        if (compactLevel > 16) compactLevel = 16;
        this.compactMask = (uint.MaxValue >> (3 + compactLevel)) << 2;
    }

    /// <summary>Create/Update an entry in the dictionary.</summary>
    /// <remarks>For every word there are deletes with an edit distance of 1..maxEditDistance created and added to the
    /// dictionary. Every delete entry has a suggestions list, which points to the original term(s) it was created from.
    /// The dictionary may be dynamically updated (word frequency and new words) at any time by calling CreateDictionaryEntry</remarks>
    /// <param name="key">The word to add to dictionary.</param>
    /// <param name="count">The frequency count for word.</param>
    /// <param name="staging">Optional staging object to speed up adding many entries by staging them to a temporary structure.</param>
    /// <returns>True if the word was added as a new correctly spelled word,
    /// or false if the word is added as a below threshold word, or updates an
    /// existing correctly spelled word.</returns>
    public bool CreateDictionaryEntry(string key, Int64 count, SuggestionStage staging = null)
    {
        if (count <= 0)
        {
            if (this.countThreshold > 0) return false; // no point doing anything if count is zero, as it can't change anything
            count = 0;
        }
        Int64 countPrevious = -1;

        // look first in below threshold words, update count, and allow promotion to correct spelling word if count reaches threshold
        // threshold must be >1 for there to be the possibility of low threshold words
        if (countThreshold > 1 && belowThresholdWords.TryGetValue(key, out countPrevious))
        {
            // calculate new count for below threshold word
            count = (Int64.MaxValue - countPrevious > count) ? countPrevious + count : Int64.MaxValue;
            // has reached threshold - remove from below threshold collection (it will be added to correct words below)
            if (count >= countThreshold)
            {
                belowThresholdWords.Remove(key);
            }
            else
            {
                belowThresholdWords[key] = count;
                return false;
            }
        }
        else if (words.TryGetValue(key, out countPrevious))
        {
            // just update count if it's an already added above threshold word
            count = (Int64.MaxValue - countPrevious > count) ? countPrevious + count : Int64.MaxValue;
            words[key] = count;
            return false;
        }
        else if (count < CountThreshold)
        {
            // new or existing below threshold word
            belowThresholdWords[key] = count;
            return false;
        }

        // what we have at this point is a new, above threshold word 
        words.Add(key, count);

        //edits/suggestions are created only once, no matter how often word occurs
        //edits/suggestions are created only as soon as the word occurs in the corpus, 
        //even if the same term existed before in the dictionary as an edit from another word
        if (key.Length > maxDictionaryWordLength) maxDictionaryWordLength = key.Length;

	if (deletes == null) this.deletes = new Dictionary<int, string[]>(initialCapacity); //initialisierung

	//create deletes
        var edits = EditsPrefix(key);
        // if not staging suggestions, put directly into main data structure
        if (staging != null)
        {
            foreach (string delete in edits) staging.Add(GetStringHash(delete), key);
        }
        else
        {
            foreach (string delete in edits)
            {
                int deleteHash = GetStringHash(delete);
                if (deletes.TryGetValue(deleteHash, out string[] suggestions))
                {
                    var newSuggestions = new string[suggestions.Length + 1];
                    Array.Copy(suggestions, newSuggestions, suggestions.Length);
                    deletes[deleteHash] = suggestions = newSuggestions;
                }
                else
                {
                    suggestions = new string[1];
                    deletes.Add(deleteHash, suggestions);
                }
                suggestions[suggestions.Length - 1] = key;
            }
        }
        return true;
    }

    public Dictionary<string, long> bigrams = new Dictionary<string, long>();
    public long bigramCountMin = long.MaxValue;

    /// <summary>Load multiple dictionary entries from a file of word/frequency count pairs.</summary>
    /// <remarks>Merges with any dictionary data already loaded.</remarks>
    /// <param name="corpus">The path+filename of the file.</param>
    /// <param name="termIndex">The column position of the word.</param>
    /// <param name="countIndex">The column position of the frequency count.</param>
    /// <param name="separatorChars">Separator characters between term(s) and count.</param>
    /// <returns>True if file loaded, or false if file not found.</returns>
    public bool LoadBigramDictionary(string corpus, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        if (!File.Exists(corpus)) return false;
        using (Stream corpusStream = File.OpenRead(corpus))
        {
            return LoadBigramDictionary(corpusStream, termIndex, countIndex, separatorChars);
        }
    }

    /// <summary>Load multiple dictionary entries from a file of word/frequency count pairs.</summary>
    /// <remarks>Merges with any dictionary data already loaded.</remarks>
    /// <param name="corpus">The path+filename of the file.</param>
    /// <param name="termIndex">The column position of the word.</param>
    /// <param name="countIndex">The column position of the frequency count.</param>
    /// <param name="separatorChars">Separator characters between term(s) and count.</param>
    /// <returns>True if file loaded, or false if file not found.</returns>
    public bool LoadBigramDictionary(Stream corpusStream, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        using (StreamReader sr = new StreamReader(corpusStream, System.Text.Encoding.UTF8, false))
        {
            String line;
            int linePartsLenth = (separatorChars == defaultSeparatorChars) ? 3 : 2;
            //process a single line at a time only for memory efficiency
            while ((line = sr.ReadLine()) != null)
            {
                string[] lineParts = line.Split(separatorChars);

                if (lineParts.Length >= linePartsLenth)
                {
                    //if default (whitespace) is defined as separator take 2 term parts, otherwise take only one
                    string key = (separatorChars == defaultSeparatorChars) ? lineParts[termIndex] + " " + lineParts[termIndex + 1]: lineParts[termIndex];
                    //Int64 count;
                    if (Int64.TryParse(lineParts[countIndex], out Int64 count))
                    {
                        bigrams[key] = count;
                        if (count < bigramCountMin) bigramCountMin = count;
                    }
                }
            }
            
        }
        return true;
    }

    /// <summary>Load multiple dictionary entries from a file of word/frequency count pairs.</summary>
    /// <remarks>Merges with any dictionary data already loaded.</remarks>
    /// <param name="corpus">The path+filename of the file.</param>
    /// <param name="termIndex">The column position of the word.</param>
    /// <param name="countIndex">The column position of the frequency count.</param>
    /// <param name="separatorChars">Separator characters between term(s) and count.</param>
    /// <returns>True if file loaded, or false if file not found.</returns>
    public bool LoadDictionary(string corpus, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        if (!File.Exists(corpus)) return false;
        using (Stream corpusStream = File.OpenRead(corpus))
        {
            return LoadDictionary(corpusStream, termIndex, countIndex, separatorChars);
        }
    }

    /// <summary>Load multiple dictionary entries from a stream of word/frequency count pairs.</summary>
    /// <remarks>Merges with any dictionary data already loaded.</remarks>
    /// <param name="corpusStream">The stream containing the word/frequency count pairs.</param>
    /// <param name="termIndex">The column position of the word.</param>
    /// <param name="countIndex">The column position of the frequency count.</param>
    /// <param name="separatorChars">Separator characters between term(s) and count.</param>
    /// <returns>True if stream loads.</returns>
    public bool LoadDictionary(Stream corpusStream, int termIndex, int countIndex, char[] separatorChars = defaultSeparatorChars)
    {
        var staging = new SuggestionStage(16384);
        using (StreamReader sr = new StreamReader(corpusStream))
        {
            String line;
            //process a single line at a time only for memory efficiency
            while ((line = sr.ReadLine()) != null)
            {
                string[] lineParts = line.Split(separatorChars);
                if (lineParts.Length >= 2)
                {
                    string key = lineParts[termIndex];
                    //Int64 count;
                    if (Int64.TryParse(lineParts[countIndex], out Int64 count))
                    {
                        CreateDictionaryEntry(key, count, staging);
                    }
                }
            }
        }
        if (this.deletes == null) this.deletes = new Dictionary<int, string[]>(staging.DeleteCount);
        CommitStaged(staging);
        return true;
    }

    /// <summary>Load multiple dictionary words from a file containing plain text.</summary>
    /// <remarks>Merges with any dictionary data already loaded.</remarks>
    /// <param name="corpus">The path+filename of the file.</param>
    /// <returns>True if file loaded, or false if file not found.</returns>
    public bool CreateDictionary(string corpus)
    {
        if (!File.Exists(corpus)) return false;
        using (Stream corpusStream = File.OpenRead(corpus))
        {
            return CreateDictionary(corpusStream);
        }
    }

    /// <summary>Load multiple dictionary words from a stream containing plain text.</summary>
    /// <remarks>Merges with any dictionary data already loaded.</remarks>
    /// <param name="corpusStream">The stream containing the plain text.</param>
    /// <returns>True if stream loads.</returns>
    public bool CreateDictionary(Stream corpusStream)
    {
        var staging = new SuggestionStage(16384);
        using (StreamReader sr = new StreamReader(corpusStream))
        {
            String line;
            //process a single line at a time only for memory efficiency
            while ((line = sr.ReadLine()) != null)
            {
                foreach (string key in ParseWords(line))
                {
                    CreateDictionaryEntry(key, 1, staging);
                }
            }
        }
        if (this.deletes == null) this.deletes = new Dictionary<int, string[]>(staging.DeleteCount);
        CommitStaged(staging);
        return true;
    }

    /// <summary>Remove all below threshold words from the dictionary.</summary>
    /// <remarks>This can be used to reduce memory consumption after populating the dictionary from
    /// a corpus using CreateDictionary.</remarks>
    public void PurgeBelowThresholdWords()
    {
        belowThresholdWords = new Dictionary<string, long>();
    }

    /// <summary>Commit staged dictionary additions.</summary>
    /// <remarks>Used when you write your own process to load multiple words into the
    /// dictionary, and as part of that process, you first created a SuggestionsStage 
    /// object, and passed that to CreateDictionaryEntry calls.</remarks>
    /// <param name="staging">The SuggestionStage object storing the staged data.</param>
    public void CommitStaged(SuggestionStage staging)
    {
        staging.CommitTo(deletes);
    }

    /// <summary>Find suggested spellings for a given input word, using the maximum
    /// edit distance specified during construction of the SymSpell dictionary.</summary>
    /// <param name="input">The word being spell checked.</param>
    /// <param name="verbosity">The value controlling the quantity/closeness of the retuned suggestions.</param>
    /// <returns>A List of SuggestItem object representing suggested correct spellings for the input word, 
    /// sorted by edit distance, and secondarily by count frequency.</returns>
    public List<SuggestItem> Lookup(string input, Verbosity verbosity)
    {
        return Lookup(input, verbosity, this.maxDictionaryEditDistance, false);
    }
	
	/// <summary>Find suggested spellings for a given input word, using the maximum
    /// edit distance specified during construction of the SymSpell dictionary.</summary>
    /// <param name="input">The word being spell checked.</param>
    /// <param name="verbosity">The value controlling the quantity/closeness of the retuned suggestions.</param>
    /// <param name="maxEditDistance">The maximum edit distance between input and suggested words.</param>
    /// <returns>A List of SuggestItem object representing suggested correct spellings for the input word, 
    /// sorted by edit distance, and secondarily by count frequency.</returns>
    public List<SuggestItem> Lookup(string input, Verbosity verbosity, int maxEditDistance)
    {
        return Lookup(input, verbosity, maxEditDistance, false);
    }

    /// <summary>Find suggested spellings for a given input word.</summary>
    /// <param name="input">The word being spell checked.</param>
    /// <param name="verbosity">The value controlling the quantity/closeness of the retuned suggestions.</param>
    /// <param name="maxEditDistance">The maximum edit distance between input and suggested words.</param>
    /// <param name="includeUnknown">Include input word in suggestions, if no words within edit distance found.</param>																													   
    /// <returns>A List of SuggestItem object representing suggested correct spellings for the input word, 
    /// sorted by edit distance, and secondarily by count frequency.</returns>
    public List<SuggestItem> Lookup(string input, Verbosity verbosity, int maxEditDistance, bool includeUnknown)
    {
        //verbosity=Top: the suggestion with the highest term frequency of the suggestions of smallest edit distance found
        //verbosity=Closest: all suggestions of smallest edit distance found, the suggestions are ordered by term frequency 
        //verbosity=All: all suggestions <= maxEditDistance, the suggestions are ordered by edit distance, then by term frequency (slower, no early termination)

        // maxEditDistance used in Lookup can't be bigger than the maxDictionaryEditDistance
        // used to construct the underlying dictionary structure.
        if (maxEditDistance > MaxDictionaryEditDistance) throw new ArgumentOutOfRangeException(nameof(maxEditDistance));

        List<SuggestItem> suggestions = new List<SuggestItem>();
        int inputLen = input.Length;
        // early exit - word is too big to possibly match any words
        if (inputLen - maxEditDistance > maxDictionaryWordLength) goto end;

        // quick look for exact match
        long suggestionCount = 0;
        if (words.TryGetValue(input, out suggestionCount))
        {
            suggestions.Add(new SuggestItem(input, 0, suggestionCount));
            // early exit - return exact match, unless caller wants all matches
            if (verbosity != Verbosity.All) goto end;
        }

        //early termination, if we only want to check if word in dictionary or get its frequency e.g. for word segmentation
        if (maxEditDistance == 0) goto end;

        // deletes we've considered already
        HashSet<string> hashset1 = new HashSet<string>();
        // suggestions we've considered already
        HashSet<string> hashset2 = new HashSet<string>();		
		// we considered the input already in the word.TryGetValue above		
        hashset2.Add(input); 

        int maxEditDistance2 = maxEditDistance;
        int candidatePointer = 0;
        var singleSuggestion = new string[1] { string.Empty };
        List<string> candidates = new List<string>();

        //add original prefix
        int inputPrefixLen = inputLen;
        if (inputPrefixLen > prefixLength)
        {
            inputPrefixLen = prefixLength;
            candidates.Add(input.Substring(0, inputPrefixLen));
        }
        else
        {
            candidates.Add(input);
        }
        var distanceComparer = new EditDistance(this.distanceAlgorithm);
        while (candidatePointer < candidates.Count)
        {
            string candidate = candidates[candidatePointer++];
            int candidateLen = candidate.Length;
            int lengthDiff = inputPrefixLen - candidateLen;

            //save some time - early termination
            //if canddate distance is already higher than suggestion distance, than there are no better suggestions to be expected
            if (lengthDiff > maxEditDistance2)
            {
                // skip to next candidate if Verbosity.All, look no further if Verbosity.Top or Closest 
                // (candidates are ordered by delete distance, so none are closer than current)
                if (verbosity == Verbosity.All) continue;
                break;
            }

            //read candidate entry from dictionary
            if (deletes.TryGetValue(GetStringHash(candidate), out string[] dictSuggestions))
            {
                //iterate through suggestions (to other correct dictionary items) of delete item and add them to suggestion list
                for (int i = 0; i < dictSuggestions.Length; i++)
                {
                    var suggestion = dictSuggestions[i];
                    int suggestionLen = suggestion.Length;
                    if (suggestion == input) continue;
                    if ((Math.Abs(suggestionLen - inputLen) > maxEditDistance2) // input and sugg lengths diff > allowed/current best distance
                        || (suggestionLen < candidateLen) // sugg must be for a different delete string, in same bin only because of hash collision
                        || (suggestionLen == candidateLen && suggestion != candidate)) // if sugg len = delete len, then it either equals delete or is in same bin only because of hash collision
                        continue;
                    var suggPrefixLen = Math.Min(suggestionLen, prefixLength);
                    if (suggPrefixLen > inputPrefixLen && (suggPrefixLen - candidateLen) > maxEditDistance2) continue;

                    //True Damerau-Levenshtein Edit Distance: adjust distance, if both distances>0
                    //We allow simultaneous edits (deletes) of maxEditDistance on on both the dictionary and the input term. 
                    //For replaces and adjacent transposes the resulting edit distance stays <= maxEditDistance.
                    //For inserts and deletes the resulting edit distance might exceed maxEditDistance.
                    //To prevent suggestions of a higher edit distance, we need to calculate the resulting edit distance, if there are simultaneous edits on both sides.
                    //Example: (bank==bnak and bank==bink, but bank!=kanb and bank!=xban and bank!=baxn for maxEditDistance=1)
                    //Two deletes on each side of a pair makes them all equal, but the first two pairs have edit distance=1, the others edit distance=2.
                    int distance = 0;
                    int min = 0;
                    if (candidateLen == 0)
                    {
                        //suggestions which have no common chars with input (inputLen<=maxEditDistance && suggestionLen<=maxEditDistance)
                        distance = Math.Max(inputLen, suggestionLen);
                        if (distance > maxEditDistance2 || !hashset2.Add(suggestion)) continue;
                    }
                    else if (suggestionLen == 1)
                    {
                        if (input.IndexOf(suggestion[0]) < 0) distance = inputLen; else distance = inputLen - 1;
                        if (distance > maxEditDistance2 || !hashset2.Add(suggestion)) continue;
                    }
                    else
                    //number of edits in prefix ==maxediddistance  AND no identic suffix
                    //, then editdistance>maxEditDistance and no need for Levenshtein calculation  
                    //      (inputLen >= prefixLength) && (suggestionLen >= prefixLength) 
                    if ((prefixLength - maxEditDistance == candidateLen)
                        && (((min = Math.Min(inputLen, suggestionLen) - prefixLength) > 1)
                            && (input.Substring(inputLen + 1 - min) != suggestion.Substring(suggestionLen + 1 - min)))
                           || ((min > 0) && (input[inputLen - min] != suggestion[suggestionLen - min])
                               && ((input[inputLen - min - 1] != suggestion[suggestionLen - min])
                                   || (input[inputLen - min] != suggestion[suggestionLen - min - 1]))))
                    {
                        continue;
                    }
                    else
                    {
                        // DeleteInSuggestionPrefix is somewhat expensive, and only pays off when verbosity is Top or Closest.
                        if ((verbosity != Verbosity.All && !DeleteInSuggestionPrefix(candidate, candidateLen, suggestion, suggestionLen))
                            || !hashset2.Add(suggestion)) continue;
                        distance = distanceComparer.Compare(input, suggestion, maxEditDistance2);
                        if (distance < 0) continue;
                    }

                    //save some time
                    //do not process higher distances than those already found, if verbosity<All (note: maxEditDistance2 will always equal maxEditDistance when Verbosity.All)
                    if (distance <= maxEditDistance2)
                    {
                        suggestionCount = words[suggestion];
                        SuggestItem si = new SuggestItem(suggestion, distance, suggestionCount);
                        if (suggestions.Count > 0)
                        {
                            switch (verbosity)
                            {
                                case Verbosity.Closest:
                                    {
                                        //we will calculate DamLev distance only to the smallest found distance so far
                                        if (distance < maxEditDistance2) suggestions.Clear();
                                        break;
                                    }
                                case Verbosity.Top:
                                    {
                                        if (distance < maxEditDistance2 || suggestionCount > suggestions[0].count)
                                        {
                                            maxEditDistance2 = distance;
                                            suggestions[0] = si;
                                        }
                                        continue;
                                    }
                            }
                        }
                        if (verbosity != Verbosity.All) maxEditDistance2 = distance;
                        suggestions.Add(si);
                    }
                }//end foreach
            }//end if         

            //add edits 
            //derive edits (deletes) from candidate (input) and add them to candidates list
            //this is a recursive process until the maximum edit distance has been reached
            if ((lengthDiff < maxEditDistance) && (candidateLen <= prefixLength))
            {
                //save some time
                //do not create edits with edit distance smaller than suggestions already found
                if (verbosity != Verbosity.All && lengthDiff >= maxEditDistance2) continue;

                for (int i = 0; i < candidateLen; i++)
                {
                    string delete = candidate.Remove(i, 1);

                    if (hashset1.Add(delete)) { candidates.Add(delete); }
                }
            }
        }//end while

        //sort by ascending edit distance, then by descending word frequency
        if (suggestions.Count > 1) suggestions.Sort();
		end: if (includeUnknown && (suggestions.Count == 0)) suggestions.Add(new SuggestItem(input, maxEditDistance + 1, 0));																															 
        return suggestions;
    }//end if         
	

    /// <summary>An intentionally opacque class used to temporarily stage
    /// dictionary data during the adding of many words. By staging the
    /// data during the building of the dictionary data, significant savings
    /// of time can be achieved, as well as a reduction in final memory usage.</summary>
    public class SuggestionStage
    {
        private struct Node
        {
            public string suggestion;
            public int next;
        }
        private struct Entry
        {
            public int count;
            public int first;
        }
        private Dictionary<int, Entry> Deletes { get; set; }
        private ChunkArray<Node> Nodes { get; set; }
        /// <summary>Create a new instance of SuggestionStage.</summary>
        /// <remarks>Specifying ann accurate initialCapacity is not essential, 
        /// but it can help speed up processing by aleviating the need for 
        /// data restructuring as the size grows.</remarks>
        /// <param name="initialCapacity">The expected number of words that will be added.</param>
        public SuggestionStage(int initialCapacity)
        {
            Deletes = new Dictionary<int, Entry>(initialCapacity);
            Nodes = new ChunkArray<Node>(initialCapacity * 2);
        }
        /// <summary>Gets the count of unique delete words.</summary>
        public int DeleteCount { get { return Deletes.Count; } }
        /// <summary>Gets the total count of all suggestions for all deletes.</summary>
        public int NodeCount { get { return Nodes.Count; } }
        /// <summary>Clears all the data from the SuggestionStaging.</summary>
        public void Clear()
        {
            Deletes.Clear();
            Nodes.Clear();
        }
        internal void Add(int deleteHash, string suggestion)
        {
            if (!Deletes.TryGetValue(deleteHash, out Entry entry)) entry = new Entry { count = 0, first = -1 };
            int next = entry.first;
            entry.count++;
            entry.first = Nodes.Count;
            Deletes[deleteHash] = entry;
            Nodes.Add(new Node { suggestion = suggestion, next = next });
        }
        internal void CommitTo(Dictionary<int, string[]> permanentDeletes)
        {
            foreach (var keyPair in Deletes)
            {
                int i;
                if (permanentDeletes.TryGetValue(keyPair.Key, out string[] suggestions))
                {
                    i = suggestions.Length;
                    var newSuggestions = new string[suggestions.Length + keyPair.Value.count];
                    Array.Copy(suggestions, newSuggestions, suggestions.Length);
                    permanentDeletes[keyPair.Key] = suggestions = newSuggestions;
                }
                else
                {
                    i = 0;
                    suggestions = new string[keyPair.Value.count];
                    permanentDeletes.Add(keyPair.Key, suggestions);
                }
                int next = keyPair.Value.first;
                while (next >= 0)
                {
                    var node = Nodes[next];
                    suggestions[i] = node.suggestion;
                    next = node.next;
                    i++;
                }
            }
        }
    }

    //check whether all delete chars are present in the suggestion prefix in correct order, otherwise this is just a hash collision
    private bool DeleteInSuggestionPrefix(string delete, int deleteLen, string suggestion, int suggestionLen)
    {
        if (deleteLen == 0) return true;
        if (prefixLength < suggestionLen) suggestionLen = prefixLength;
        int j = 0;
        for (int i = 0; i < deleteLen; i++)
        {
            char delChar = delete[i];
            while (j < suggestionLen && delChar != suggestion[j]) j++;
            if (j == suggestionLen) return false;
        }
        return true;
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
    private HashSet<string> Edits(string word, int editDistance, HashSet<string> deleteWords)
    {
        editDistance++;
        if (word.Length > 1)
        {
            for (int i = 0; i < word.Length; i++)
            {
                string delete = word.Remove(i, 1);
                if (deleteWords.Add(delete))
                {
                    //recursion, if maximum edit distance not yet reached
                    if (editDistance < maxDictionaryEditDistance) Edits(delete, editDistance, deleteWords);
                }
            }
        }
        return deleteWords;
    }

    private HashSet<string> EditsPrefix(string key)
    {
        HashSet<string> hashSet = new HashSet<string>();
        if (key.Length <= maxDictionaryEditDistance) hashSet.Add("");
        if (key.Length > prefixLength) key = key.Substring(0, prefixLength);
        hashSet.Add(key);
        return Edits(key, 0, hashSet);
    }

    private int GetStringHash(string s)
    {
        //return s.GetHashCode();

        int len = s.Length;
        int lenMask = len;
        if (lenMask > 3) lenMask = 3;

        uint hash = 2166136261;
        for (var i = 0; i < len; i++)
        {
            unchecked
            {
                hash ^= s[i];
                hash *= 16777619;
            }
        }

        hash &= this.compactMask;
        hash |= (uint)lenMask;
        return (int)hash;
    }

    // A growable list of elements that's optimized to support adds, but not deletes,
    // of large numbers of elements, storing data in a way that's friendly to the garbage
    // collector (not backed by a monolithic array object), and can grow without needing
    // to copy the entire backing array contents from the old backing array to the new.
    private class ChunkArray<T>
    {
        private const int ChunkSize = 4096; //this must be a power of 2, otherwise can't optimize Row and Col functions
        private const int DivShift = 12; // number of bits to shift right to do division by ChunkSize (the bit position of ChunkSize)
        public T[][] Values { get; private set; }
        public int Count { get; private set; }
        public ChunkArray(int initialCapacity)
        {
            int chunks = (initialCapacity + ChunkSize - 1) / ChunkSize;
            Values = new T[chunks][];
            for (int i = 0; i < Values.Length; i++) Values[i] = new T[ChunkSize];
        }
        public int Add(T value)
        {
            if (Count == Capacity)
            {
                var newValues = new T[Values.Length + 1][];
                // only need to copy the list of array blocks, not the data in the blocks
                Array.Copy(Values, newValues, Values.Length);
                newValues[Values.Length] = new T[ChunkSize];
                Values = newValues;
            }
            Values[Row(Count)][Col(Count)] = value;
            Count++;
            return Count - 1;
        }
        public void Clear()
        {
            Count = 0;
        }
        public T this[int index]
        {
            get { return Values[Row(index)][Col(index)]; }
            set { Values[Row(index)][Col(index)] = value; }
        }
        private int Row(int index) { return index >> DivShift; } // same as index / ChunkSize
        private int Col(int index) { return index & (ChunkSize - 1); } //same as index % ChunkSize
        private int Capacity { get { return Values.Length * ChunkSize; } }
    }

    //######################

    //LookupCompound supports compound aware automatic spelling correction of multi-word input strings with three cases:
    //1. mistakenly inserted space into a correct word led to two incorrect terms 
    //2. mistakenly omitted space between two correct words led to one incorrect combined term
    //3. multiple independent input terms with/without spelling errors

    /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
    /// <param name="input">The string being spell checked.</param>																										   
    /// <returns>A List of SuggestItem object representing suggested correct spellings for the input string.</returns> 
    public List<SuggestItem> LookupCompound(string input)
    {
        return LookupCompound(input, this.maxDictionaryEditDistance);
    }

    /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
    /// <param name="input">The string being spell checked.</param>
    /// <param name="maxEditDistance">The maximum edit distance between input and suggested words.</param>																											   
    /// <returns>A List of SuggestItem object representing suggested correct spellings for the input string.</returns> 
    public List<SuggestItem> LookupCompound(string input, int editDistanceMax)
    {
        //parse input string into single terms
        string[] termList1 = ParseWords(input);

        List<SuggestItem> suggestions = new List<SuggestItem>();     //suggestions for a single term
        List<SuggestItem> suggestionParts = new List<SuggestItem>(); //1 line with separate parts
        var distanceComparer = new EditDistance(this.distanceAlgorithm);

        //translate every term to its best suggestion, otherwise it remains unchanged
        bool lastCombi = false;
        for (int i = 0; i < termList1.Length; i++)
        {
            suggestions = Lookup(termList1[i], Verbosity.Top, editDistanceMax);

            //combi check, always before split
            if ((i > 0) && !lastCombi)
            {
                List<SuggestItem> suggestionsCombi = Lookup(termList1[i - 1] + termList1[i], Verbosity.Top, editDistanceMax);

                if (suggestionsCombi.Count > 0)
                {
                    SuggestItem best1 = suggestionParts[suggestionParts.Count - 1];
                    SuggestItem best2 = new SuggestItem();
                    if (suggestions.Count > 0)
                    {
                        best2 = suggestions[0];
                    }
                    else
                    {
                        //unknown word
                        best2.term = termList1[i];
                        //estimated edit distance
                        best2.distance = editDistanceMax + 1;
                        //estimated word occurrence probability P=10 / (N * 10^word length l)
                        best2.count = (long)((double)10 / Math.Pow((double)10, (double)best2.term.Length)); // 0;
                    }

                    //distance1=edit distance between 2 split terms und their best corrections : als comparative value for the combination
                    int distance1 = best1.distance + best2.distance;
                    if ((distance1 >= 0) && ((suggestionsCombi[0].distance + 1 < distance1) || ((suggestionsCombi[0].distance + 1 == distance1) && ((double)suggestionsCombi[0].count > (double)best1.count / (double)SymSpell.N * (double)best2.count))))
                    {
                        suggestionsCombi[0].distance++;
                        suggestionParts[suggestionParts.Count - 1] = suggestionsCombi[0];
                        lastCombi = true;
                        goto nextTerm;
                    }
                }
            }
            lastCombi = false;

            //alway split terms without suggestion / never split terms with suggestion ed=0 / never split single char terms
            if ((suggestions.Count > 0) && ((suggestions[0].distance == 0) || (termList1[i].Length == 1)))
            {
                //choose best suggestion
                suggestionParts.Add(suggestions[0]);
            }
            else
            {
                //if no perfect suggestion, split word into pairs
                SuggestItem suggestionSplitBest = null;

                //add original term 
                if (suggestions.Count > 0) suggestionSplitBest = suggestions[0];

                if (termList1[i].Length > 1)
                {
                    for (int j = 1; j < termList1[i].Length; j++)
                    {
                        string part1 = termList1[i].Substring(0, j);
                        string part2 = termList1[i].Substring(j);
                        SuggestItem suggestionSplit = new SuggestItem();
                        List<SuggestItem> suggestions1 = Lookup(part1, Verbosity.Top, editDistanceMax);
                        if (suggestions1.Count > 0)
                        {
                            List<SuggestItem> suggestions2 = Lookup(part2, Verbosity.Top, editDistanceMax);
                            if (suggestions2.Count > 0)
                            {
                                //select best suggestion for split pair
                                suggestionSplit.term = suggestions1[0].term + " " + suggestions2[0].term;

                                int distance2 = distanceComparer.Compare(termList1[i], suggestionSplit.term, editDistanceMax);
                                if (distance2 < 0) distance2 = editDistanceMax + 1;

                                if (suggestionSplitBest != null)
                                {
                                    if (distance2 > suggestionSplitBest.distance) continue;
                                    if (distance2 < suggestionSplitBest.distance) suggestionSplitBest = null;
                                }

                                suggestionSplit.distance = distance2;
                                //if bigram exists in bigram dictionary
                                if (bigrams.TryGetValue(suggestionSplit.term, out long bigramCount))
                                {
                                    suggestionSplit.count = bigramCount;

                                    //increase count, if split.corrections are part of or identical to input  
                                    //single term correction exists
                                    if (suggestions.Count > 0)
                                    {
                                        //alternatively remove the single term from suggestionsSplit, but then other splittings could win
                                        if ((suggestions1[0].term + suggestions2[0].term == termList1[i]))
                                        {
                                            //make count bigger than count of single term correction
                                            suggestionSplit.count = Math.Max(suggestionSplit.count, suggestions[0].count + 2);
                                        }
                                        else if ((suggestions1[0].term == suggestions[0].term) || (suggestions2[0].term == suggestions[0].term))
                                        {
                                            //make count bigger than count of single term correction
                                            suggestionSplit.count = Math.Max(suggestionSplit.count, suggestions[0].count + 1);
                                        }
                                    }
                                    //no single term correction exists
                                    else if ((suggestions1[0].term + suggestions2[0].term == termList1[i]))
                                    {
                                        suggestionSplit.count = Math.Max(suggestionSplit.count, Math.Max(suggestions1[0].count, suggestions2[0].count) + 2);
                                    }

                                }
                                else
                                {
                                    //The Naive Bayes probability of the word combination is the product of the two word probabilities: P(AB) = P(A) * P(B)
                                    //use it to estimate the frequency count of the combination, which then is used to rank/select the best splitting variant  
                                    suggestionSplit.count = Math.Min(bigramCountMin, (long)((double)suggestions1[0].count / (double)SymSpell.N * (double)suggestions2[0].count));
                                }

                                if ((suggestionSplitBest == null) || (suggestionSplit.count > suggestionSplitBest.count)) suggestionSplitBest = suggestionSplit;
                            }
                        }
                    }

                    if (suggestionSplitBest != null)
                    {
                        //select best suggestion for split pair
                        suggestionParts.Add(suggestionSplitBest);
                    }
                    else
                    {
                        SuggestItem si = new SuggestItem();
                        si.term = termList1[i];
                        //estimated word occurrence probability P=10 / (N * 10^word length l)
                        si.count = (long)((double)10 / Math.Pow((double)10, (double)si.term.Length));
                        si.distance = editDistanceMax + 1;
                        suggestionParts.Add(si);
                    }
                }
                else
                {
                    SuggestItem si = new SuggestItem();
                    si.term = termList1[i];
                    //estimated word occurrence probability P=10 / (N * 10^word length l)
                    si.count = (long)((double)10 / Math.Pow((double)10, (double)si.term.Length));
                    si.distance = editDistanceMax + 1;
                    suggestionParts.Add(si);
                }
            }
        nextTerm:;
        }

        SuggestItem suggestion = new SuggestItem();

        double count = SymSpell.N;
        System.Text.StringBuilder s = new System.Text.StringBuilder();
        foreach (SuggestItem si in suggestionParts) { s.Append(si.term + " "); count *= (double)si.count / (double)SymSpell.N; }
        suggestion.count = (long)count;

        suggestion.term = s.ToString().TrimEnd();
        suggestion.distance = distanceComparer.Compare(input, suggestion.term, int.MaxValue);

        List<SuggestItem> suggestionsLine = new List<SuggestItem>();
        suggestionsLine.Add(suggestion);
        return suggestionsLine;
    }

    //######

    //WordSegmentation divides a string into words by inserting missing spaces at the appropriate positions
    //misspelled words are corrected and do not affect segmentation
    //existing spaces are allowed and considered for optimum segmentation

    //SymSpell.WordSegmentation uses a novel approach *without* recursion.
    //https://seekstorm.com/blog/fast-word-segmentation-noisy-text/
    //While each string of length n can be segmentend in 2^n−1 possible compositions https://en.wikipedia.org/wiki/Composition_(combinatorics)
    //SymSpell.WordSegmentation has a linear runtime O(n) to find the optimum composition

    //number of all words in the corpus used to generate the frequency dictionary
    //this is used to calculate the word occurrence probability p from word counts c : p=c/N
    //N equals the sum of all counts c in the dictionary only if the dictionary is complete, but not if the dictionary is truncated or filtered
    public static long N = 1024908267229L;

    /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
    /// <param name="input">The string being spell checked.</param>
    /// <returns>The word segmented string, 
    /// the word segmented and spelling corrected string, 
    /// the Edit distance sum between input string and corrected string, 
    /// the Sum of word occurence probabilities in log scale (a measure of how common and probable the corrected segmentation is).</returns> 
    public (string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum) WordSegmentation(string input)
    {
        return WordSegmentation(input, this.MaxDictionaryEditDistance, this.maxDictionaryWordLength);
    }

    /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
    /// <param name="input">The string being spell checked.</param>
    /// <param name="maxEditDistance">The maximum edit distance between input and corrected words 
    /// (0=no correction/segmentation only).</param>	
    /// <returns>The word segmented string, 
    /// the word segmented and spelling corrected string, 
    /// the Edit distance sum between input string and corrected string, 
    /// the Sum of word occurence probabilities in log scale (a measure of how common and probable the corrected segmentation is).</returns> 
    public (string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum) WordSegmentation(string input, int maxEditDistance)
    {
        return WordSegmentation(input, maxEditDistance, this.maxDictionaryWordLength);
    }

    /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
    /// <param name="input">The string being spell checked.</param>
    /// <param name="maxSegmentationWordLength">The maximum word length that should be considered.</param>	
    /// <param name="maxEditDistance">The maximum edit distance between input and corrected words 
    /// (0=no correction/segmentation only).</param>	
    /// <returns>The word segmented string, 
    /// the word segmented and spelling corrected string, 
    /// the Edit distance sum between input string and corrected string, 
    /// the Sum of word occurence probabilities in log scale (a measure of how common and probable the corrected segmentation is).</returns> 
    public (string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum) WordSegmentation(string input, int maxEditDistance, int maxSegmentationWordLength)
    {
        //v6.7
        //normalize ligatures: 
        //"scientific"
        //"scientiﬁc" "ﬁelds" "ﬁnal"
        input = input.Normalize(System.Text.NormalizationForm.FormKC).Replace("\u002D", "");//.Replace("\uC2AD","");

        int arraySize = Math.Min(maxSegmentationWordLength, input.Length);
        (string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum)[] compositions = new(string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum)[arraySize];
        int circularIndex = -1;

        //outer loop (column): all possible part start positions
        for (int j = 0; j < input.Length; j++)
        {
            //inner loop (row): all possible part lengths (from start position): part can't be bigger than longest word in dictionary (other than long unknown word)
            int imax = Math.Min(input.Length - j, maxSegmentationWordLength);
            for (int i = 1; i <= imax; i++)
            {
                //get top spelling correction/ed for part
                string part = input.Substring(j, i);
                int separatorLength = 0;
                int topEd = 0;
                decimal topProbabilityLog = 0;
                string topResult = "";

                if (Char.IsWhiteSpace(part[0]))
                {
                    //remove space for levensthein calculation
                    part = part.Substring(1);
                }
                else
                {
                    //add ed+1: space did not exist, had to be inserted
                    separatorLength = 1;
                }

                //remove space from part1, add number of removed spaces to topEd                
                topEd += part.Length;
                //remove space
                part = part.Replace(" ", ""); //=System.Text.RegularExpressions.Regex.Replace(part1, @"\s+", "");
                                              //add number of removed spaces to ed
                topEd -= part.Length;

                //v6.7
                //Lookup against the lowercase term
                List<SymSpell.SuggestItem> results = this.Lookup(part.ToLower(), SymSpell.Verbosity.Top, maxEditDistance);
                if (results.Count > 0)
                {
                    topResult = results[0].term;
                    //v6.7
                    //retain/preserve upper case 
                    if ((part.Length>0) && Char.IsUpper(part[0]))
                    {
                        char[] a = topResult.ToCharArray();
                        a[0] = char.ToUpper(topResult[0]);
                        topResult = new string(a);
                    }

                    topEd += results[0].distance;
                    //Naive Bayes Rule
                    //we assume the word probabilities of two words to be independent
                    //therefore the resulting probability of the word combination is the product of the two word probabilities

                    //instead of computing the product of probabilities we are computing the sum of the logarithm of probabilities
                    //because the probabilities of words are about 10^-10, the product of many such small numbers could exceed (underflow) the floating number range and become zero
                    //log(ab)=log(a)+log(b)
                    topProbabilityLog = (decimal)Math.Log10((double)results[0].count / (double)N);
                }
                else
                {
                    topResult = part;
                    //default, if word not found
                    //otherwise long input text would win as long unknown word (with ed=edmax+1 ), although there there should many spaces inserted 
                    topEd += part.Length;
                    topProbabilityLog = (decimal)Math.Log10(10.0 / (N * Math.Pow(10.0, part.Length)));
                }

                int destinationIndex = ((i + circularIndex) % arraySize);

                //set values in first loop
                if (j == 0)
                {
                    compositions[destinationIndex] = (part, topResult, topEd, topProbabilityLog);
                }
                else if ((i == maxSegmentationWordLength)
                    //replace values if better probabilityLogSum, if same edit distance OR one space difference 
                    || (((compositions[circularIndex].distanceSum + topEd == compositions[destinationIndex].distanceSum) || (compositions[circularIndex].distanceSum + separatorLength + topEd == compositions[destinationIndex].distanceSum)) && (compositions[destinationIndex].probabilityLogSum < compositions[circularIndex].probabilityLogSum + topProbabilityLog))
                    //replace values if smaller edit distance     
                    || (compositions[circularIndex].distanceSum + separatorLength + topEd < compositions[destinationIndex].distanceSum))
                {
                    //v6.7
                    //keep punctuation or spostrophe adjacent to previous word
                    if (((topResult.Length == 1) && char.IsPunctuation(topResult[0])) || ((topResult.Length == 2) && topResult.StartsWith("’")))
                    {
                        compositions[destinationIndex] = (
                        compositions[circularIndex].segmentedString + part,
                        compositions[circularIndex].correctedString + topResult,
                        compositions[circularIndex].distanceSum + topEd,
                        compositions[circularIndex].probabilityLogSum + topProbabilityLog);
                    }
                    else
                    {
                        compositions[destinationIndex] = (
                        compositions[circularIndex].segmentedString + " " + part,
                        compositions[circularIndex].correctedString + " " + topResult,
                        compositions[circularIndex].distanceSum + separatorLength + topEd,
                        compositions[circularIndex].probabilityLogSum + topProbabilityLog);
                    }
                }
            }
            circularIndex++; if (circularIndex == arraySize) circularIndex = 0;
        }
        return compositions[circularIndex];
    }


}
