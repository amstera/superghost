using System;
using System.Collections.Generic;
using System.Linq;

public class WordDictionary
{
    private HashSet<string> words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> filteredWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> lostChallengeSubstring = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, int> commonWords = new Dictionary<string, int>();
    private Random rng = new Random();

    private readonly char[] vowels = "aeiouy".ToCharArray();
    private readonly char[] consonants = "bcdfghjklmnpqrstvwxz".ToCharArray();
    private readonly char[] specialConsonants = "bcdgkpstw".ToCharArray();
    private readonly char[] weightedLetters = GenerateWeightedLetters();

    public void LoadWords(string[] lines)
    {
        foreach (var line in lines)
        {
            var word = line.Trim().ToLower();
            if (!string.IsNullOrEmpty(word) && word.Length > 3)
            {
                words.Add(word);
                filteredWords.Add(word);
            }
        }
    }

    public void LoadCommonWords(string[] lines)
    {
        foreach (var line in lines)
        {
            var parts = line.Trim().ToLower().Split(' ');
            if (parts.Length == 2 && int.TryParse(parts[1], out int frequency))
            {
                commonWords[parts[0]] = frequency;
            }
        }
    }

    public void SortWordsByCommonality()
    {
        // Convert HashSet to a List for sorting
        var sortedList = words.ToList();

        // Sort the list by commonality
        sortedList.Sort((w1, w2) =>
        {
            int freq1 = commonWords.TryGetValue(w1, out var frequency1) ? frequency1 : int.MaxValue;
            int freq2 = commonWords.TryGetValue(w2, out var frequency2) ? frequency2 : int.MaxValue;
            return freq1.CompareTo(freq2);
        });

        // Reassign the sorted list back to words
        words = sortedList.ToHashSet();
    }

    public void SetFilteredWords(string substring)
    {
        filteredWords = filteredWords.Where(w => w.Contains(substring, StringComparison.InvariantCultureIgnoreCase)).ToHashSet();
    }

    public void ClearFilteredWords()
    {
        filteredWords = new HashSet<string>(words);
    }

    public bool IsWordReal(string word)
    {
        return filteredWords.Contains(word.ToLower());
    }

    public string FindWordContains(string word)
    {
        return filteredWords
            .Where(w => w.Contains(word, StringComparison.InvariantCultureIgnoreCase))
            .OrderBy(w => w.Length)
            .FirstOrDefault();
    }

