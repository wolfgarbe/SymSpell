import urllib.request
import gzip
import shutil
import os.path

url = "http://storage.googleapis.com/books/ngrams/books/googlebooks-{lang}-all-1gram-20120701-{letter}.gz"
max_final_words = 100000

# For each language you need a '{lang}-wordlist.txt' wordlist file
langs = ['chi-sim', 'eng', 'fre', 'ger', 'heb', 'ita', 'rus', 'spa']
letters = ['a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o',
           'other', 'p', 'pos', 'punctuation', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z']


def download(url, file_name):
    try:
        u = urllib.request.urlopen(url)
    except urllib.error.HTTPError as err:
        print("Did not find: "+file_name)
        return 0

    f = open(file_name, "wb")
    file_size = int(u.info().get("Content-Length"))
    print("Downloading: %s Bytes: %s" % (file_name, file_size))

    file_size_dl = 0
    block_sz = 8192
    percent = 0
    broadcast = 0
    while True:
        buffer = u.read(block_sz)
        if not buffer:
            break

        file_size_dl += len(buffer)
        f.write(buffer)
        percent = file_size_dl * 100.0 / file_size
        if (percent > broadcast + 7.5):
            broadcast = percent
            status = r"%10d  [%3.2f%%]" % (file_size_dl, broadcast)
            status = status + chr(8) * (len(status) + 1)
            print(status)
    f.close()
    return percent


# Download files from google Ngrams
for lang in langs:
    for letter in letters:
        percent = download(url.format(
            lang=lang, letter=letter), lang+"-"+letter+".gz")
        print("Done with download for {} letter {} at {}%".format(
            lang, letter, percent))

# Decompress google Ngram archives
for lang in langs:
    for letter in letters:
        f_name = lang+'-'+letter+'.gz'
        if os.path.isfile(f_name):
            with open(lang+'-'+letter+'.txt', 'wb') as f_out, gzip.open(f_name, 'rb') as f_in:
                f_out.write(f_in.read())
            print("Done decompressing - "+f_name)
        else:
            print("Not found - "+f_name)

# Merge Ngram files with wordlists
for lang in langs:
    words = {}
    with open(lang+'-wordlist.txt', 'r', encoding="UTF-8") as dic:
        for line in dic:
            words[line.split()[0].lower()] = 0

    for letter in letters:
        if os.path.isfile(lang+'-'+letter+'.txt'):
            with open(lang+'-'+letter+'.txt', 'r', encoding="UTF-8") as f_in:
                for line in f_in:
                    data = line.split()
                    name = data[0].lower()
                    if name in words:
                        words[name] += int(data[2])
            print("Done merging {} letter {}!".format(lang, letter))

    num = 0
    with open(lang+'-frequency.txt', 'a+', encoding="UTF-8") as f_out:
        for word, count in {k: v for k, v in sorted(words.items(), key=lambda item: item[1], reverse=True)}.items():
            num += 1
            f_out.write(word+" "+str(count)+"\n")
            if num >= max_final_words:
                break

    print(">> Done with lang "+lang)
