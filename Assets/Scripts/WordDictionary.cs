using System;
using System.Collections.Generic;
using System.Linq;

public class WordDictionary
{
    private List<string> words = new List<string>();
    private List<string> originalWords = new List<string>();
    private List<string> filteredWords = new List<string>();
    private HashSet<string> lostChallengeSubstring = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private HashSet<char> restrictedLetters = new HashSet<char>();
    private Dictionary<string, int> commonWords = new Dictionary<string, int>();
    private Random rng = new Random();

    private readonly char[] vowels = "aeiouy".ToCharArray();
    private readonly char[] consonants = "bcdfghjklmnpqrstvwxz".ToCharArray();
    private readonly char[] specialConsonants = "bcdgkpstw".ToCharArray();
    private readonly char[] weightedLetters = GenerateWeightedLetters();
    private int minLength = 3;
    private int wordDirection;
    private NumberCriteria numberCriteria = null;
    private bool noRepeatingLetters;
    private delegate bool MatchCondition(string w, string extension);

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

    public void LoadOriginalWords(string[] lines)
    {
        foreach (var line in lines)
        {
            var word = line.Trim().ToLower();
            if (!string.IsNullOrEmpty(word) && word.Length > 3)
            {
                originalWords.Add(word);
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
        // Convert to HashSets for faster membership checking
        var originalWordsSet = new HashSet<string>(originalWords);
        var wordsSet = new HashSet<string>(words);

        // Create a list of tuples with precomputed values for sorting
        var wordWithKeys = wordsSet.Select(w =>
        {
            int commonality = commonWords.TryGetValue(w, out var frequency) ? frequency : 0;
            bool isInOriginal = originalWordsSet.Contains(w);
            return (Word: w, IsInOriginal: isInOriginal, Commonality: commonality);
        }).ToList();

        // Sort by commonality first, then by whether it is in originalWords
        wordWithKeys.Sort((wk1, wk2) =>
        {
            // First, sort by commonality (higher frequency comes first)
            int commonalityComparison = wk2.Commonality.CompareTo(wk1.Commonality);
            if (commonalityComparison != 0)
                return commonalityComparison;

            // If commonality is the same, prioritize originalWords
            return wk2.IsInOriginal.CompareTo(wk1.IsInOriginal);
        });

        // Convert back to list after sorting
        words = wordWithKeys.Select(wk => wk.Word).ToList();
    }

    public void SetFilteredWords(string substring)
    {
        substring = substring.ToLower();

        var words = filteredWords.Where(w =>
            w.Length > minLength &&
            w.Contains(substring, StringComparison.InvariantCultureIgnoreCase) &&
            (numberCriteria == null || numberCriteria.IsAllowed(w.Length)) &&
            (!noRepeatingLetters || HasNoRepeatingLetters(w)) &&
            IsValidExtension(w, substring)
        );

        filteredWords = words.ToList();
    }

    private bool HasNoRepeatingLetters(string word)
    {
        var seenLetters = new HashSet<char>();

        foreach (var letter in word)
        {
            if (!seenLetters.Add(letter))
            {
                return false; // Found a repeating letter
            }
        }

        return true;
    }

    public void ClearFilteredWords(List<string> blockedWords)
    {
        // Convert blocked words list to a HashSet for faster lookup
        var blockedWordsSet = new HashSet<string>(blockedWords, StringComparer.InvariantCultureIgnoreCase);

        // Remove blocked words first with case insensitivity
        var unblockedWords = words.Where(w => !blockedWordsSet.Contains(w, StringComparer.InvariantCultureIgnoreCase)).ToList();

        // Then filter by restricted letters
        filteredWords = unblockedWords.Where(w =>
            !restrictedLetters.Any(l => w.Contains(l, StringComparison.InvariantCultureIgnoreCase))
        ).ToList();
    }

    public bool IsWordReal(string word, bool useEntireDictionary = false)
    {
        if (useEntireDictionary)
        {
            return words.Contains(word.ToLower());
        }

        return filteredWords.Contains(word.ToLower());
    }

    public string FindWordContains(string substring, bool isBluff)
    {
        if (isBluff)
        {
            if (filteredWords.Any(w => w.Contains(substring, StringComparison.InvariantCultureIgnoreCase) && commonWords.ContainsKey(w)))
            {
                return filteredWords.FirstOrDefault(w => w.Contains(substring, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        return filteredWords.Where(w => w.Contains(substring, StringComparison.InvariantCultureIgnoreCase)).OrderBy(w => w.Length).FirstOrDefault();
    }

    public void AddLostChallengeWord(string substring)
    {
        substring = substring.ToLower();
        lostChallengeSubstring.Add(substring);
    }

    public void AddRestrictedLetter(char c)
    {
        restrictedLetters.Add(c);
    }

    public void ClearRestrictions()
    {
        restrictedLetters.Clear();
        minLength = 3;
        numberCriteria = null;
    }

    public void SetNumberCriteria(NumberCriteria numberCriteria)
    {
        this.numberCriteria = numberCriteria;
    }

    public void SetNoRepeatingLetters(bool value)
    {
        noRepeatingLetters = value;
    }

    public void SetMinLength(int minLength)
    {
        this.minLength = minLength;
    }

    public void SetWordDirection(int direction)
    {
        wordDirection = direction;
    }

    public string BluffWord(string substring, Difficulty difficulty)
    {
        substring = substring.ToLower();

        var difficultySettings = DifficultySettings.GetSettingsForDifficulty(difficulty);

        float oddsToBluff = difficultySettings.ProbabilityOffset / 4.5f;
        if (rng.NextDouble() < oddsToBluff)
        {
            char firstChar = substring[0];
            char lastChar = substring[^1];
            bool firstCharIsVowel = vowels.Contains(firstChar);
            bool lastCharIsVowel = vowels.Contains(lastChar);

            bool addAtEnd = true;
            if (difficulty != Difficulty.Easy)
            {
                if (firstCharIsVowel && !lastCharIsVowel)
                {
                    addAtEnd = false;
                }
            }

            if (filteredWords.Count > 0)
            {
                if (filteredWords.All(w => w.EndsWith(substring)))
                {
                    addAtEnd = false;
                }
                else if (filteredWords.All(w => w.StartsWith(substring)))
                {
                    addAtEnd = true;
                }
            }

            if (wordDirection == 1)
            {
                addAtEnd = true;
            }

            char nextLetter = ChooseNextLetter(firstCharIsVowel, lastCharIsVowel, addAtEnd && wordDirection != -1, substring);

            var bluffedWord = addAtEnd && wordDirection != -1 ? substring + nextLetter : nextLetter + substring;
            if (filteredWords.Contains(bluffedWord.ToLower())) // it is bluffing accidentally with a real word
            {
                return BluffWord(substring, difficulty); // redo it and try again
            }

            return bluffedWord;
        }

        // return nothing if choosing not to bluff the word
        return null;
    }

    private char ChooseNextLetter(bool firstCharIsVowel, bool lastCharIsVowel, bool addAtEnd, string substring)
    {
        // Determine the type of letter to add based on the position and whether the adjoining character is a vowel
        bool shouldAddVowel = (firstCharIsVowel && !addAtEnd) || (lastCharIsVowel && addAtEnd) ? false : true;

        // Filter the weightedLetters based on whether we should add a vowel or consonant
        var possibleLetters = weightedLetters.Where(letter => !restrictedLetters.Contains(char.ToUpper(letter)) && ((shouldAddVowel && vowels.Contains(letter)) || (!shouldAddVowel && consonants.Contains(letter)))).ToArray();
        if (noRepeatingLetters)
        {
            possibleLetters = possibleLetters.Where(l => !substring.ToLower().Contains(l)).ToArray();
        }

        return possibleLetters[rng.Next(possibleLetters.Length)];
    }

    public bool ShouldChallenge(string substring, int playerAIWinDifference, Difficulty difficulty, bool aiCallsChallenge)
    {
        if (string.IsNullOrEmpty(substring)) return false;

        substring = substring.ToLower();

        if (filteredWords.Count == 0) return true; // No possible words

        if (substring.Length < minLength) return false;

        if (lostChallengeSubstring.Contains(substring)) return false;

        // Word Completion Percentage Check
        foreach (var word in filteredWords)
        {
            double completionPercentage = (double)substring.Length / word.Length;
            var maxThresholdPercentage = word.StartsWith(substring) ? 0.75f : 0.85f;
            if (completionPercentage >= maxThresholdPercentage) return false; // Too close to completion
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
        var difficultySettings = DifficultySettings.GetSettingsForDifficulty(difficulty);

        double challengeProbability = Math.Max(0, difficultySettings.ProbabilityOffset - avgScore / maxCommonessThreshold);
        if (playerAIWinDifference < 0 && !aiCallsChallenge) // AI is winning and deciding to accept bluff
        {
            challengeProbability *= 1f + (0.25f * Math.Abs(playerAIWinDifference));
            challengeProbability = Math.Min(0.9f, challengeProbability);
        }

        return rng.NextDouble() < challengeProbability;
    }

    public string FindNextWord(string substring, int playerAIWinDifference, Difficulty difficulty)
    {
        substring = substring.ToLower();
        if (substring.Length == 0)
        {
            var lettersToChoose = weightedLetters.Where(w => !restrictedLetters.Contains(char.ToUpper(w))).ToArray();
            int index = rng.Next(lettersToChoose.Length);
            return lettersToChoose[index].ToString();
        }

        if (filteredWords.Count == 0) // no words match
        {
            return null;
        }

        bool isAILosing = playerAIWinDifference > 0;

        float ratio = 0.65f - playerAIWinDifference * 0.1f;
        if (!isAILosing && difficulty == Difficulty.Easy && rng.NextDouble() <= ratio) // if it's easy and you can spell a word, just spell it
        {
            if (filteredWords.Any(f => f.Contains(substring) && f.Length - substring.Length == 1))
            {
                return null;
            }
        }

        if (difficulty == Difficulty.Easy)
        {
            isAILosing = false;
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

        ratio = 0.3f + playerAIWinDifference * 0.075f;
        if (difficulty == Difficulty.Hard || rng.NextDouble() <= ratio)
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

        bool anyEasyWordsRemain = false;
        int minScore = 0;
        var difficultySettings = DifficultySettings.GetSettingsForDifficulty(difficulty);
        foreach (var scoreThreshold in difficultySettings.ScoreThresholds)
        {
            minScore = scoreThreshold;
            anyEasyWordsRemain = filteredWords.Any(w => commonWords.TryGetValue(w, out int frequency) && frequency >= minScore);
            if (anyEasyWordsRemain)
            {
                break;
            }
        }

        List<string> evenLengthWords = new List<string>();
        List<string> oddLengthWords = new List<string>();
        foreach (var word in filteredWords)
        {
            if (!anyEasyWordsRemain || (difficulty > Difficulty.Easy && isAILosing) || (commonWords.TryGetValue(word, out int frequency) && frequency >= minScore))
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

        // Determine the priority order based on isAILosing flag
        ratio = 0.5f + playerAIWinDifference * 0.1f;
        bool primaryIsEven = isAILosing || (difficulty == Difficulty.Hard && rng.NextDouble() <= ratio);
        var primaryList = primaryIsEven ? evenLengthWords : oddLengthWords;
        var secondaryList = primaryIsEven ? oddLengthWords : evenLengthWords;

        if (!primaryIsEven && substring.Length >= 2)
        {
            primaryList = primaryList.Where(p => !evenLengthWords.Any(e => p.Contains(e))).ToList();
        }

        // Attempt to find a word in the primary list, then in the secondary if necessary
        string foundWord = FindWord(substring, lettersForStartWith, lettersForEndWith, primaryList, playerAIWinDifference, difficulty);
        if (foundWord == null)
        {
            Random random = new Random();
            ratio = 0.4f - playerAIWinDifference * 0.025f;
            if (!isAILosing && difficulty == Difficulty.Normal && random.NextDouble() <= ratio)
            {
                if (filteredWords.Any(f => f.Contains(substring) && f.Length - substring.Length == 1))
                {
                    return null;
                }
            }

            return FindWord(substring, lettersForStartWith, lettersForEndWith, secondaryList, playerAIWinDifference, difficulty);
        }

        return foundWord;
    }

    private string FindWord(string substring, char[] lettersForStartWith, char[] lettersForEndWith, List<string> wordList, int playerAIWinDifference, Difficulty difficulty)
    {
        bool isAILosing = playerAIWinDifference > 0;
        bool prioritizeStart = ShouldPrioritizeStart(substring.Length, playerAIWinDifference, difficulty);
        var lettersPrimaryList = prioritizeStart ? lettersForStartWith : lettersForEndWith;
        var lettersSecondaryList  = prioritizeStart ? lettersForEndWith : lettersForStartWith;

        // First, try to find words with the possible priorizitation
        var startWithResult = TryExtensionsWithPriority(substring, lettersPrimaryList, prioritizeStart, wordList, difficulty);
        if (string.IsNullOrEmpty(startWithResult))
        {
            Random random = new Random();
            float ratio = 0.25f - playerAIWinDifference * 0.025f;
            if (!isAILosing && difficulty == Difficulty.Normal && random.NextDouble() <= ratio)
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

    private bool ShouldPrioritizeStart(int substringLength, int playerAIWinDifference, Difficulty difficulty)
    {
        if (wordDirection == -1)
        {
            return false;
        }

        if (wordDirection == 1)
        {
            return true;
        }

        Random random = new Random();

        if (difficulty == Difficulty.Hard)
        {
            return random.NextDouble() <= 0.5f;
        }

        if (substringLength <= 2) return true;

        // Otherwise chance it will
        float odds = 0.9f - playerAIWinDifference * 0.1f;
        return random.NextDouble() <= odds;
    }

    private string TryExtensionsWithPriority(string substring, char[] letters, bool prioritizeStart, List<string> wordList, Difficulty difficulty)
    {
        string bestExtension = null;
        foreach (var letter in letters)
        {
            string[] extensions = prioritizeStart ? new[] { substring + letter } : new[] { letter + substring };
            MatchCondition initialCondition = (w, extension) => substring.Length < minLength || !IsWordReal(extension);

            foreach (var extension in extensions)
            {
                if (wordList.Any(w => initialCondition(w, extension)
                    && (prioritizeStart ?
                        w.StartsWith(extension, StringComparison.InvariantCultureIgnoreCase)
                        : w.EndsWith(extension, StringComparison.InvariantCultureIgnoreCase))))
                {
                    bool isInOriginalWords = originalWords.Any(word => word.Contains(extension, StringComparison.InvariantCultureIgnoreCase));
                    if (isInOriginalWords)
                    {
                        return extension; // Immediately return if the extension is in originalWords
                    }
                    else if (bestExtension == null)
                    {
                        bestExtension = extension; // Store as a fallback if no better option is found
                    }
                }
            }

            if (!prioritizeStart || difficulty == Difficulty.Hard)
            {
                foreach (var extension in extensions)
                {
                    if (wordList.Any(w => initialCondition(w, extension)
                        && w.Contains(extension, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        bool isInOriginalWords = originalWords.Any(word => word.Contains(extension, StringComparison.InvariantCultureIgnoreCase));
                        if (isInOriginalWords)
                        {
                            return extension; // Immediately return if the extension is in originalWords
                        }
                        else if (bestExtension == null)
                        {
                            bestExtension = extension; // Store as a fallback if no better option is found
                        }
                    }
                }
            }
        }

        return bestExtension; // Return the best option found, or null if none found
    }

    private bool IsValidExtension(string word, string substring)
    {
        if (wordDirection == 0)
        {
            return true;
        }
        else if (wordDirection == -1)
        {
            return word.EndsWith(substring, StringComparison.InvariantCultureIgnoreCase);
        }
        else if (wordDirection == 1)
        {
            return word.StartsWith(substring, StringComparison.InvariantCultureIgnoreCase);
        }
        return false;
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
        var weightedList = new List<char>();
        var letterFrequencies = new Dictionary<char, int>
        {
            {'e', 12}, {'t', 9}, {'a', 8}, {'o', 7}, {'i', 7}, {'n', 6}, {'s', 6}, {'h', 6},
            {'r', 5}, {'d', 4}, {'l', 4}, {'c', 3}, {'u', 3}, {'m', 2}, {'w', 2}, {'f', 2},
            {'g', 2}, {'y', 2}, {'p', 1}, {'b', 1}, {'v', 1}, {'k', 1}, {'j', 1}, {'x', 1},
            {'q', 1}, {'z', 1}
        };

        foreach (var kvp in letterFrequencies)
        {
            weightedList.AddRange(Enumerable.Repeat(kvp.Key, kvp.Value));
        }

        return weightedList.ToArray();
    }
}

public class DifficultySettings
{
    public float ProbabilityOffset { get; set; }
    public int[] ScoreThresholds { get; set; }

    public static DifficultySettings GetSettingsForDifficulty(Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => new DifficultySettings { ProbabilityOffset = 1f, ScoreThresholds = new[] { 1500, 1250, 1000, 750, 500, 400, 250, 100 } },
            Difficulty.Normal => new DifficultySettings { ProbabilityOffset = 0.85f, ScoreThresholds = new[] { 1250, 1000, 750, 500, 400, 250, 100 } },
            Difficulty.Hard => new DifficultySettings { ProbabilityOffset = 0.65f, ScoreThresholds = new[] { 1000, 750, 500, 400, 250, 100 } },
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), "Unsupported difficulty level.")
        };
    }
}