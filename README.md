SymSpell<br>
[![NuGet version](https://badge.fury.io/nu/symspell.svg)](https://badge.fury.io/nu/symspell)
[![MIT License](https://img.shields.io/github/license/wolfgarbe/symspell.svg)](https://github.com/wolfgarbe/SymSpell/blob/master/LICENSE)
========

Spelling correction & Fuzzy search: **1 million times faster** through Symmetric Delete spelling correction algorithm
 
The Symmetric Delete spelling correction algorithm reduces the complexity of edit candidate generation and dictionary lookup for a given Damerau-Levenshtein distance. It is six orders of magnitude faster ([than the standard approach with deletes + transposes + replaces + inserts](http://norvig.com/spell-correct.html)) and language independent.

Opposite to other algorithms only deletes are required, no transposes + replaces + inserts.
Transposes + replaces + inserts of the input term are transformed into deletes of the dictionary term.
Replaces and inserts are expensive and language dependent: e.g. Chinese has 70,000 Unicode Han characters!

The speed comes from the inexpensive **delete-only edit candidate generation** and the **pre-calculation**.<br>
An average 5 letter word has about **3 million possible spelling errors** within a maximum edit distance of 3,<br>
but SymSpell needs to generate **only 25 deletes** to cover them all, both at pre-calculation and at lookup time. Magic!

If you like SymSpell, try [**SeekStorm**](https://github.com/SeekStorm/SeekStorm) - a sub-millisecond full-text search library & multi-tenancy server in Rust (Open Source).

<br>

```
Copyright (c) 2025 Wolf Garbe
Version: 6.7.3
Author: Wolf Garbe <wolf.garbe@seekstorm.com>
Maintainer: Wolf Garbe <wolf.garbe@seekstorm.com>
URL: https://github.com/wolfgarbe/symspell
Description: https://seekstorm.com/blog/1000x-spelling-correction/

MIT License

Copyright (c) 2025 Wolf Garbe

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

https://opensource.org/licenses/MIT
```

---

## Single word spelling correction

**Lookup** provides a very fast spelling correction of single words.
* A **Verbosity parameter** allows to control the number of returned results:<br>
Top: Top suggestion with the highest term frequency of the suggestions of smallest edit distance found.<br>
Closest: All suggestions of smallest edit distance found, suggestions ordered by term frequency.<br>
All: All suggestions within maxEditDistance, suggestions ordered by edit distance, then by term frequency.
* The **Maximum edit distance parameter** controls up to which edit distance words from the dictionary should be treated as suggestions.
* The required **Word frequency dictionary** can either be directly loaded from text files (**LoadDictionary**) or generated from a large text corpus (**CreateDictionary**).

#### Applications

* Spelling correction,
* Query correction (10–15% of queries contain misspelled terms),
* Chatbots,
* OCR post-processing,
* Automated proofreading.
* Fuzzy search & approximate string matching

#### Performance (single term)

0.033 milliseconds/word (edit distance 2) and 0.180 milliseconds/word (edit distance 3) (single core on 2012 Macbook Pro)<br>

![Benchmark](https://cdn-images-1.medium.com/max/800/1*1l_5pOYU3AhoijKfVD-Qag.png "Benchmark")
<br><br>
**1,870 times faster than [BK-tree](https://en.wikipedia.org/wiki/BK-tree)** (see [Benchmark 1](https://seekstorm.com/blog/symspell-vs-bk-tree/): dictionary size=500,000, maximum edit distance=3, query terms with random edit distance = 0...maximum edit distance, verbose=0)<br><br>
**1 million times faster than [Norvig's algorithm](http://norvig.com/spell-correct.html)** (see [Benchmark 2](http://blog.faroo.com/2015/03/24/fast-approximate-string-matching-with-large-edit-distances/): dictionary size=29,157, maximum edit distance=3, query terms with fixed edit distance = maximum edit distance, verbose=0)<br>

#### Blog Posts: Algorithm, Benchmarks, Applications
[1000x Faster Spelling Correction algorithm](https://seekstorm.com/blog/1000x-spelling-correction/)<br>
[Fast approximate string matching with large edit distances in Big Data](https://seekstorm.com/blog/fast-approximate-string-matching/)<br> 
[Very fast Data cleaning of product names, company names & street names](https://seekstorm.com/blog/very-data-cleaning-of-product-names-company-names-street-names/)<br>
[Sub-millisecond compound aware automatic spelling correction](https://seekstorm.com/blog/sub-millisecond-compound-aware-automatic.spelling-correction/)<br>
[SymSpell vs. BK-tree: 100x faster fuzzy string search & spell checking](https://seekstorm.com/blog/symspell-vs-bk-tree/)<br>
[Fast Word Segmentation for noisy text](https://seekstorm.com/blog/fast-word-segmentation-noisy-text/)<br>
[The Pruning Radix Trie — a Radix trie on steroids](https://seekstorm.com/blog/pruning-radix-trie/)<br>

---

## Compound aware multi-word spelling correction

**LookupCompound** supports __compound__ aware __automatic__ spelling correction of __multi-word input__ strings. 

__1. Compound splitting & decompounding__

Lookup() assumes every input string as _single term_. LookupCompound also supports _compound splitting / decompounding_ with three cases:
1. mistakenly __inserted space within a correct word__ led to two incorrect terms 
2. mistakenly __omitted space between two correct words__ led to one incorrect combined term
3. __multiple input terms__ with/without spelling errors

Splitting errors, concatenation errors, substitution errors, transposition errors, deletion errors and insertion errors can by mixed within the same word.

__2. Automatic spelling correction__

* Large document collections make manual correction infeasible and require unsupervised, fully-automatic spelling correction. 
* In conventional spelling correction of a single token, the user is presented with multiple spelling correction suggestions. <br>For automatic spelling correction of long multi-word text the algorithm itself has to make an educated choice.

__Examples:__

```diff
- whereis th elove hehad dated forImuch of thepast who couqdn'tread in sixthgrade and ins pired him
+ where is the love he had dated for much of the past who couldn't read in sixth grade and inspired him  (9 edits)

- in te dhird qarter oflast jear he hadlearned ofca sekretplan
+ in the third quarter of last year he had learned of a secret plan  (9 edits)

- the bigjest playrs in te strogsommer film slatew ith plety of funn
+ the biggest players in the strong summer film slate with plenty of fun  (9 edits)

- Can yu readthis messa ge despite thehorible sppelingmsitakes
+ can you read this message despite the horrible spelling mistakes  (9 edits)
```
#### Performance (compounds)

0.2 milliseconds / word (edit distance 2)
5000 words / second (single core on 2012 Macbook Pro)

---

## Word Segmentation of noisy text

**WordSegmentation** divides a string into words by inserting missing spaces at appropriate positions.<br>
* Misspelled words are corrected and do not prevent segmentation.<br>
* Existing spaces are allowed and considered for optimum segmentation.<br>
* SymSpell.WordSegmentation uses a [**Triangular Matrix approach**](https://seekstorm.com/blog/fast-word-segmentation-noisy-text/) instead of the conventional Dynamic Programming: It uses an array instead of a dictionary for memoization, loops instead of recursion and incrementally optimizes prefix strings instead of remainder strings.<br>
* The Triangular Matrix approach is faster than the Dynamic Programming approach. It has a lower memory consumption, better scaling (constant O(1) memory consumption vs. linear O(n)) and is GC friendly.
* While each string of length n can be segmented into **2^n−1** possible [compositions](https://en.wikipedia.org/wiki/Composition_(combinatorics)),<br> 
   SymSpell.WordSegmentation has a **linear runtime O(n)** to find the optimum composition.

__Examples:__

```diff
- thequickbrownfoxjumpsoverthelazydog
+ the quick brown fox jumps over the lazy dog

- itwasabrightcolddayinaprilandtheclockswerestrikingthirteen
+ it was a bright cold day in april and the clocks were striking thirteen

- itwasthebestoftimesitwastheworstoftimesitwastheageofwisdomitwastheageoffoolishness
+ it was the best of times it was the worst of times it was the age of wisdom it was the age of foolishness 
```

__Applications:__

* Word Segmentation for CJK languages for Indexing Spelling correction, Machine translation, Language understanding, Sentiment analysis
* Normalizing English compound nouns for search & indexing (e.g. ice box = ice-box = icebox; pig sty = pig-sty = pigsty) 
* Word segmentation for compounds if both original word and split word parts should be indexed.
* Correction of missing spaces caused by Typing errors.
* Correction of Conversion errors: spaces between word may get lost e.g. when removing line breaks.
* Correction of OCR errors: inferior quality of original documents or handwritten text may prevent that all spaces are recognized.
* Correction of Transmission errors: during the transmission over noisy channels spaces can get lost or spelling errors introduced.
* Keyword extraction from URL addresses, domain names, #hashtags, table column descriptions or programming variables written without spaces.
* For password analysis, the extraction of terms from passwords can be required.
* For Speech recognition, if spaces between words are not properly recognized in spoken language.
* Automatic CamelCasing of programming variables.
* Applications beyond Natural Language processing, e.g. segmenting DNA sequence into words

__Performance:__

4 milliseconds for segmenting an 185 char string into 53 words (single core on 2012 Macbook Pro)
<br>

---

#### Usage SymSpell Demo
single word + Enter:  Display spelling suggestions<br>
Enter without input:  Terminate the program

#### Usage SymSpellCompound Demo
multiple words + Enter: Display spelling suggestions<br>
Enter without input: Terminate the program

#### Usage Segmentation Demo
string without spaces + Enter: Display word segmented text<br>
Enter without input: Terminate the program

*Demo, DemoCompound and SegmentationDemo projects can be built with the free [Visual Studio Code](https://code.visualstudio.com/), which runs on Windows, MacOS and Linux.*

#### Usage SymSpell Library
```csharp
//create object
int initialCapacity = 82765;
int maxEditDistanceDictionary = 2; //maximum edit distance per dictionary precalculation
var symSpell = new SymSpell(initialCapacity, maxEditDistanceDictionary);
      
//load dictionary
string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
string dictionaryPath= baseDirectory + "../../../../SymSpell/frequency_dictionary_en_82_765.txt";
int termIndex = 0; //column of the term in the dictionary text file
int countIndex = 1; //column of the term frequency in the dictionary text file
if (!symSpell.LoadDictionary(dictionaryPath, termIndex, countIndex))
{
  Console.WriteLine("File not found!");
  //press any key to exit program
  Console.ReadKey();
  return;
}

//lookup suggestions for single-word input strings
string inputTerm="house";
int maxEditDistanceLookup = 1; //max edit distance per lookup (maxEditDistanceLookup<=maxEditDistanceDictionary)
var suggestionVerbosity = SymSpell.Verbosity.Closest; //Top, Closest, All
var suggestions = symSpell.Lookup(inputTerm, suggestionVerbosity, maxEditDistanceLookup);

//display suggestions, edit distance and term frequency
foreach (var suggestion in suggestions)
{ 
  Console.WriteLine(suggestion.term +" "+ suggestion.distance.ToString() +" "+ suggestion.count.ToString("N0"));
}


//load bigram dictionary
string dictionaryPath= baseDirectory + "../../../../SymSpell/frequency_bigramdictionary_en_243_342.txt";
int termIndex = 0; //column of the term in the dictionary text file
int countIndex = 2; //column of the term frequency in the dictionary text file
if (!symSpell.LoadBigramDictionary(dictionaryPath, termIndex, countIndex))
{
  Console.WriteLine("File not found!");
  //press any key to exit program
  Console.ReadKey();
  return;
}

//lookup suggestions for multi-word input strings (supports compound splitting & merging)
inputTerm="whereis th elove hehad dated forImuch of thepast who couqdn'tread in sixtgrade and ins pired him";
maxEditDistanceLookup = 2; //max edit distance per lookup (per single word, not per whole input string)
suggestions = symSpell.LookupCompound(inputTerm, maxEditDistanceLookup);

//display suggestions, edit distance and term frequency
foreach (var suggestion in suggestions)
{ 
  Console.WriteLine(suggestion.term +" "+ suggestion.distance.ToString() +" "+ suggestion.count.ToString("N0"));
}


//word segmentation and correction for multi-word input strings with/without spaces
inputTerm="thequickbrownfoxjumpsoverthelazydog";
maxEditDistance = 0;
suggestion = symSpell.WordSegmentation(input);

//display term and edit distance
Console.WriteLine(suggestion.correctedString + " " + suggestion.distanceSum.ToString("N0"));


//press any key to exit program
Console.ReadKey();
```
#### Three ways to add SymSpell to your project:
1. Add **[SymSpell.cs](https://github.com/wolfgarbe/SymSpell/blob/master/SymSpell/SymSpell.cs), [EditDistance.cs](https://github.com/wolfgarbe/SymSpell/blob/master/SymSpell/EditDistance.cs) and [frequency_dictionary_en_82_765.txt](https://github.com/wolfgarbe/SymSpell/blob/master/SymSpell/frequency_dictionary_en_82_765.txt)** to your project. All three files are located in the [SymSpell folder](https://github.com/wolfgarbe/SymSpell/tree/master/SymSpell). Enabling the compiler option **"Prefer 32-bit"** will significantly **reduce the memory consumption** of the precalculated dictionary.
2. Add **[SymSpell NuGet](https://www.nuget.org/packages/symspell)** to your **Net Framework** project: Visual Studio / Tools / NuGet Packager / Manage Nuget packages for solution / Select "Browse tab"/ Search for SymSpell / Select SymSpell / Check your project on the right hand windows / Click install button. The [frequency_dictionary_en_82_765.txt](https://github.com/wolfgarbe/SymSpell/blob/master/SymSpell/frequency_dictionary_en_82_765.txt) is **automatically installed**. 
3. Add **[SymSpell NuGet](https://www.nuget.org/packages/symspell)** to your **Net Core** project: Visual Studio / Tools / NuGet Packager / Manage Nuget packages for solution / Select "Browse tab"/ Search for SymSpell / Select SymSpell / Check your project on the right hand windows / Click install button. The [frequency_dictionary_en_82_765.txt](https://github.com/wolfgarbe/SymSpell/blob/master/SymSpell/frequency_dictionary_en_82_765.txt) must be **copied manually** to your project.

SymSpell targets [.NET Standard v2.0](https://blogs.msdn.microsoft.com/dotnet/2016/09/26/introducing-net-standard/) and can be used  in:
1. NET Framework (**Windows** Forms, WPF, ASP.NET), 
2. NET Core (UWP, ASP.NET Core, **Windows**, **OS X**, **Linux**),
3. XAMARIN (**iOS**, **OS X**, **Android**) projects.

*The SymSpell, Demo,  DemoCompound and Benchmark projects can be built with the free [Visual Studio Code](https://code.visualstudio.com/), which runs on Windows, MacOS and Linux.*

---

#### Frequency dictionary
Dictionary quality is paramount for correction quality. In order to achieve this two data sources were combined by intersection: Google Books Ngram data which provides representative word frequencies (but contains many entries with spelling errors) and SCOWL — Spell Checker Oriented Word Lists which ensures genuine English vocabulary (but contained no word frequencies required for ranking of suggestions within the same edit distance).

The [frequency_dictionary_en_82_765.txt](https://github.com/wolfgarbe/SymSpell/blob/master/SymSpell/frequency_dictionary_en_82_765.txt) was created by intersecting the two lists mentioned below. By reciprocally filtering only those words which appear in both lists are used. Additional filters were applied and the resulting list truncated to &#8776; 80,000 most frequent words.
* [Google Books Ngram data](http://storage.googleapis.com/books/ngrams/books/datasetsv2.html)   [(License)](https://creativecommons.org/licenses/by/3.0/) : Provides representative word frequencies
* [SCOWL - Spell Checker Oriented Word Lists](http://wordlist.aspell.net/)   [(License)](http://wordlist.aspell.net/scowl-readme/) : Ensures genuine English vocabulary    

#### Dictionary file format
* Plain text file in UTF-8 encoding.
* Word and Word Frequency are separated by space or tab. Per default, the word is expected in the first column and the frequency in the second column. But with the termIndex and countIndex parameters in LoadDictionary() the position and order of the values can be changed and selected from a row with more than two values. This allows to augment the dictionary with additional information or to adapt to existing dictionaries without reformatting.
* Every word-frequency-pair in a separate line. A line is defined as a sequence of characters followed by a line feed ("\n"), a carriage return ("\r"), or a carriage return immediately followed by a line feed ("\r\n").
* Both dictionary terms and input term are expected to be in **lower case**.

You can build your own frequency dictionary for your language or your specialized technical domain.
The SymSpell spelling correction algorithm supports languages with non-latin characters, e.g Cyrillic, Chinese or [Georgian](https://github.com/irakli97/Frequency_Dictionary_GE_363_202).

#### Frequency dictionaries in other languages

SymSpell includes an [English frequency dictionary](https://github.com/wolfgarbe/SymSpell/blob/master/SymSpell/frequency_dictionary_en_82_765.txt) 

Dictionaries for Chinese, English, French, German, Hebrew, Italian, Russian and Spanish are located here:<br>
[SymSpell.FrequencyDictionary](SymSpell.FrequencyDictionary)  

Frequency dictionaries in many other languages can be found here:<br>
[FrequencyWords repository](https://github.com/hermitdave/FrequencyWords)<br>
[Frequency dictionaries](https://github.com/dataiku/dss-plugin-nlp-preparation/tree/master/resource/dictionaries)<br>
[Frequency dictionaries](https://github.com/LuminosoInsight/wordfreq/tree/master/wordfreq/data)

N-Gram Generator by repetitio:<br>
This repository contains a script to generate unigrams and bigrams from Wikipedias dataset from HuggingFace for the use with the SymSpell.<br>
https://gitlab.com/repetitio/utils/ngram-frequencies/-/tree/main?ref_type=heads

---

**C#** (original source code)<br>
https://github.com/wolfgarbe/symspell

**.NET** (NuGet package)<br>
https://www.nuget.org/packages/symspell

### Ports

The following third party ports or reimplementations to other programming languages have not been tested by myself whether they are an exact port, error free, provide identical results or are as fast as the original algorithm. 

Most ports target SymSpell **version 3.0**. But **version 6.1.** provides **much higher speed & lower memory consumption!**

**WebAssembly**<br>
https://github.com/justinwilaby/spellchecker-wasm<br>

**WEB API (Docker)**<br>
https://github.com/LeonErath/SymSpellAPI (Version 6.3)<br>

**C++**<br>
https://github.com/AtheS21/SymspellCPP (Version 6.5)<br>
https://github.com/erhanbaris/SymSpellPlusPlus (Version 6.1)

**Crystal**<br>
https://github.com/chenkovsky/aha/blob/master/src/aha/sym_spell.cr

**Go**<br>
https://github.com/snapp-incubator/go-symspell<br>
https://github.com/sajari/fuzzy<br>
https://github.com/eskriett/spell

**Haskell**<br>
https://github.com/cbeav/symspell

**Java**<br>
https://github.com/MighTguY/customized-symspell (Version 6.6)<br>
https://github.com/rxp90/jsymspell (Version 6.6)<br>
https://github.com/Lundez/JavaSymSpell (Version 6.4)<br>
https://github.com/rxp90/jsymspell<br>
https://github.com/gpranav88/symspell<br>
https://github.com/searchhub/preDict<br>
https://github.com/jpsingarayar/SpellBlaze

**Javascript**<br>
https://github.com/MathieuLoutre/node-symspell (Version 6.6, needs Node.js)<br>
https://github.com/itslenny/SymSpell.js<br>
https://github.com/dongyuwei/SymSpell<br>
https://github.com/IceCreamYou/SymSpell<br>
https://github.com/Yomguithereal/mnemonist/blob/master/symspell.js

**Julia**<br>
https://github.com/Arkoniak/SymSpell.jl

**Kotlin**<br>
https://github.com/Wavesonics/SymSpellKt

**Objective-C**<br>
https://github.com/AmitBhavsarIphone/SymSpell (Version 6.3)

**PHP**<br>
https://github.com/Jakhotiya/symspell-php

**Python**<br>
https://github.com/mammothb/symspellpy  (Version 6.7)<br>
https://github.com/viig99/SymSpellCppPy  (Version 6.5)<br>
https://github.com/zoho-labs/symspell (Python bindings of Rust version)<br>
https://github.com/ne3x7/pysymspell/ (Version 6.1)<br>
https://github.com/Ayyuriss/SymSpell<br>
https://github.com/ppgmg/github_public/blob/master/spell/symspell_python.py<br>
https://github.com/rcourivaud/symspellcompound<br>
https://github.com/Esukhia/sympound-python<br>
https://www.kaggle.com/yk1598/symspell-spell-corrector

**Ruby**<br>
https://github.com/PhilT/symspell

**Rust**<br>
https://github.com/reneklacan/symspell (Version 6.6, compiles to WebAssembly)<br>
https://github.com/luketpeterson/fuzzy_rocks (persistent datastore backed by RocksDB)

**Scala**<br>
https://github.com/semkath/symspell

**Swift**<br>
https://github.com/gdetari/SymSpellSwift

**Zig**<br>
https://github.com/alim-zanibekov/miara?tab=readme-ov-file#symspell

---

### Citations

Contextual Multilingual Spellchecker for User Queries<br>
Sanat Sharma, Josep Valls-Vargas, Tracy Holloway King, Francois Guerin, Chirag Arora (Adobe)<br>
https://arxiv.org/abs/2305.01082

A context sensitive real-time Spell Checker with language adaptability<br>
Prabhakar Gupta (Amazon)<br>
https://arxiv.org/abs/1910.11242

SpeakGer: A meta-data enriched speech corpus of German state and federal parliaments<br>
Kai-Robin Lange and Carsten Jentsch<br>
https://arxiv.org/pdf/2410.17886

An Extended Sequence Tagging Vocabulary for Grammatical Error Correction<br>
Stuart Mesham, Christopher Bryant, Marek Rei, Zheng Yuan<br>
https://arxiv.org/abs/2302.05913

German Parliamentary Corpus (GERPARCOR)<br>
Giuseppe Abrami, Mevlüt Bagci, Leon Hammerla, Alexander Mehler<br>
https://arxiv.org/abs/2204.10422

iOCR: Informed Optical Character Recognition for Election Ballot Tallies<br>
Kenneth U. Oyibo, Jean D. Louis, Juan E. Gilbert<br>
https://arxiv.org/abs/2208.00865

Amazigh spell checker using Damerau-Levenshtein algorithm and N-gram<br>
Youness Chaabi, Fadoua Ataa Allah<br>
https://www.sciencedirect.com/science/article/pii/S1319157821001828

Survey of Query correction for Thai business-oriented information retrieval<br>
Phongsathorn Kittiworapanya, Nuttapong Saelek, Anuruth Lertpiya, Tawunrat Chalothorn<br>
https://ieeexplore.ieee.org/document/9376809

SymSpell and LSTM based Spell- Checkers for Tamil<br>
Selvakumar MuruganTamil Arasan BakthavatchalamTamil Arasan BakthavatchalamMalaikannan Sankarasubbu<br>
https://www.researchgate.net/publication/349924975_SymSpell_and_LSTM_based_Spell-_Checkers_for_Tamil

SymSpell4Burmese: Symmetric Delete Spelling Correction Algorithm (SymSpell) for Burmese Spelling Checking<br>
Ei Phyu Phyu Mon; Ye Kyaw Thu; Than Than Yu; Aye Wai Oo<br>
https://ieeexplore.ieee.org/document/9678171

Spell Check Indonesia menggunakan Norvig dan SymSpell<br>
Yasir Abdur Rohman<br>
https://medium.com/@yasirabd/spell-check-indonesia-menggunakan-norvig-dan-symspell-4fa583d62c24

Analisis Perbandingan Metode Burkhard Keller Tree dan SymSpell dalam Spell Correction Bahasa Indonesia<br>
Muhammad Hafizh Ferdiansyah, I Kadek Dwi Nuryana<br>
https://ejournal.unesa.ac.id/index.php/jinacs/article/download/50989/41739

Improving Document Retrieval with Spelling Correction for Weak and Fabricated Indonesian-Translated Hadith<br>
Muhammad zaky ramadhanKemas M LhaksmanaKemas M Lhaksmana<br>
https://www.researchgate.net/publication/342390145_Improving_Document_Retrieval_with_Spelling_Correction_for_Weak_and_Fabricated_Indonesian-Translated_Hadith

Symspell을 이용한 한글 맞춤법 교정<br>
김희규<br>
https://heegyukim.medium.com/symspell%EC%9D%84-%EC%9D%B4%EC%9A%A9%ED%95%9C-%ED%95%9C%EA%B8%80-%EB%A7%9E%EC%B6%A4%EB%B2%95-%EA%B5%90%EC%A0%95-3def9ca00805

Mending Fractured Texts. A heuristic procedure for correcting OCR data<br>
Jens Bjerring-Hansen, Ross Deans Kristensen-McLachla2, Philip Diderichsen and Dorte Haltrup Hansen<br>
https://ceur-ws.org/Vol-3232/paper14.pdf

Towards the Natural Language Processing as Spelling Correction for Offline Handwritten Text Recognition Systems<br>
Arthur Flor de Sousa Neto; Byron Leite Dantas Bezerra; and Alejandro Héctor Toselli<br>
https://www.mdpi.com/2076-3417/10/21/7711

When to Use OCR Post-correction for Named Entity Recognition?<br>
Vinh-Nam Huynh, Ahmed Hamdi, Antoine Doucet<br>
https://hal.science/hal-03034484v1/

Automatic error Correction: Evaluating Performance of Spell Checker Tools<br>
A. Tolegenova<br>
https://journals.sdu.edu.kz/index.php/nts/article/view/690

ZHAW-CAI: Ensemble Method for Swiss German Speech to Standard German Text<br>
Malgorzata Anna Ulasik, Manuela Hurlimann, Bogumila Dubel, Yves Kaufmann,<br>
Silas Rudolf, Jan Deriu, Katsiaryna Mlynchyk, Hans-Peter Hutter, and Mark Cieliebak<br>
https://ceur-ws.org/Vol-2957/sg_paper3.pdf

Cyrillic Word Error Program Based on Machine Learning<br>
Battumur, K., Dulamragchaa, U., Enkhbat, S., Altanhuyag, L., & Tumurbaatar, P.<br>
https://mongoliajol.info/index.php/JIMDT/article/view/2661

Fast Approximate String Search for Wikification<br>
Szymon Olewniczak, Julian Szymanski<br>
https://www.iccs-meeting.org/archive/iccs2021/papers/127440334.pdf

RuMedSpellchecker: Correcting Spelling Errors for Natural Russian Language in Electronic Health Records Using Machine Learning Techniques<br>
Dmitrii Pogrebnoi, Anastasia Funkner, Sergey Kovalchuk<br>
https://link.springer.com/chapter/10.1007/978-3-031-36024-4_16

An Extended Sequence Tagging Vocabulary for Grammatical Error Correction<br>
Stuart Mesham, Christopher Bryant, Marek Rei, Zheng Yuan<br>
https://aclanthology.org/2023.findings-eacl.119.pdf

Lightning-fast adaptive immune receptor similarity search by symmetric deletion lookup<br>
Touchchai Chotisorayuth, Andreas Tiffeau-Mayer<br>
https://arxiv.org/html/2403.09010v1

Unveiling Disguised Toxicity: A Novel Pre-processing Module for Enhanced Content Moderation<br>
Johnny Chan, Yuming Li<br>
https://www.sciencedirect.com/science/article/pii/S2215016124001225

Beyond the dictionary attack: Enhancing password cracking efficiency through machine learning-induced mangling rules<br>
Radek Hranický, Lucia Šírová, Viktor Rucký<br>
https://www.sciencedirect.com/science/article/pii/S2666281725000046

---

### Upcoming changes

1. Utilizing the [pigeonhole principle](https://en.wikipedia.org/wiki/Pigeonhole_principle) by partitioning both query and dictionary terms will result in 5x less memory consumption and 3x faster precalculation time. 
2. Option to preserve case (upper/lower case) of input term.
3. Open source the code for creating custom frequency dictionaries in any language and size as intersection between Google Books Ngram data (Provides representative word frequencies) and SCOWL Spell Checker Oriented Word Lists (Ensures genuine English vocabulary).

#### Changes in v6.7.3

- TargetFrameworks changed from `netstandard2.0;net461;net47;netcoreapp3.0` to `netstandard2.0;net9.0`.
- PackageReferences updated.
- In SymSpell.Test all Assert.AreEqual changed to Assert.That
- Incorporates PR #126 that fixes null reference exception in CommitStaged (#139).

#### Changes in v6.7.2

1. Exception fixed in WordSegmentation
2. Platform changed from netcore 2.1 to netcore 3.0

#### Changes in v6.7.1

1. Framework target changed from net472 to net47<br>
2. Framework target added netcoreapp3.0<br>
3. More common contractions added to frequency_dictionary_en_82_765.txt<br>

#### Changes in v6.7

1. WordSegmentation did not work correctly if input string contained words in uppercase.<br>
2. WordSegmentation now retains/preserves case.<br>
3. WordSegmentation now keeps punctuation or apostrophe adjacent to previous word.<br>
4. WordSegmentation now normalizes ligatures: "scientiﬁc" -> "scientific".<br>
5. WordSegmentation now removes hyphens prior to word segmentation (as they might be caused by syllabification).<br>
6. American English word forms added to dictionary in addition to British English e.g. favourable -> favorable.<br>

#### Changes in v6.6

1. IMPROVEMENT: LoadDictionary and LoadBigramDictionary now have an optional separator parameter, which defines the separator characters (e.g. '\t') between term(s) and count. Default is defaultSeparatorChars=null for white space.<br>
This allows the dictionaries to contain space separated phrases.<br>
If in LoadBigramDictionary no separator parameter is stated or defaultSeparatorChars (whitespace) is stated as separator parameter, then take two term parts, otherwise take only one (which then itself is a space separated bigram).

#### Changes in v6.5

1. IMPROVEMENT: Better SymSpell.LookupCompound correction quality with existing single term dictionary by using Naive Bayes probability for selecting best word splitting.<br>
`bycycle` -> `bicycle` (instead of  `by cycle` )<br>
`inconvient` -> `inconvenient` (instead of `i convent`)<br>
2. IMPROVEMENT: Even better SymSpell.LookupCompound correction quality, when using the optional bigram dictionary in order to use sentence level context information for selecting best spelling correction.<br>
3. IMPROVEMENT: English bigram frequency dictionary included

#### Changes in v6.4

1.	LoadDictioary(Stream, ...) and CreateDictionary(Stream) methods added (contibution by [ccady](https://github.com/ccady))<br>
	Allows to get dictionaries from network streams, memory streams, and resource streams in addition to previously supported files.

#### Changes in v6.3

1. IMPROVEMENT: WordSegmentation added:<br>
   WordSegmentation divides a string into words by inserting missing spaces at appropriate positions.<br>
   Misspelled words are corrected and do not prevent segmentation.<br>
   Existing spaces are allowed and considered for optimum segmentation.<br>
   SymSpell.WordSegmentation uses a [novel approach to word segmentation **without** recursion](https://seekstorm.com/blog/fast-word-segmentation-noisy-text/).<br>
   While each string of length n can be segmented into **2^n−1** possible [compositions](https://en.wikipedia.org/wiki/Composition_(combinatorics)),<br> 
   SymSpell.WordSegmentation has a **linear runtime O(n)** to find the optimum composition.
2. IMPROVEMENT: New CommandLine parameters:<br>
   LookupType: lookup, lookupcompound, wordsegment.<br>
   OutputStats: switch to show only corrected string or corrected string, edit distance, word frequency/probability.
3. IMPROVEMENT: Lookup with maxEditDistance=0 faster.

#### Changes in v6.2

1. IMPROVEMENT: SymSpell.CommandLine project added. Allows pipes and redirects for Input & Output.
   Dictionary/Copus file, MaxEditDistance, Verbosity, PrefixLength can be specified via Command Line. 
   No programming required.
2. IMPROVEMENT: DamerauOSA edit distance updated, Levenshtein edit distance added (in SoftWx.Match by [Steve Hatchett](https://github.com/softwx))
3. CHANGE: Other projects in the SymSpell solution now use references to SymSpell instead of links to the source files.

#### Changes in v6.1

1. IMPROVEMENT: [SymSpellCompound](https://github.com/wolfgarbe/SymSpellCompound) has been refactored from static to instantiated class and integrated into [SymSpell](https://github.com/wolfgarbe/SymSpell)
   Therefore SymSpellCompound is now also based on the latest SymSpell version with all fixes and performance improvements
2. IMPROVEMENT: symspell.demo.csproj, symspell.demoCompound.csproj, symspell.Benchmark.csproj have been recreated from scratch 
   and target now .Net Core instead of .Net Framework for improved compatibility with other platforms like MacOS and Linux
3. CHANGE: The testdata directory has been moved from the demo folder into the benchmark folder
4. CHANGE: License changed from LGPL 3.0 to the more permissive MIT license to allow frictionless commercial usage.

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

---

**SymSpell** is contributed by [**SeekStorm** - the high performance Search as a Service & search API](https://seekstorm.com)
