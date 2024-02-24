using System.Collections.Generic;
using System.Linq;

public class WordDictionary
{
    private HashSet<string> words = new HashSet<string>();
    private System.Random rng = new System.Random();

    private readonly char[] vowels = "aeiou".ToCharArray();
    private readonly char[] consonants = "bcdfghjklmnpqrstvwxyz".ToCharArray();

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

    public bool IsWordReal(string word)
    {
        return words.Contains(word.ToLower());
    }

    public string FindWordContains(string word)
    {
        return words
            .Where(w => w.Contains(word))
            .OrderBy(w => w.Length)
            .FirstOrDefault();
    }

    public bool CanExtendWordToLeft(string word)
    {
        return words.Any(w => w.Contains(word) && w.IndexOf(word) > 0);
    }

    public bool CanExtendWordToRight(string word)
    {
        return words.Any(w => w.Contains(word) && w.IndexOf(word) < w.Length - word.Length);
    }

    public string FindNextWord(string substring)
    {
        if (substring.Length == 0)
        {
            char[] letters = consonants.Concat(vowels).ToArray();
            ShuffleArray(letters);
            return letters[0].ToString();
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

        // Pre-filter the words list to include only those that contain the substring
        var filteredWords = words.Where(w => w.Contains(substring)).ToList();

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
}