using System;
using System.Collections.Generic;
using System.Linq;

public class WordDictionary
{
    private HashSet<string> words = new HashSet<string>();
    private Dictionary<string, int> commonWords = new Dictionary<string, int>();
    private Random rng = new Random();

    private readonly char[] vowels = "aeiou".ToCharArray();
    private readonly char[] consonants = "bcdfghjklmnpqrstvwxyz".ToCharArray();
    private readonly char[] weightedLetters = GenerateWeightedLetters();

    public void LoadWords(string[] lines)
    {
        foreach (var line in lines)
        {
            var word = line.Trim().ToLower();
            if (!string.IsNullOrEmpty(word))
            {
                words.Add(word);
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

    public bool IsWordReal(string word)
    {
        return words.Contains(word.ToLower());
    }

    public string FindWordContains(string word)
    {
        word = word.ToLower();
        return words
            .Where(w => w.Contains(word))
            .OrderBy(w => w.Length)
            .FirstOrDefault();
    }

    public bool CanExtendWordToLeft(string word)
    {
        word = word.ToLower();
        return words.Any(w => w.Contains(word) && w.IndexOf(word) > 0);
    }

    public bool CanExtendWordToRight(string word)
    {
        word = word.ToLower();
        return words.Any(w => w.Contains(word) && w.IndexOf(word) < w.Length - word.Length);
    }

    public string FindNextWord(string substring)
    {
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
        bool endsWithVowel = vowels.Contains(substring[^1]);
        bool startsWithVowel = vowels.Contains(substring[0]);

        // Concatenate vowels and consonants in the order based on the substring's characteristics
        char[] lettersForStartWith = endsWithVowel ? consonants.Concat(vowels).ToArray() : vowels.Concat(consonants).ToArray();
        char[] lettersForEndWith = startsWithVowel ? consonants.Concat(vowels).ToArray() : vowels.Concat(consonants).ToArray();

        if (rng.NextDouble() <= 0.5f)
        {
            ShuffleArray(lettersForStartWith);
            ShuffleArray(lettersForEndWith);
        }

        List<string> evenLengthWords = new List<string>();
        List<string> oddLengthWords = new List<string>();

        // Pre-filter the words list to include only those that contain the substring
        var filteredWords = words.Where(w => w.Contains(substring)).ToList();
        foreach (var word in filteredWords)
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

        // Prioritize words where the length difference is even
        string foundWord = FindWord(substring, lettersForStartWith, lettersForEndWith, evenLengthWords);
        if (!string.IsNullOrEmpty(foundWord))
        {
            return foundWord;
        }

        return FindWord(substring, lettersForStartWith, lettersForEndWith, oddLengthWords);
    }

    public bool ShouldChallenge(string substring)
    {
        if (string.IsNullOrEmpty(substring)) return false;

        substring = substring.ToLower();

        int minSubstringLength = 3;
        if (substring.Length < minSubstringLength) return false;

        // Filter words containing the substring
        var possibleWords = words.Where(word => word.Contains(substring)).ToList();
        if (possibleWords.Count == 0) return true; // No possible words

        // Word Completion Percentage Check
        double maxThresholdPercentage = 0.8;
        foreach (var word in possibleWords)
        {
            double completionPercentage = (double)substring.Length / word.Length;
            if (completionPercentage > maxThresholdPercentage) return false; // Too close to completion
        }

        //Commoness
        int maxCommonessThreshold = 1500;
        int totalScore = 0;
        foreach (var word in possibleWords)
        {
            if (commonWords.TryGetValue(word, out int frequency))
            {
                if (frequency > maxCommonessThreshold)
                {
                    // Found a common word, no need to challenge
                    return false;
                }
                int multiplier = word.StartsWith(substring) ? 2 : 1;
                totalScore += frequency * multiplier;
            }
            else
            {
                // Word not found in commonWords, treating as not very common
                totalScore += 1;
            }
        }

        double avgScore = Math.Min(maxCommonessThreshold, totalScore / (double)possibleWords.Count);

        double challengeProbability = Math.Max(0, 0.85f - avgScore / maxCommonessThreshold);

        return rng.NextDouble() < challengeProbability;
    }

    private string FindWord(string substring, char[] lettersForStartWith, char[] lettersForEndWith, List<string> filteredWords)
    {
        // First, try to find words that start with the substring in the filtered list
        var startWithResult = TryExtensionsWithPriority(substring, lettersForStartWith, true, filteredWords);
        if (!string.IsNullOrEmpty(startWithResult))
        {
            return startWithResult;
        }

        // If none found, fallback to trying words that end with the substring in the filtered list
        return TryExtensionsWithPriority(substring, lettersForEndWith, false, filteredWords);
    }

    private string TryExtensionsWithPriority(string substring, char[] letters, bool prioritizeStart, List<string> filteredWords)
    {
        foreach (var letter in letters)
        {
            string[] extensions = prioritizeStart ? new[] { substring + letter } : new[] { letter + substring };

            foreach (var extension in extensions)
            {
                var matchedWords = filteredWords.Where(w => (substring.Length < 3 || !IsWordReal(extension))
                                                    && (prioritizeStart ? w.StartsWith(extension) : w.Contains(extension)));
                if (matchedWords.Any())
                {
                    return extension;
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