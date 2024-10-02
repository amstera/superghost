using System;
using System.Collections.Generic;
using System.Text;

public class BKTree
{
    private BKTreeNode root;

    public BKTree(IEnumerable<string> words)
    {
        BuildTree(words);
    }

    private void BuildTree(IEnumerable<string> words)
    {
        using var enumerator = words.GetEnumerator();
        if (!enumerator.MoveNext())
            throw new Exception("Word list is empty.");

        // Normalize and create the root node
        root = new BKTreeNode(enumerator.Current.ToLowerInvariant());

        while (enumerator.MoveNext())
        {
            // Normalize words at insertion
            root.Add(enumerator.Current.ToLowerInvariant());
        }
    }

    public string FindBestMatch(string word, double minSimilarity = 0.7)
    {
        if (root == null)
            return null;

        string bestMatch = null;
        int bestDistance = -1; // Changed from int.MaxValue
        double bestSimilarity = 0.0;
        string wordSoundex = ComputeSoundex(word);
        HashSet<string> wordBigrams = GenerateBigrams(word);
        HashSet<char> wordCharSet = new HashSet<char>(word);

        root.Search(
            word.ToLowerInvariant(),
            minSimilarity,
            ref bestMatch,
            ref bestDistance,
            ref bestSimilarity,
            wordSoundex,
            wordBigrams,
            wordCharSet);

        return bestSimilarity >= minSimilarity ? bestMatch : null;
    }

    private static string ComputeSoundex(string word)
    {
        if (string.IsNullOrEmpty(word))
            return null;

        word = word.ToLowerInvariant();

        // Transformations to simplify the word
        var transformations = new Dictionary<string, string>
        {
            {"ph", "f"}, {"ck", "k"}, {"gh", "f"}, {"ps", "s"},
            {"qu", "kw"}, {"wr", "r"}, {"mb", "m"}, {"or", "er"}
        };

        // Apply transformations using StringBuilder for efficiency
        StringBuilder sbWord = new StringBuilder(word);
        foreach (var pair in transformations)
        {
            sbWord.Replace(pair.Key, pair.Value);
        }
        word = sbWord.ToString();

        // Handle silent final 'e'
        if (word.EndsWith("e"))
        {
            word = word.Substring(0, word.Length - 1);
        }

        // Handle hard and soft 'c'
        word = ReplaceHardC(word);

        // Retain the first letter
        char firstLetter = char.ToUpperInvariant(word[0]);
        StringBuilder soundexCode = new StringBuilder();
        soundexCode.Append(firstLetter);

        // Soundex mapping
        var soundexMap = new Dictionary<char, char>
        {
            {'B', '1'}, {'F', '1'}, {'P', '1'}, {'V', '1'},
            {'C', '2'}, {'G', '2'}, {'J', '2'}, {'K', '2'},
            {'Q', '2'}, {'S', '2'}, {'X', '2'}, {'Z', '2'},
            {'D', '3'}, {'T', '3'},
            {'L', '4'},
            {'M', '5'}, {'N', '5'},
            {'R', '6'}
        };

        // Generate Soundex code, skipping the first letter
        char previousCode = '0';
        for (int i = 1; i < word.Length; i++)
        {
            char currentChar = char.ToUpperInvariant(word[i]);
            if (soundexMap.TryGetValue(currentChar, out char code))
            {
                if (code != previousCode)
                {
                    soundexCode.Append(code);
                    previousCode = code;
                }
            }
            else
            {
                previousCode = '0'; // Reset if character is not mapped
            }
        }

        // Ensure the result is exactly 4 characters
        while (soundexCode.Length < 4)
        {
            soundexCode.Append('0');
        }

        return soundexCode.ToString().Substring(0, 4);
    }

