SymSpell
========

Spelling correction & Fuzzy search: **1 million times faster** through Symmetric Delete spelling correction algorithm

The Symmetric Delete spelling correction algorithm reduces the complexity of edit candidate generation and dictionary lookup for a given Damerau-Levenshtein distance. It is six orders of magnitude faster (than the standard approach with deletes + transposes + replaces + inserts) and language independent.

Opposite to other algorithms only deletes are required, no transposes + replaces + inserts.
Transposes + replaces + inserts of the input term are transformed into deletes of the dictionary term.
Replaces and inserts are expensive and language dependent: e.g. Chinese has 70,000 Unicode Han characters!

The speed comes from pre-calculation. An average 5 letter word has about **3 million possible spelling errors** within a maximum edit distance of 3, but with SymSpell you need to pre-calculate & store **only 25 deletes** to cover them all. Magic!

```
Copyright (C) 2015 Wolf Garbe
Version: 3.0
Author: Wolf Garbe <wolf.garbe@faroo.com>
Maintainer: Wolf Garbe <wolf.garbe@faroo.com>
URL: http://blog.faroo.com/2015/03/24/fast-approximate-string-matching-with-large-edit-distances/
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
<br>
##### Blog Posts: Algorithm, Benchmarks, Applications
[1000x Faster Spelling Correction algorithm](http://blog.faroo.com/2012/06/07/improved-edit-distance-based-spelling-correction/)<br>
[1000x Faster Spelling Correction: Source Code released](http://blog.faroo.com/2012/06/24/1000x-faster-spelling-correction-source-code-released/)<br>
[Fast approximate string matching with large edit distances in Big Data](http://blog.faroo.com/2015/03/24/fast-approximate-string-matching-with-large-edit-distances/)<br> 
[Very fast Data cleaning of product names, company names & street names](http://blog.faroo.com/2015/09/29/how-to-correct-company-names-street-names-product-names/) 
<br><br>
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
