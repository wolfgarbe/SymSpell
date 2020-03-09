Dictionary quality is paramount for correction quality.
These dictionaries have both reliable frequency values and correct vocabulary. This was achieved by intersecting the large Ngram datasets from [google Ngram](http://storage.googleapis.com/books/ngrams/books/datasetsv2.html) with wordlists generated from [hunspell dictionary files](https://github.com/wooorm/dictionaries).

Still, it sometimes might be a good idea to generate your own frequency dictionary which better fits your use-case.

The python script used for donwloading the Ngram dataset, decompressing it and merging it with existing wordlists is also provided.
