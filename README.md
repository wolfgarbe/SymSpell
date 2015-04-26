SymSpell
========

SymSpell: 1 million times faster through Symmetric Delete spelling correction algorithm

The Symmetric Delete spelling correction algorithm reduces the complexity of edit candidate generation and dictionary lookup for a given Damerau-Levenshtein distance. It is six orders of magnitude faster (than the standard approach with deletes + transposes + replaces + inserts) and language independent.

Opposite to other algorithms only deletes are required, no transposes + replaces + inserts.
Transposes + replaces + inserts of the input term are transformed into deletes of the dictionary term.
Replaces and inserts are expensive and language dependent: e.g. Chinese has 70,000 Unicode Han characters!

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

