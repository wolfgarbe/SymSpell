// Copyright ©2015-2018 SoftWx, Inc.
// Released under the MIT License the text of which appears at the end of this file.
// <authors> Steve Hatchett
using System;
using System.Runtime.CompilerServices;

namespace SoftWx.Match {
    /// <summary>
    /// Class providing optimized methods for computing Levenshtein comparisons between two strings.
    /// </summary>
    /// <remarks>
    /// Copyright ©2015-2018 SoftWx, Inc.
    /// The inspiration for creating highly optimized edit distance functions was 
    /// from Sten Hjelmqvist's "Fast, memory efficient" algorithm, described at
    /// http://www.codeproject.com/Articles/13525/Fast-memory-efficient-Levenshtein-algorithm
    /// The Levenshtein algorithm computes the edit distance metric between two strings, i.e.
    /// the number of insertion, deletion, and substitution edits required to transform one
    /// string to the other. This value will be >= 0, where 0 indicates identical strings.
    /// Comparisons are case sensitive, so for example, "Fred" and "fred" will have a 
    /// distance of 1. The optimized algorithm was described in detail in my post at
    /// http://blog.softwx.net/2014/12/optimizing-levenshtein-algorithm-in-c.html
    /// Also see http://en.wikipedia.org/wiki/Levenshtein_distance for general information.
    /// The methods in this class are not threadsafe. Use the static versions in the Distance
    /// class if that is required.</remarks>
    public class Levenshtein : IDistance, ISimilarity {
        private int[] baseChar1Costs;

        /// <summary>Create a new instance of Levenshtein.</summary>
        public Levenshtein() {
            this.baseChar1Costs = new int[0];
        }

        /// <summary>Create a new instance of Levenshtein using the specified expected
        /// maximum string length that will be encountered.</summary>
        /// <remarks>By specifying the max expected string length, better memory efficiency
        /// can be achieved.</remarks>
        /// <param name="expectedMaxStringLength">The expected maximum length of strings that will
        /// be passed to the Levenshtein methods.</param>
        public Levenshtein(int expectedMaxStringLength) {
            this.baseChar1Costs = new int[expectedMaxStringLength];
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
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

            return Distance(string1, string2, len1, len2, start,
                (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]));
        }

