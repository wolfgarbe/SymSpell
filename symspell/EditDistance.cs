using System;

public class EditDistance {
    /// <summary>
    /// Computes and returns the Damerau-Levenshtein edit distance between two strings, 
    /// i.e. the number of insertion, deletion, sustitution, and transposition edits
    /// required to transform one string to the other. This value will be >= 0, where 0
    /// indicates identical strings. Comparisons are case sensitive, so for example, 
    /// "Fred" and "fred" will have a distance of 1. This algorithm is basically the
    /// Levenshtein algorithm with a modification that considers transposition of two
    /// adjacent characters as a single edit.
    /// http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html
    /// https://github.com/softwx/SoftWx.Match
    /// </summary>
    /// <remarks>See http://en.wikipedia.org/wiki/Damerau%E2%80%93Levenshtein_distance
    /// This is inspired by Sten Hjelmqvist'string1 "Fast, memory efficient" algorithm, described
    /// at http://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm.
    /// This version differs by adding additiona optimizations, and extending it to the Damerau-
    /// Levenshtein algorithm.
    /// Note that this is the simpler and faster optimal string alignment (aka restricted edit) distance
    /// that difers slightly from the classic Damerau-Levenshtein algorithm by imposing the restriction
    /// that no substring is edited more than once. So for example, "CA" to "ABC" has an edit distance
    /// of 2 by a complete application of Damerau-Levenshtein, but a distance of 3 by this method that
    /// uses the optimal string alignment algorithm. See wikipedia article for more detail on this
    /// distinction.
    /// </remarks>
    /// <license>
    /// The MIT License (MIT)
    ///
    ///Copyright(c) 2015 Steve Hatchett
    ///
    ///Permission is hereby granted, free of charge, to any person obtaining a copy
    ///of this software and associated documentation files(the "Software"), to deal
    ///in the Software without restriction, including without limitation the rights
    ///to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    ///copies of the Software, and to permit persons to whom the Software is
    ///furnished to do so, subject to the following conditions:
    ///
    ///The above copyright notice and this permission notice shall be included in all
    ///copies or substantial portions of the Software.
    ///
    ///THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    ///IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    ///FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    ///AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    ///LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    ///OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    ///SOFTWARE.
    /// </license>

    //Supported edit distance algorithms.
    public enum DistanceAlgorithm {
        Damerau
    }
    private string baseString;
    private DistanceAlgorithm algorithm;
    private int[] v0;
    private int[] v2; 
    /// <summary>Create a new EditDistance object.</summary>
    /// <param name="baseString">The base string to which other strings will be compared.</param>
    /// <param name="algorithm">The desired edit distance algorithm.</param>
    public EditDistance(string baseString, DistanceAlgorithm algorithm)
    {
        this.baseString = baseString;
        this.algorithm = algorithm;
        if (this.baseString == "")
        {
            this.baseString = null;
            return;
        }
        if (algorithm == DistanceAlgorithm.Damerau)
        {
            v0 = new int[baseString.Length];
            v2 = new int[baseString.Length]; // stores one level further back (offset by +1 position)
        }
    }
    /// <summary>Compare a string to the base string to determine the edit distance,
    /// using the previously selected algorithm.</summary>
    /// <param name="string2">The string to compare.</param>
    /// <param name="maxDistance">The maximum distance allowed.</param>
    /// <returns>The edit distance (or -1 if maxDistance exceeded).</returns>
    public int Compare(string string2, int maxDistance)
    {
        switch (algorithm) {
            case DistanceAlgorithm.Damerau: return DamerauLevenshteinDistance(string2, maxDistance);
        }
        throw new ArgumentException("unknown DistanceAlgorithm");
    }
    // stores one level further back (offset by +1 position)
    /// <param name="string1">String being compared for distance.</param>
    /// <param name="string2">String being compared against other string.</param>
    /// <param name="maxDistance">The maximum edit distance of interest.</param>
    /// <returns>int edit distance, >= 0 representing the number of edits required
    /// to transform one string to the other, or -1 if the distance is greater than the specified maxDistance.</returns>
    public int DamerauLevenshteinDistance(string string2, int maxDistance) {
        if (baseString == null) return (string2 ?? "").Length;
        if (String.IsNullOrEmpty(string2)) return baseString.Length;

        // if strings of different lengths, ensure shorter string is in string1. This can result in a little
        // faster speed by spending more time spinning just the inner loop during the main processing.
        string string1;
        if (baseString.Length > string2.Length) {
            string1 = string2;
            string2 = baseString;
        } else {
            string1 = baseString;
        }
        int sLen = string1.Length; // this is also the minimun length of the two strings
        int tLen = string2.Length;

        // suffix common to both strings can be ignored
        while ((sLen > 0) && (string1[sLen - 1] == string2[tLen - 1])) { sLen--; tLen--; }

        int start = 0;
        if ((string1[0] == string2[0]) || (sLen == 0)) { // if there'string1 a shared prefix, or all string1 matches string2'string1 suffix
                                                // prefix common to both strings can be ignored
            while ((start < sLen) && (string1[start] == string2[start])) start++;
            sLen -= start; // length of the part excluding common prefix and suffix
            tLen -= start;

            // if all of shorter string matches prefix and/or suffix of longer string, then
            // edit distance is just the delete of additional characters present in longer string
            if (sLen == 0) return tLen;

            string2 = string2.Substring(start, tLen); // faster than string2[start+j] in inner loop below
        }
        int lenDiff = tLen - sLen;
        if ((maxDistance < 0) || (maxDistance > tLen)) {
            maxDistance = tLen;
        } else if (lenDiff > maxDistance) return -1;

        if (tLen > v0.Length)
        {
            v0 = new int[tLen];
            v2 = new int[tLen];
        } else {
            Array.Clear(v2, 0, tLen);
        }
        int j;
        for (j = 0; j < maxDistance; j++) v0[j] = j + 1;
        for (; j < tLen; j++) v0[j] = maxDistance + 1;

        int jStartOffset = maxDistance - (tLen - sLen);
        bool haveMax = maxDistance < tLen;
        int jStart = 0;
        int jEnd = maxDistance;
        char sChar = string1[0];
        int current = 0;
        for (int i = 0; i < sLen; i++) {
            char prevsChar = sChar;
            sChar = string1[start + i];
            char tChar = string2[0];
            int left = i;
            current = left + 1;
            int nextTransCost = 0;
            // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
            // and the upper left diagonal + maxDistance cells (upper left is i)
            jStart += (i > jStartOffset) ? 1 : 0;
            jEnd += (jEnd < tLen) ? 1 : 0;
            for (j = jStart; j < jEnd; j++) {
                int above = current;
                int thisTransCost = nextTransCost;
                nextTransCost = v2[j];
                v2[j] = current = left; // cost of diagonal (substitution)
                left = v0[j];    // left now equals current cost (which will be diagonal at next iteration)
                char prevtChar = tChar;
                tChar = string2[j];
                if (sChar != tChar) {
                    if (left < current) current = left;   // insertion
                    if (above < current) current = above; // deletion
                    current++;
                    if ((i != 0) && (j != 0)
                        && (sChar == prevtChar)
                        && (prevsChar == tChar)) {
                        thisTransCost++;
                        if (thisTransCost < current) current = thisTransCost; // transposition
                    }
                }
                v0[j] = current;
            }
            if (haveMax && (v0[i + lenDiff] > maxDistance)) return -1;
        }
        return (current <= maxDistance) ? current : -1;
    }
}
