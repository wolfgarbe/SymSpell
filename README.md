SymSpell<br>
[![NuGet version](https://badge.fury.io/nu/symspell.svg)](https://badge.fury.io/nu/symspell)
========

Spelling correction & Fuzzy search: **1 million times faster** through Symmetric Delete spelling correction algorithm
 
The Symmetric Delete spelling correction algorithm reduces the complexity of edit candidate generation and dictionary lookup for a given Damerau-Levenshtein distance. It is six orders of magnitude faster ([than the standard approach with deletes + transposes + replaces + inserts](http://norvig.com/spell-correct.html)) and language independent.

Opposite to other algorithms only deletes are required, no transposes + replaces + inserts.
Transposes + replaces + inserts of the input term are transformed into deletes of the dictionary term.
Replaces and inserts are expensive and language dependent: e.g. Chinese has 70,000 Unicode Han characters!

The speed comes from pre-calculation. An average 5 letter word has about **3 million possible spelling errors** within a maximum edit distance of 3, but with SymSpell you need to pre-calculate & store **only 25 deletes** to cover them all. Magic!


### UPDATE: see also [SymSpellCompound](https://github.com/wolfgarbe/SymSpellCompound) for compound support (word split & merge)

<br>

```
Copyright (C) 2017 Wolf Garbe
Version: 6.0
Author: Wolf Garbe <wolf.garbe@faroo.com>
Maintainer: Wolf Garbe <wolf.garbe@faroo.com>
URL: https://github.com/wolfgarbe/symspell
Description: http://blog.faroo.com/2012/06/07/improved-edit-distance-based-spelling-correction/
License:
This program is free software; you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License, 
version 3.0 (LGPL-3.0) as published by the Free Software Foundation.
http://www.opensource.org/licenses/LGPL-3.0
```
#### Usage SymSpell Demo
single word + Enter:  Display spelling suggestions<br>
Enter without input:  Terminate the program

#### Usage SymSpell Library
```csharp
//create object
int initialCapacity = 82765;
int maxEditDistanceDictionary = 2; //maximum edit distance per dictionary precalculation
var symSpell = new SymSpell(initialCapacity, maxEditDistanceDictionary);
      
//load dictionary
string dictionaryPath="../../frequency_dictionary_en_82_765.txt";        
int termIndex = 0; //column of the term in the dictionary text file
int countIndex = 1; //column of the term frequency in the dictionary text file
symSpell.LoadDictionary(dictionaryPath, termIndex, countIndex);

//lookup suggestions
string inputTerm="house";
int maxEditDistanceLookup = 1; //max edit distance per lookup (maxEditDistanceLookup<=maxEditDistanceDictionary)
var suggestionVerbosity = SymSpell.Verbosity.Closest; //Top, Closest, All
var suggestions = symSpell.Lookup(inputTerm, suggestionVerbosity, maxEditDistanceLookup);

//display suggestions, edit distance and term frequency
foreach (var suggestion in suggestions)
{ 
  Console.WriteLine( suggestion.term + " " + suggestion.distance.ToString() + " " + suggestion.count.ToString());
}
```
Enabling the compiler option **"Prefer 32-bit"** will significantly **reduce the size** of the precalculated dictionary.

#### Performance

0.000033 seconds/word (edit distance 2) and 0.000180 seconds/word (edit distance 3) (single core on 2012 Macbook Pro)<br>

![Benchmark](https://cdn-images-1.medium.com/max/800/1*1l_5pOYU3AhoijKfVD-Qag.png "Benchmark")
<br><br>
**1,870 times faster than [BK-tree](https://en.wikipedia.org/wiki/BK-tree)** (see [Benchmark 1](https://medium.com/@wolfgarbe/symspell-vs-bk-tree-100x-faster-fuzzy-string-search-spell-checking-c4f10d80a078): dictionary size=500,000, maximum edit distance=3, query terms with random edit distance = 0...maximum edit distance, verbose=0)<br><br>
**1 million times faster than [Norvig's algorithm](http://norvig.com/spell-correct.html)** (see [Benchmark 2](http://blog.faroo.com/2015/03/24/fast-approximate-string-matching-with-large-edit-distances/): dictionary size=29,157, maximum edit distance=3, query terms with fixed edit distance = maximum edit distance, verbose=0)<br><br>


#### Applications

* Spelling correction
* Query correction (10–15% of queries contain misspelled terms),
* Chatbots,
* OCR post-processing,
* Automated proofreading.

#### Frequency dictionary
The [frequency_dictionary_en_82_765.txt](https://github.com/wolfgarbe/symspell/blob/master/symspell/frequency_dictionary_en_82_765.txt) was created by intersecting the two lists mentioned below. By reciprocally filtering only those words which appear in both lists are used. Additional filters were applied and the resulting list truncated to &#8776; 80,000 most frequent words.
* [Google Books Ngram data](http://storage.googleapis.com/books/ngrams/books/datasetsv2.html)   [(License)](https://creativecommons.org/licenses/by/3.0/) : Provides representative word frequencies
* [SCOWL - Spell Checker Oriented Word Lists](http://wordlist.aspell.net/)   [(License)](http://wordlist.aspell.net/scowl-readme/) : Ensures genuine English vocabulary    

#### Blog Posts: Algorithm, Benchmarks, Applications
[1000x Faster Spelling Correction algorithm](http://blog.faroo.com/2012/06/07/improved-edit-distance-based-spelling-correction/)<br>
[1000x Faster Spelling Correction: Source Code released](http://blog.faroo.com/2012/06/24/1000x-faster-spelling-correction-source-code-released/)<br>
[Fast approximate string matching with large edit distances in Big Data](http://blog.faroo.com/2015/03/24/fast-approximate-string-matching-with-large-edit-distances/)<br> 
[Very fast Data cleaning of product names, company names & street names](http://blog.faroo.com/2015/09/29/how-to-correct-company-names-street-names-product-names/)<br>
[Sub-millisecond compound aware automatic spelling correction](https://medium.com/@wolfgarbe/symspellcompound-10ec8f467c9b)<br>
[SymSpell vs. BK-tree: 100x faster fuzzy string search & spell checking](https://medium.com/@wolfgarbe/symspell-vs-bk-tree-100x-faster-fuzzy-string-search-spell-checking-c4f10d80a078)
<br><br>

**C#** (original source code)<br>
https://github.com/wolfgarbe/symspell

**.NET** (NuGet package)<br>
https://www.nuget.org/packages/symspell

#### Ports
The following third party ports or reimplementations to other programming languages have not been tested by myself whether they are an exact port, error free, provide identical results or are as fast as the original algorithm. Most of the ports target **SymSpell algorithm version 3.0** or earlier:


**C++** (third party port)<br>
https://github.com/erhanbaris/SymSpellPlusPlus

**Go** (third party port)<br>
https://github.com/heartszhang/symspell<br>
https://github.com/sajari/fuzzy

**Java** (third party port)<br>
https://github.com/gpranav88/symspell<br>
https://github.com/searchhub/preDict

**Javascript** (third party port)<br>
https://github.com/itslenny/SymSpell.js<br>
https://github.com/dongyuwei/SymSpell<br>
https://github.com/IceCreamYou/SymSpell<br>
https://github.com/Yomguithereal/mnemonist/blob/master/symspell.js

**Python** (third party port)<br>
https://github.com/ppgmg/github_public/blob/master/spell/symspell_python.py
https://github.com/rcourivaud/symspellcompound

**Ruby** (third party port)<br>
https://github.com/PhilT/symspell

**Scala** (third party reimplementation)<br>
https://github.com/semkath/symspell

**Swift** (third party port)<br>
https://github.com/Archivus/SymSpell

---

#### Changes in v6.0

1. IMPROVEMENT: SymSpell internal dictionary has been refactored by [Steve Hatchett](https://github.com/softwx).<br>
   2x faster dictionary precalculation and 2x lower memory consumption.

#### Changes in v5.1

1. IMPROVEMENT: SymSpell has been refactored from static to instantiated class by [Steve Hatchett](https://github.com/softwx).
2. IMPROVEMENT: Added benchmarking project. 
3. IMPROVEMENT: Added unit test project.
4. IMPROVEMENT:	Different maxEditDistance for dictionary precalculation and for Lookup. 
5. CHANGE: Removed language feature (use separate SymSpell instances instead).
6. CHANGE: Verbosity parameter changed from Int to Enum
7. FIX: Incomplete lookup results, if maxEditDistance=1 AND input.Length>prefixLength.
8. FIX: count overflow protection fixed.

#### Changes in v5.0
1. FIX: Suggestions were not always complete for input.Length <= editDistanceMax.
2. FIX: Suggestions were not always complete/best for verbose < 2.
3. IMPROVEMENT: Prefix indexing implemented: more than 90% memory reduction, depending on prefix length and edit distance.
   The discriminatory power of additional chars is decreasing with word length. 
   By restricting the delete candidate generation to the prefix, we can save space, without sacrificing filter efficiency too much. 
   Longer prefix length means higher search speed at the cost of higher index size.
4. IMPROVEMENT: Algorithm for DamerauLevenshteinDistance() changed for a faster one.
5. ParseWords() without LINQ
6. CreateDictionaryEntry simplified, AddLowestDistance() removed.
7. Lookup() improved.
8. Benchmark() added: Lookup of 1000 terms with random spelling errors.

#### Changes in v4.1
1. symspell.csproj Generates a [SymSpell NuGet package](https://www.nuget.org/packages/symspell) (which can be added to your project)
2. symspelldemo.csproj Shows how SymSpell can be used in your project (by using symspell.cs directly or by adding the [SymSpell NuGet package](https://www.nuget.org/packages/symspell) )

#### Changes in v4.0
1. Fix: previously not always all suggestions within edit distance (verbose=1) or the best suggestion (verbose=0) were returned : e.g. "elove" did not return "love"
2. Regex will not anymore split words at apostrophes
3. Dictionary<string, object> dictionary   changed to   Dictionary<string, Int32> dictionary
4. LoadDictionary() added to load a frequency dictionary. CreateDictionary remains and can be used alternatively to create a dictionary from a large text corpus.
5. English word frequency dictionary added (wordfrequency_en.txt). Dictionary quality is paramount for correction quality. In order to achieve this two data sources were combined by intersection:
   Google Books Ngram data which provides representative word frequencies (but contains many entries with spelling errors) and SCOWL — Spell Checker Oriented Word Lists which ensures genuine English vocabulary (but contained no word frequencies required for ranking of suggestions within the same edit distance).
6. dictionaryItem.count was changed from Int32 to Int64 for compatibility with dictionaries derived from Google Ngram data.
