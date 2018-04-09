// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett

using System;
using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    /// <summary>
    /// Class providing optimized methods for computing Damerau-Levenshtein Optimal String
    /// Alignment (OSA) comparisons between two strings.
    /// </summary>
    /// <remarks>
    /// Copyright ©2015-2018 SoftWx, Inc.
    /// The inspiration for creating highly optimized edit distance functions was 
    /// from Sten Hjelmqvist's "Fast, memory efficient" algorithm, described at
    /// http://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm
    /// The Damerau-Levenshtein algorithm is basically the Levenshtein algorithm with a 
    /// modification that considers transposition of two adjacent characters as a single edit.
    /// The optimized algorithm was described in detail in my post at 
    /// http://blog.softwx.net/2015/01/optimizing-damerau-levenshtein_15.html
    /// Also see http://en.wikipedia.org/wiki/Damerau%E2%80%93Levenshtein_distance
    /// Note that this implementation of Damerau-Levenshtein is the simpler and faster optimal
    /// string alignment (aka restricted edit) distance that difers slightly from the classic
    /// algorithm by imposing the restriction that no substring is edited more than once. So,
    /// for example, "CA" to "ABC" has an edit distance of 2 by a complete application of
    /// Damerau-Levenshtein, but has a distance of 3 by the method implemented here, that uses
    /// the optimal string alignment algorithm. This means that this algorithm is not a true
    /// metric since it does not uphold the triangle inequality. In real use though, this OSA
    /// version may be desired. Besides being faster, it does not give the lower distance score
    /// for transpositions that occur across long distances. Actual human error transpositions
    /// are most likely for adjacent characters. For example, the classic Damerau algorithm 
    /// gives a distance of 1 for these two strings: "sated" and "dates" (it counts the 's' and
    /// 'd' as a single transposition. The optimal string alignment version of Damerau in this
    /// class gives a distance of 2 for these two strings (2 substitutions), as it only counts
    /// transpositions for adjacent characters.
    /// The methods in this class are not threadsafe. Use the static versions in the Distance
    /// class if that is required.</remarks>
    public class DamerauOSA : IDistance {
        private int[] baseChar1Costs;
        private int[] basePrevChar1Costs;

        /// <summary>Create a new instance of DamerauOSA.</summary>
        public DamerauOSA() {
            this.baseChar1Costs = new int[0];
            this.basePrevChar1Costs = new int[0];
        }

        /// <summary>Create a new instance of DamerauOSA using the specified expected
        /// maximum string length that will be encountered.</summary>
        /// <remarks>By specifying the max expected string length, better memory efficiency
        /// can be achieved.</remarks>
        /// <param name="expectedMaxStringLength">The expected maximum length of strings that will
        /// be passed to the edit distance functions.</param>
        public DamerauOSA(int expectedMaxStringLength) {
            this.baseChar1Costs = new int[expectedMaxStringLength];
            this.basePrevChar1Costs = new int[expectedMaxStringLength];
        }

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>0 if the strings are equivalent, otherwise a positive number whose
        /// magnitude increases as difference between the strings increases.</returns>
        public double Distance(string string1, string string2) {
            if (string1 == null) return (string2 ?? "").Length;
            if (string2 == null) return string1.Length;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return len2;

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs);
        }

        /// <summary>Compute and return the Damerau-Levenshtein optimal string
        /// alignment edit distance between two strings.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match
        /// This method is not threadsafe.</remarks>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="maxDistance">The maximum distance that is of interest.</param>
        /// <returns>-1 if the distance is greater than the maxDistance, 0 if the strings
        /// are equivalent, otherwise a positive number whose magnitude increases as
        /// difference between the strings increases.</returns>
        public double Distance(string string1, string string2, double maxDistance) {
            if (string1 == null || string2 == null) return Helpers.NullDistanceResults(string1, string2, maxDistance);
            if (maxDistance <= 0) return (string1 == string2) ? 0 : -1;
            maxDistance = Math.Ceiling(maxDistance);
            int iMaxDistance = (maxDistance <= int.MaxValue) ? (int)maxDistance : int.MaxValue;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }
            if (string2.Length - string1.Length > iMaxDistance) return -1;

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return (len2 <= iMaxDistance) ? len2 : -1;

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance, this.baseChar1Costs, this.basePrevChar1Costs);
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs);
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two strings (1 - (damerau distance / len of longer string)).</summary>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <returns>The degree of similarity 0 to 1.0, where 0 represents a lack of any
        /// noteable similarity, and 1 represents equivalent strings.</returns>
        public double Similarity(string string1, string string2) {
            if (string1 == null) return (string2 == null) ? 1 : 0;
            if (string2 == null) return 0;

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return 1.0;

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs)
                .ToSimilarity(string2.Length);
        }

        /// <summary>Return Damerau-Levenshtein optimal string alignment similarity
        /// between two strings (1 - (damerau distance / len of longer string)).</summary>
        /// <param name="string1">One of the strings to compare.</param>
        /// <param name="string2">The other string to compare.</param>
        /// <param name="minSimilarity">The minimum similarity that is of interest.</param>
        /// <returns>The degree of similarity 0 to 1.0, where -1 represents a similarity
        /// lower than minSimilarity, otherwise, a number between 0 and 1.0 where 0
        /// represents a lack of any noteable similarity, and 1 represents equivalent
        /// strings.</returns>
        public double Similarity(string string1, string string2, double minSimilarity) {
            if (minSimilarity < 0 || minSimilarity > 1) throw new ArgumentException("minSimilarity must be in range 0 to 1.0");
            if (string1 == null || string2 == null) return Helpers.NullSimilarityResults(string1, string2, minSimilarity);

            // if strings of different lengths, ensure shorter string is in string1. This can result in a little
            // faster speed by spending more time spinning just the inner loop during the main processing.
            if (string1.Length > string2.Length) { var t = string1; string1 = string2; string2 = t; }

            int iMaxDistance = minSimilarity.ToDistance(string2.Length);
            if (string2.Length - string1.Length > iMaxDistance) return -1;
            if (iMaxDistance <= 0) return (string1 == string2) ? 1 : -1;

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return 1.0;

            if (len2 > this.baseChar1Costs.Length) {
                this.baseChar1Costs = new int[len2];
                this.basePrevChar1Costs = new int[len2];
            }
            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance, this.baseChar1Costs, this.basePrevChar1Costs)
                    .ToSimilarity(string2.Length);
            }
            return Distance(string1, string2, len1, len2, start, this.baseChar1Costs, this.basePrevChar1Costs)
                .ToSimilarity(string2.Length);
        }

        /// <summary>Internal implementation of the core Damerau-Levenshtein, optimal string alignment algorithm.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Distance(string string1, string string2, int len1, int len2, int start, int[] char1Costs, int[] prevChar1Costs) {
            int j;
            for (j = 0; j < len2;) char1Costs[j] = ++j;
            char char1 = ' ';
            int currentCost = 0;
            for (int i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = string1[start + i];
                char char2 = ' ';
                int leftCharCost, aboveCharCost;
                leftCharCost = aboveCharCost = i;
                int nextTransCost = 0;
                for (j = 0; j < len2; ++j) {
                    int thisTransCost = nextTransCost;
                    nextTransCost = prevChar1Costs[j];
                    prevChar1Costs[j] = currentCost = leftCharCost; // cost of diagonal (substitution)
                    leftCharCost = char1Costs[j];    // left now equals current cost (which will be diagonal at next iteration)
                    char prevChar2 = char2;
                    char2 = string2[start + j];
                    if (char1 != char2) {
                        //substitution if neither of two conditions below
                        if (aboveCharCost < currentCost) currentCost = aboveCharCost; // deletion
                        if (leftCharCost < currentCost) currentCost = leftCharCost;   // insertion
                        ++currentCost;
                        if ((i != 0) && (j != 0)
                            && (char1 == prevChar2)
                            && (prevChar1 == char2)
                            && (thisTransCost + 1 < currentCost)) { 
                            currentCost = thisTransCost + 1; // transposition
                        }
                    }
                    char1Costs[j] = aboveCharCost = currentCost;
                }
            }
            return currentCost;
        }

        /// <summary>Internal implementation of the core Damerau-Levenshtein, optimal string alignment algorithm
        /// that accepts a maxDistance.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        internal static int Distance(string string1, string string2, int len1, int len2, int start, int maxDistance, int[] char1Costs, int[] prevChar1Costs) {
