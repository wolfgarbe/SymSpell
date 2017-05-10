SymSpell
========

Spelling correction & Fuzzy search: **1 million times faster** through Symmetric Delete spelling correction algorithm

The Symmetric Delete spelling correction algorithm reduces the complexity of edit candidate generation and dictionary lookup for a given Damerau-Levenshtein distance. It is six orders of magnitude faster (than the standard approach with deletes + transposes + replaces + inserts) and language independent.

Opposite to other algorithms only deletes are required, no transposes + replaces + inserts.
Transposes + replaces + inserts of the input term are transformed into deletes of the dictionary term.
Replaces and inserts are expensive and language dependent: e.g. Chinese has 70,000 Unicode Han characters!

The speed comes from pre-calculation. An average 5 letter word has about **3 million possible spelling errors** within a maximum edit distance of 3, but with SymSpell you need to pre-calculate & store **only 25 deletes** to cover them all. Magic!

```
Copyright (C) 2017 Wolf Garbe
Version: 4.0
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
Usage: single word + Enter:  Display spelling suggestions
       Enter without input:  Terminate the program



<br><br>
__UPDATE: see also [SymSpellCompound](https://github.com/wolfgarbe/SymSpellCompound)__
<br><br>
#### Performance

0.000033 seconds (edit distance 2) and 0.000180 seconds (edit distance 3) (single core on 2012 Macbook Pro)

#### Applications

Query correction, OCR post-processing, orthographic quality assessment, agent & chat bot conversation.

#### Frequency dictionary
The [word frequency list](https://github.com/wolfgarbe/symspell/blob/master/wordfrequency_en.txt) was created by intersecting the two lists mentioned below. By reciprocally filtering only those words which appear in both lists are used. Additional filters were applied and the resulting list truncated to &#8776; 80,000 most frequent words.
* [Google Books Ngram data](http://storage.googleapis.com/books/ngrams/books/datasetsv2.html)   [(License)](https://creativecommons.org/licenses/by/3.0/) : Provides representative word frequencies
* [SCOWL - Spell Checker Oriented Word Lists](http://wordlist.aspell.net/)   [(License)](http://wordlist.aspell.net/scowl-readme/) : Ensures genuine English vocabulary    
<br>
##### Blog Posts: Algorithm, Benchmarks, Applications
[1000x Faster Spelling Correction algorithm](http://blog.faroo.com/2012/06/07/improved-edit-distance-based-spelling-correction/)<br>
[1000x Faster Spelling Correction: Source Code released](http://blog.faroo.com/2012/06/24/1000x-faster-spelling-correction-source-code-released/)<br>
[Fast approximate string matching with large edit distances in Big Data](http://blog.faroo.com/2015/03/24/fast-approximate-string-matching-with-large-edit-distances/)<br> 
[Very fast Data cleaning of product names, company names & street names](http://blog.faroo.com/2015/09/29/how-to-correct-company-names-street-names-product-names/) 
<br>
##### Ports
The following third party ports to other programming languages have not been tested by myself whether they are an exact port, error free, provide identical results or are as fast as the original algorithm:


**C++** (third party port)<br>
https://github.com/erhanbaris/SymSpellPlusPlus

**Go** (third party port)<br>
https://github.com/heartszhang/symspell<br>
https://github.com/sajari/fuzzy

**Java** (third party port)<br>
https://github.com/gpranav88/symspell

**Javascript** (third party port)<br>
https://github.com/itslenny/SymSpell.js<br>
https://github.com/dongyuwei/SymSpell<br>
https://github.com/IceCreamYou/SymSpell<br>
https://github.com/Yomguithereal/mnemonist/blob/master/symspell.js

**Python** (third party port)<br>
https://github.com/ppgmg/spark-n-spell-1/blob/master/symspell_python.py

**Ruby** (third party port)<br>
https://github.com/PhilT/symspell

**Swift** (third party port)<br>
https://github.com/Archivus/SymSpell

---

#### Changes in v4.0
1. Fix: previously not always all suggestions within edit distance (verbose=1) or the best suggestion (verbose=0) were returned : e.g. "elove" did not return "love"
2. Regex will not anymore split words at apostrophes
3. Dictionary<string, object> dictionary = new Dictionary<string, object>(); changed to Dictionary<string, Int32> dictionary = new Dictionary<string, Int32>();
4. LoadDictionary() added to load a Frequency dictionary. CreateDictionary remains and can be used alternatively to create a dictionary from a large text corpus.
5. English word frequency dictionary added (wordfrequency_en.txt). Dictionary quality is paramount for correction quality. In order to achieve this two data sources were combined by intersection:
   Google Books Ngram data which provides representative word frequencies (but contains many entries with spelling errors) and SCOWL — Spell Checker Oriented Word Lists which ensures genuine English vocabulary (but contained no word frequencies required for ranking of suggestions within the same edit distance).
6. dictionaryItem.count was changed from Int32 to Int64 for compatibility with dictionaries derived from Google Ngram data.

