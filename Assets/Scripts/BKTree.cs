using System;
using System.Collections.Generic;
using System.Text;

public class BKTree
{
    private BKTreeNode root;
    private static Dictionary<string, int> wordFrequency; // Frequency dictionary for prioritizing common words

    public BKTree(IEnumerable<string> words, Dictionary<string, int> frequencies = null)
    {
        wordFrequency = frequencies ?? new Dictionary<string, int>();
        BuildTree(words);
    }

    private void BuildTree(IEnumerable<string> words)
    {
        using var enumerator = words.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new Exception("Word list is empty.");

        root = new BKTreeNode(enumerator.Current);

        while (enumerator.MoveNext())
        {
            root.Add(enumerator.Current);
        }
    }

    public string FindBestMatch(string word, double minSimilarity = 0.7)
    {
        if (root == null)
            return null;

        string bestMatch = null;
        int bestDistance = int.MaxValue;
        double bestSimilarity = 0.0;

        root.Search(word.ToLower(), minSimilarity, ref bestMatch, ref bestDistance, ref bestSimilarity);

        return bestSimilarity >= minSimilarity ? bestMatch : null;
    }

    private static string ComputeSoundex(string word)
    {
        if (string.IsNullOrEmpty(word))
            return null;

        // Handle "ph" as "f" and other special mappings before processing
        word = word.Replace("ph", "f").Replace("ck", "k").Replace("sh", "s").Replace("gh", "g").Replace("c", "k");

        // Retain the first letter of the word
        char firstLetter = char.ToUpper(word[0]);

        // Build the Soundex code by converting each character based on its sound
        StringBuilder soundexCode = new StringBuilder();
        soundexCode.Append(firstLetter);

        for (int i = 1; i < word.Length; i++)
        {
            char c = char.ToUpper(word[i]);

            if ("BFPV".Contains(c))
                soundexCode.Append('1');
            else if ("CGJKQSXZ".Contains(c))
                soundexCode.Append('2');
            else if ("DT".Contains(c))
                soundexCode.Append('3');
            else if ("L".Contains(c))
                soundexCode.Append('4');
            else if ("MN".Contains(c))
                soundexCode.Append('5');
            else if ("R".Contains(c))
                soundexCode.Append('6');
            // Ignore vowels and non-consonant characters
        }

        // Remove consecutive duplicates
        string rawSoundex = soundexCode.ToString();
        StringBuilder result = new StringBuilder();
        result.Append(rawSoundex[0]);

        for (int i = 1; i < rawSoundex.Length; i++)
        {
            if (rawSoundex[i] != rawSoundex[i - 1])
            {
                result.Append(rawSoundex[i]);
            }
        }

        // Pad with zeros or trim to ensure a four-character code
        while (result.Length < 4)
        {
            result.Append('0');
        }

        return result.ToString().Substring(0, 4);
    }

    // N-Gram comparison method (bi-gram) to improve structural similarity comparison
    private static double ComputeNGramSimilarity(string s1, string s2)
    {
        var bigrams1 = GenerateBigrams(s1);
        var bigrams2 = GenerateBigrams(s2);

        int matches = 0;
        foreach (var bigram in bigrams1)
        {
            if (bigrams2.Contains(bigram))
                matches++;
        }

        return (double)matches / Math.Max(bigrams1.Count, bigrams2.Count);
    }

    private static HashSet<string> GenerateBigrams(string word)
    {
        HashSet<string> bigrams = new HashSet<string>();
        for (int i = 0; i < word.Length - 1; i++)
        {
            bigrams.Add(word.Substring(i, 2));
        }
        return bigrams;
    }

    private class BKTreeNode
    {
        public string Word { get; private set; }
        public int MaxWordLength { get; private set; }
        private Dictionary<int, BKTreeNode> Children { get; set; }

        public BKTreeNode(string word)
        {
            Word = word;
            MaxWordLength = word.Length;
            Children = new Dictionary<int, BKTreeNode>();
        }