        /// <summary>Compute and return the Levenshtein edit distance between two strings.</summary>
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

            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance,
                    (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]));
            }
            return Distance(string1, string2, len1, len2, start,
                (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]));
        }

        /// <summary>Return Levenshtein similarity between two strings
        /// (1 - (levenshtein distance / len of longer string)).</summary>
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

            return Distance(string1, string2, len1, len2, start,
                    (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]))
                    .ToSimilarity(string2.Length);
        }


        /// <summary>Return Levenshtein similarity between two strings 
        /// (1 - (levenshtein distance / len of longer string)).</summary>
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
            if (iMaxDistance == 0) return (string1 == string2) ? 1 : -1;

            // identify common suffix and/or prefix that can be ignored
            int len1, len2, start;
            Helpers.PrefixSuffixPrep(string1, string2, out len1, out len2, out start);
            if (len1 == 0) return 1.0;

            if (iMaxDistance < len2) {
                return Distance(string1, string2, len1, len2, start, iMaxDistance,
                        (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]))
                        .ToSimilarity(string2.Length);
            }
            return Distance(string1, string2, len1, len2, start,
                    (this.baseChar1Costs = (len2 <= this.baseChar1Costs.Length) ? this.baseChar1Costs : new int[len2]))
                    .ToSimilarity(string2.Length);
        }

        /// <summary>Internal implementation of the core Levenshtein algorithm.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int Distance(string string1, string string2, int len1, int len2, int start, int[] char1Costs) {
            for (int j = 0; j < len2;) char1Costs[j] = ++j;
            int currentCharCost = 0;
            if (start == 0) {
                for (int i = 0; i < len1; ++i) {
                    int leftCharCost, aboveCharCost;
                    leftCharCost = aboveCharCost = i;
                    char char1 = string1[i];
                    for (int j = 0; j < len2; ++j) {
                        currentCharCost = leftCharCost; // cost on diagonal (substitution)
                        leftCharCost = char1Costs[j];
                        if (string2[j] != char1) {
                            // substitution if neither of two conditions below
                            if (aboveCharCost < currentCharCost) currentCharCost = aboveCharCost; // deletion
                            if (leftCharCost < currentCharCost) currentCharCost = leftCharCost; // insertion
                            ++currentCharCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCharCost;
                    }
                }
            } else {
                for (int i = 0; i < len1; ++i) {
                    int leftCharCost, aboveCharCost;
                    leftCharCost = aboveCharCost = i;
                    char char1 = string1[start + i];
                    for (int j = 0; j < len2; ++j) {
                        currentCharCost = leftCharCost; // cost on diagonal (substitution)
                        leftCharCost = char1Costs[j];
                        if (string2[start + j] != char1) {
                            // substitution if neither of two conditions below
                            if (aboveCharCost < currentCharCost) currentCharCost = aboveCharCost; // deletion
                            if (leftCharCost < currentCharCost) currentCharCost = leftCharCost; // insertion
                            ++currentCharCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCharCost;
                    }
                }
            }
            return currentCharCost;
        }


        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //internal static int DistanceExperiment(string string1, string string2, int len1, int len2, int start, int[] char1Costs) {
        //    if (char1Costs[len2-1] > -len2) for (int j = 0; j < len2; ++j) char1Costs[j] = 0;
        //    int currentCharCost = 0;
        //    for (int i = 0; i < len1; ++i) {
        //        char char1 = string1[i + start];
        //        int prevChar1Cost, aboveCharCost;
        //        prevChar1Cost = aboveCharCost = i;
        //        for (int j = 0; j < len2; ++j) {
        //            currentCharCost = prevChar1Cost - 1; // cost on diagonal (substitution)
        //            prevChar1Cost = char1Costs[j];
        //            if (string2[start + j] != char1) {
        //                // substitution if neither of two conditions below
        //                if (aboveCharCost <= currentCharCost) currentCharCost = aboveCharCost - 1; // deletion
        //                if (prevChar1Cost < currentCharCost) currentCharCost = prevChar1Cost; // insertion
        //                ++currentCharCost;
        //            }
        //            char1Costs[j] = aboveCharCost = currentCharCost;
        //        }
        //    }
        //    return currentCharCost + len2;
        //}

        /// <summary>Internal implementation of the core Levenshtein algorithm that accepts a maxDistance.</summary>
        /// <remarks>https://github.com/softwx/SoftWx.Match</remarks>
        internal static int Distance(string string1, string string2, int len1, int len2, int start, int maxDistance, int[] char1Costs) {
//            if (maxDistance >= len2) return Distance(string1, string2, len1, len2, start, char1Costs);
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
            int currentCost = 0;
            if (start == 0) {
                for (i = 0; i < len1; ++i) {
                    char char1 = string1[i];
                    int prevChar1Cost, aboveCharCost;
                    prevChar1Cost = aboveCharCost = i;
                    // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                    // and the upper left diagonal + maxDistance cells (upper left is i)
                    jStart += (i > jStartOffset) ? 1 : 0;
                    jEnd += (jEnd < len2) ? 1 : 0;
                    for (j = jStart; j < jEnd; ++j) {
                        currentCost = prevChar1Cost; // cost on diagonal (substitution)
                        prevChar1Cost = char1Costs[j];
                        if (string2[j] != char1) {
                            // substitution if neither of two conditions below
                            if (aboveCharCost < currentCost) currentCost = aboveCharCost; // deletion
                            if (prevChar1Cost < currentCost) currentCost = prevChar1Cost;   // insertion
                            ++currentCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCost;
                    }
                    if (char1Costs[i + lenDiff] > maxDistance) return -1;
                }
            } else {
                for (i = 0; i < len1; ++i) {
                    char char1 = string1[start + i];
                    int prevChar1Cost, aboveCharCost;
                    prevChar1Cost = aboveCharCost = i;
                    // no need to look beyond window of lower right diagonal - maxDistance cells (lower right diag is i - lenDiff)
                    // and the upper left diagonal + maxDistance cells (upper left is i)
                    jStart += (i > jStartOffset) ? 1 : 0;
                    jEnd += (jEnd < len2) ? 1 : 0;
                    for (j = jStart; j < jEnd; ++j) {
                        currentCost = prevChar1Cost; // cost on diagonal (substitution)
                        prevChar1Cost = char1Costs[j];
                        if (string2[start + j] != char1) {
                            // substitution if neither of two conditions below
                            if (aboveCharCost < currentCost) currentCost = aboveCharCost; // deletion
                            if (prevChar1Cost < currentCost) currentCost = prevChar1Cost;   // insertion
                            ++currentCost;
                        }
                        char1Costs[j] = aboveCharCost = currentCost;
                    }
                    if (char1Costs[i + lenDiff] > maxDistance) return -1;
                }
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