    private static string ReplaceHardC(string word)
    {
        // Use StringBuilder to avoid multiple string allocations
        StringBuilder result = new StringBuilder(word.Length);

        for (int i = 0; i < word.Length; i++)
        {
            if (word[i] == 'c' && (i + 1 < word.Length) && !"eiyao".Contains(word[i + 1]))
            {
                result.Append('k'); // Replace hard 'c' with 'k'
            }
            else
            {
                result.Append(word[i]);
            }
        }

        return result.ToString();
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
        public string SoundexCode { get; private set; }
        public HashSet<string> Bigrams { get; private set; }
        public HashSet<char> CharacterSet { get; private set; }
        private SortedList<int, BKTreeNode> Children { get; set; }

        public BKTreeNode(string word)
        {
            Word = word;
            MaxWordLength = word.Length;
            SoundexCode = ComputeSoundex(word);
            Bigrams = GenerateBigrams(word);
            CharacterSet = new HashSet<char>(word);
            Children = new SortedList<int, BKTreeNode>();
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
                Children.Add(distance, new BKTreeNode(word));
            }
        }

        public void Search(
            string word,
            double minSimilarity,
            ref string bestMatch,
            ref int bestDistance,
            ref double bestSimilarity,
            string wordSoundex,
            HashSet<string> wordBigrams,
            HashSet<char> wordCharSet)
        {
            int distance = ComputeLevenshteinDistance(Word, word);

            int maxLength = Math.Max(word.Length, Word.Length);
            double similarity = 1.0 - ((double)distance / maxLength);

            double adjustedSimilarity = similarity;

            // Use precomputed Soundex codes
            if (wordSoundex == SoundexCode)
            {
                adjustedSimilarity += 0.2; // Boost for phonetic similarity
            }

            // Use precomputed bigrams
            int matches = 0;
            foreach (var bigram in Bigrams)
            {
                if (wordBigrams.Contains(bigram))
                    matches++;
            }
            double nGramSimilarity = (double)matches / Math.Max(Bigrams.Count, wordBigrams.Count);
            adjustedSimilarity += nGramSimilarity * 0.3; // Boost for structural similarity

            // Penalize if too few shared letters
            if (HasFewSharedLetters(wordCharSet, CharacterSet))
            {
                adjustedSimilarity -= 0.2; // Penalize for few shared letters
            }

            // Update best match if this node is better
            if ((adjustedSimilarity > bestSimilarity) ||
                (Math.Abs(adjustedSimilarity - bestSimilarity) < 0.0001 && (distance < bestDistance || bestDistance == -1)))
            {
                bestSimilarity = adjustedSimilarity;
                bestDistance = distance;
                bestMatch = Word;
            }

            // Prune the search space
            foreach (var kvp in Children)
            {
                int key = kvp.Key;
                BKTreeNode childNode = kvp.Value;

                // Compute minimal possible distance
                int minPossibleDistance = Math.Abs(distance - key);
                int childMaxLength = Math.Max(word.Length, childNode.MaxWordLength);
                double maxPossibleSimilarity = 1.0 - ((double)minPossibleDistance / childMaxLength);

                if (maxPossibleSimilarity >= minSimilarity && maxPossibleSimilarity > bestSimilarity)
                {
                    childNode.Search(
                        word,
                        minSimilarity,
                        ref bestMatch,
                        ref bestDistance,
                        ref bestSimilarity,
                        wordSoundex,
                        wordBigrams,
                        wordCharSet);
                }
            }
        }

        private bool HasFewSharedLetters(HashSet<char> inputSet, HashSet<char> candidateSet)
        {
            int sharedLetters = 0;
            foreach (var c in inputSet)
            {
                if (candidateSet.Contains(c))
                    sharedLetters++;
                if (sharedLetters >= 3)
                    return false; // Enough shared letters
            }
            return true; // Fewer than 3 shared letters
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
                            currentRow[j - 1] + 1,     // Insertion
                            previousRow[j] + 1),       // Deletion
                        previousRow[j - 1] + cost);    // Substitution
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