#if DEBUG
            if (len2 < maxDistance) throw new ArgumentException();
            if (len2-len1 > maxDistance) throw new ArgumentException();
#endif
            int i, j;
            for (j = 0; j < maxDistance;) char1Costs[j] = ++j;
            for (; j < len2;) char1Costs[j++] = maxDistance + 1;
            int lenDiff = len2 - len1;
            int jStartOffset = maxDistance - lenDiff;
            int jStart = 0;
            int jEnd = maxDistance;
            char char1 = ' ';
            int currentCost = 0;
            for (i = 0; i < len1; ++i) {
                char prevChar1 = char1;
                char1 = string1[start + i];
                char char2 = ' ';
                int leftCharCost, aboveCharCost;
                leftCharCost = aboveCharCost = i;
                int nextTransCost = 0;
                // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                // and the upper left diagonal + maxDistance cells (upper left is i)
                jStart += (i > jStartOffset) ? 1 : 0;
                jEnd += (jEnd < len2) ? 1 : 0;
                for (j = jStart; j < jEnd; ++j) {
                    int thisTransCost = nextTransCost;
                    nextTransCost = prevChar1Costs[j];
                    prevChar1Costs[j] = currentCost = leftCharCost; // cost on diagonal (substitution)
                    leftCharCost = char1Costs[j];     // left now equals current cost (which will be diagonal at next iteration)
                    char prevChar2 = char2;
                    char2 = string2[start + j];
                    if (char1 != char2) {
                        // substitution if neither of two conditions below
                        if (aboveCharCost < currentCost) currentCost = aboveCharCost; // deletion
                        if (leftCharCost < currentCost) currentCost = leftCharCost;   // insertion
                        ++currentCost;
                        if ((i != 0) && (j != 0)
                            && (char1 == prevChar2)
                            && (prevChar1 == char2)
                            && (thisTransCost + 1 < currentCost)) {
                            currentCost = thisTransCost + 1; // transposition
                        }
                    }
                    char1Costs[j] = aboveCharCost = currentCost;
                }
                if (char1Costs[i + lenDiff] > maxDistance) return -1;
            }
            return (currentCost <= maxDistance) ? currentCost : -1;
        }
    }
}
/*
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/