    public bool CanExtendWordToLeft(string word)
    {
        return filteredWords.Any(w => w.Contains(word, StringComparison.InvariantCultureIgnoreCase) && w.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) > 0);
    }

    public bool CanExtendWordToRight(string word)
    {
        return filteredWords.Any(w => w.Contains(word, StringComparison.InvariantCultureIgnoreCase) && w.IndexOf(word, StringComparison.InvariantCultureIgnoreCase) < w.Length - word.Length);
    }

    public void AddLostChallengeWord(string word)
    {
        lostChallengeSubstring.Add(word);
    }

    public bool ShouldChallenge(string substring, Difficulty difficulty)
    {
        if (string.IsNullOrEmpty(substring)) return false;

        substring = substring.ToLower();

        int minSubstringLength = 3;
        if (substring.Length < minSubstringLength) return false;

        if (lostChallengeSubstring.Contains(substring)) return false;

        if (filteredWords.Count == 0) return true; // No possible words

        // Word Completion Percentage Check
        foreach (var word in filteredWords)
        {
            double completionPercentage = (double)substring.Length / word.Length;
            var maxThresholdPercentage = word.StartsWith(substring) ? 0.6f : 0.8f;
            if (completionPercentage > maxThresholdPercentage) return false; // Too close to completion
        }

        //Commoness
        int maxCommonessThreshold = 1500;
        int totalScore = 0;
        foreach (var word in filteredWords)
        {
            if (commonWords.TryGetValue(word, out int frequency))
            {
                if (frequency > maxCommonessThreshold)
                {
                    // Found a common word, no need to challenge
                    return false;
                }
                float multiplier = word.StartsWith(substring) ? 1.5f : 1;
                totalScore += (int)(frequency * multiplier);
            }
            else
            {
                // Word not found in commonWords, treating as not very common
                totalScore += 5;
            }
        }

        double avgScore = Math.Min(maxCommonessThreshold, totalScore / (double)filteredWords.Count);

        float probabilityOffSet = 0.8f;
        if (difficulty == Difficulty.Easy)
        {
            probabilityOffSet = 0.95f;
        }
        else if (difficulty == Difficulty.Hard)
        {
            probabilityOffSet = 0.6f;
        }

        double challengeProbability = Math.Max(0, probabilityOffSet - avgScore / maxCommonessThreshold);

        return rng.NextDouble() < challengeProbability;
    }

    public string FindNextWord(string substring, bool isLosing, Difficulty difficulty)
    {
        if (difficulty == Difficulty.Easy)
        {
            isLosing = false;
        }

        substring = substring.ToLower();
        if (substring.Length == 0)
        {
            int index = rng.Next(weightedLetters.Length);
            return weightedLetters[index].ToString();
        }

        // Shuffle vowels and consonants separately
        ShuffleArray(vowels);
        ShuffleArray(consonants);

        // Determine whether to prioritize vowels or consonants based on the substring's start and end
        var startChar = substring[0];
        var endChar = substring[^1];
        bool endsWithVowel = vowels.Contains(endChar);
        bool startsWithVowel = vowels.Contains(startChar);

        // Concatenate vowels and consonants in the order based on the substring's characteristics
        char[] lettersForStartWith = endsWithVowel ? consonants.Concat(vowels).ToArray() : vowels.Concat(consonants).ToArray();
        char[] lettersForEndWith = startsWithVowel ? consonants.Concat(vowels).ToArray() : vowels.Concat(consonants).ToArray();

        if (difficulty > Difficulty.Easy && (difficulty == Difficulty.Hard || rng.NextDouble() <= 0.4f))
        {
            ShuffleArray(lettersForStartWith);
            ShuffleArray(lettersForEndWith);
        }
        else
        {
            if (!startsWithVowel && specialConsonants.Contains(startChar))
            {
                ShuffleArray(lettersForEndWith);
            }
            if (!endsWithVowel && specialConsonants.Contains(endChar))
            {
                ShuffleArray(lettersForStartWith);
            }
        }

        int minScore = 400;
        if (difficulty == Difficulty.Hard)
        {
            minScore = 50;
        }
        else if (difficulty == Difficulty.Easy)
        {
            minScore = 750;
        }

        bool anyEasyWordsRemain = filteredWords.Any(w => commonWords.TryGetValue(w, out int frequency) && frequency >= minScore);

        List<string> evenLengthWords = new List<string>();
        List<string> oddLengthWords = new List<string>();
        foreach (var word in filteredWords)
        {
            if (!anyEasyWordsRemain || (difficulty > Difficulty.Easy && isLosing) || (commonWords.TryGetValue(word, out int frequency) && frequency >= minScore))
            {
                if (Math.Abs(word.Length - substring.Length) % 2 == 0)
                {
                    evenLengthWords.Add(word);
                }
                else
                {
                    oddLengthWords.Add(word);
                }
            }
        }

        // Determine the priority order based on isLosing flag
        var primaryList = isLosing || difficulty == Difficulty.Hard ? evenLengthWords : oddLengthWords;
        var secondaryList = isLosing || difficulty == Difficulty.Hard ? oddLengthWords : evenLengthWords;

        // Attempt to find a word in the primary list, then in the secondary if necessary
        string foundWord = FindWord(substring, lettersForStartWith, lettersForEndWith, primaryList, isLosing, difficulty);
        if (foundWord == null)
        {
            if (difficulty == Difficulty.Easy)
            {
                if (filteredWords.Any(f => f.Contains(substring) && f.Length - substring.Length == 1))
                {
                    return null;
                }
            }

            return FindWord(substring, lettersForStartWith, lettersForEndWith, secondaryList, isLosing, difficulty);
        }

        return foundWord;
    }

    private string FindWord(string substring, char[] lettersForStartWith, char[] lettersForEndWith, List<string> wordList, bool isLosing, Difficulty difficulty)
    {
        bool prioritizeStart = ShouldPrioritizeStart(substring.Length, isLosing, difficulty);
        var lettersPrimaryList = prioritizeStart ? lettersForStartWith : lettersForEndWith;
        var lettersSecondaryList  = prioritizeStart ? lettersForEndWith : lettersForStartWith;

        // First, try to find words with the possible priorizitation
        var startWithResult = TryExtensionsWithPriority(substring, lettersPrimaryList, prioritizeStart, wordList, difficulty);
        if (string.IsNullOrEmpty(startWithResult))
        {
            if (difficulty == Difficulty.Easy)
            {
                if (filteredWords.Any(f => f.Contains(substring) && f.Length - substring.Length == 1))
                {
                    return null;
                }
            }
        }
        else
        {
            return startWithResult;
        }

        // If none found, fallback to the opposite prioritization
        return TryExtensionsWithPriority(substring, lettersSecondaryList, !prioritizeStart, wordList, difficulty);
    }

    private bool ShouldPrioritizeStart(int substringLength, bool isLosing, Difficulty difficulty)
    {
        if (difficulty == Difficulty.Easy)
        {
            return true;
        }

        Random random = new Random();

        if (difficulty == Difficulty.Hard)
        {
            return random.NextDouble() <= 0.15f;
        }

        if (substringLength <= 2) return true;

        // Otherwise chance it will
        float odds = isLosing ? 0.6f : 0.85f;
        return random.NextDouble() <= odds;
    }

    private string TryExtensionsWithPriority(string substring, char[] letters, bool prioritizeStart, List<string> wordList, Difficulty difficulty)
    {
        foreach (var letter in letters)
        {
            string[] extensions = prioritizeStart ? new[] { substring + letter } : new[] { letter + substring };

            foreach (var extension in extensions)
            {
                var matchedWords = wordList.Any(w => (substring.Length < 3 || !IsWordReal(extension))
                                                    && (prioritizeStart ? w.StartsWith(extension, StringComparison.InvariantCultureIgnoreCase) : w.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase)));
                if (matchedWords)
                {
                    return extension;
                }
            }

            if (!prioritizeStart || difficulty == Difficulty.Hard)
            {
                // loop through it again but try with contains
                foreach (var extension in extensions)
                {
                    var matchedWords = wordList.Any(w => (substring.Length < 3 || !IsWordReal(extension))
                                                        && w.Contains(extension));
                    if (matchedWords)
                    {
                        return extension;
                    }
                }
            }
        }

        return null; // No valid extension found
    }


    private void ShuffleArray<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    private static char[] GenerateWeightedLetters()
    {
        // Initialize a list to hold the weighted letters
        List<char> weightedList = new List<char>();

        // Add letters based on their frequency
        weightedList.AddRange(Enumerable.Repeat('e', 12));
        weightedList.AddRange(Enumerable.Repeat('t', 9));
        weightedList.AddRange(Enumerable.Repeat('a', 8));
        weightedList.AddRange(Enumerable.Repeat('o', 7));
        weightedList.AddRange(Enumerable.Repeat('i', 7));
        weightedList.AddRange(Enumerable.Repeat('n', 6));
        weightedList.AddRange(Enumerable.Repeat('s', 6));
        weightedList.AddRange(Enumerable.Repeat('h', 6));
        weightedList.AddRange(Enumerable.Repeat('r', 5));
        weightedList.AddRange(Enumerable.Repeat('d', 4));
        weightedList.AddRange(Enumerable.Repeat('l', 4));
        weightedList.AddRange(Enumerable.Repeat('c', 3));
        weightedList.AddRange(Enumerable.Repeat('u', 3));
        weightedList.AddRange(Enumerable.Repeat('m', 2));
        weightedList.AddRange(Enumerable.Repeat('w', 2));
        weightedList.AddRange(Enumerable.Repeat('f', 2));
        weightedList.AddRange(Enumerable.Repeat('g', 2));
        weightedList.AddRange(Enumerable.Repeat('y', 2));
        weightedList.AddRange(Enumerable.Repeat('p', 1));
        weightedList.AddRange(Enumerable.Repeat('b', 1));
        weightedList.AddRange(Enumerable.Repeat('v', 1));
        weightedList.AddRange(Enumerable.Repeat('k', 1));
        weightedList.AddRange(Enumerable.Repeat('j', 1));
        weightedList.AddRange(Enumerable.Repeat('x', 1));
        weightedList.AddRange(Enumerable.Repeat('q', 1));
        weightedList.AddRange(Enumerable.Repeat('z', 1));

        // Convert the list to an array and return it
        return weightedList.ToArray();
    }
}