        public void Add(string word)
        {
            int distance = ComputeLevenshteinDistance(Word, word);
            MaxWordLength = Math.Max(MaxWordLength, word.Length);

            if (distance == 0)
                return; // Skip duplicate words

            if (Children.TryGetValue(distance, out BKTreeNode child))
            {
                child.Add(word);
                child.MaxWordLength = Math.Max(child.MaxWordLength, word.Length);
            }
            else
            {
                Children[distance] = new BKTreeNode(word);
            }
        }

        public void Search(string word, double minSimilarity, ref string bestMatch, ref int bestDistance, ref double bestSimilarity)
        {
            int distance = ComputeLevenshteinDistance(Word, word);
            int maxLength = Math.Max(word.Length, Word.Length);
            double similarity = 1.0 - ((double)distance / maxLength);

            // Adjust similarity based on letter structure
            double adjustedSimilarity = similarity;

            string wordSoundex = ComputeSoundex(word);
            string candidateSoundex = ComputeSoundex(Word);
            if (wordSoundex == candidateSoundex)
            {
                adjustedSimilarity += 0.2; // Boost similarity for phonetically similar words
            }

            // Boost similarity based on N-Gram comparison
            double nGramSimilarity = ComputeNGramSimilarity(word, Word);
            adjustedSimilarity += nGramSimilarity * 0.3; // Boost for bigram structural similarity

            // Penalize words that have fewer shared letters across the entire word
            if (HasFewSharedLetters(word, Word))
            {
                adjustedSimilarity -= 0.3; // Penalize for having too few shared letters
            }

            // Choose the best match based on adjusted similarity
            if ((adjustedSimilarity > bestSimilarity) || (adjustedSimilarity == bestSimilarity && distance < bestDistance))
            {
                bestSimilarity = adjustedSimilarity;
                bestDistance = distance;
                bestMatch = Word;
            }

            foreach (var kvp in Children)
            {
                int key = kvp.Key;
                BKTreeNode childNode = kvp.Value;

                // Compute minimum possible distance between target word and child's word
                int minPossibleDistance = Math.Abs(distance - key);
                int childMaxLength = Math.Max(word.Length, childNode.MaxWordLength);
                double maxPossibleSimilarity = 1.0 - ((double)minPossibleDistance / childMaxLength);

                if (maxPossibleSimilarity >= minSimilarity)
                {
                    childNode.Search(word, minSimilarity, ref bestMatch, ref bestDistance, ref bestSimilarity);
                }
            }
        }

        private bool HasFewSharedLetters(string input, string candidate)
        {
            var inputSet = new HashSet<char>(input);
            var candidateSet = new HashSet<char>(candidate);

            inputSet.IntersectWith(candidateSet);
            return inputSet.Count < 3; // Penalize if fewer than 3 letters are shared
        }

        private int ComputeLevenshteinDistance(string s, string t)
        {
            int n = s.Length;
            int m = t.Length;

            if (n == 0) return m;
            if (m == 0) return n;

            int[] previousRow = new int[m + 1];
            int[] currentRow = new int[m + 1];

            for (int j = 0; j <= m; j++)
                previousRow[j] = j;

            for (int i = 1; i <= n; i++)
            {
                currentRow[0] = i;
                char sChar = s[i - 1];

                for (int j = 1; j <= m; j++)
                {
                    int cost = (sChar == t[j - 1]) ? 0 : 1;
                    currentRow[j] = Math.Min(
                        Math.Min(
                            currentRow[j - 1] + 1,       // Insertion
                            previousRow[j] + 1),         // Deletion
                        previousRow[j - 1] + cost);     // Substitution
                }

                // Swap rows for next iteration
                var tempRow = previousRow;
                previousRow = currentRow;
                currentRow = tempRow;
            }

            return previousRow[m];
        }
    }
}