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
Version: 5.0
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
#### Usage
single word + Enter:  Display spelling suggestions<br>
Enter without input:  Terminate the program

#### Performance

0.000033 seconds/word (edit distance 2) and 0.000180 seconds/word (edit distance 3) (single core on 2012 Macbook Pro)<br>
[Benchmark 1](http://blog.faroo.com/2015/03/24/fast-approximate-string-matching-with-large-edit-distances/)<br>
[Benchmark 2](https://medium.com/@wolfgarbe/symspell-vs-bk-tree-100x-faster-fuzzy-string-search-spell-checking-c4f10d80a078)

#### Applications

* Query correction (10–15% of queries contain misspelled terms),
* Chatbots,
* OCR post-processing,
* Automated proofreading.

#### Frequency dictionary
The [word frequency list](https://github.com/wolfgarbe/symspell/blob/master/wordfrequency_en.txt) was created by intersecting the two lists mentioned below. By reciprocally filtering only those words which appear in both lists are used. Additional filters were applied and the resulting list truncated to &#8776; 80,000 most frequent words.
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
The following third party ports to other programming languages have not been tested by myself whether they are an exact port, error free, provide identical results or are as fast as the original algorithm:


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

**Ruby** (third party port)<br>
https://github.com/PhilT/symspell

**Swift** (third party port)<br>
https://github.com/Archivus/SymSpell

---

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
