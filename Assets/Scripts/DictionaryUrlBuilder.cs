using System;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public static class DictionaryUrlBuilder
{
    private static readonly string[] dictionaryUrls =
    {
        "https://www.dictionary.com/browse/{0}",
        "https://www.merriam-webster.com/dictionary/{0}",
        "https://en.wiktionary.org/wiki/{0}"
    };

    public static readonly string fallbackUrl = "https://scrabble.merriam.com/finder/{0}";

    // Use a single static HttpClient instance for all requests
    private static readonly HttpClient client;

    static DictionaryUrlBuilder()
    {
        client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10) // Set an appropriate timeout
        };
    }

    public static async Task<string> BuildDictionaryUrlAsync(string word)
    {
        string wordLower = word.ToLower();

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("No internet connection. Using fallback URL.");
            return string.Format(fallbackUrl, wordLower);
        }

        foreach (string url in dictionaryUrls)
        {
            string finalUrl = string.Format(url, wordLower);
            try
            {
                HttpResponseMessage response = await client.GetAsync(finalUrl);

                if (response.IsSuccessStatusCode)
                {
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (IsWordFound(responseText, finalUrl))
                    {
                        return finalUrl;
                    }
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    continue;
                }
            }
            catch (HttpRequestException e) when (e.InnerException is System.Net.Sockets.SocketException)
            {
                Debug.LogError($"Request error (no internet): {e.Message}");
                return string.Format(fallbackUrl, wordLower);
            }
            catch (TaskCanceledException e)
            {
                Debug.LogError($"Request timed out: {e.Message}");
                // Optionally handle timeout by retrying or using the next URL
                continue;
            }
            catch (Exception e)
            {
                Debug.LogError($"Unexpected error: {e.Message}");
                // Optionally handle other exceptions or continue to the next URL
                continue;
            }
        }

        // If no word is found in any dictionary, use the fallback URL
        return string.Format(fallbackUrl, wordLower);
    }

    private static bool IsWordFound(string htmlText, string url)
    {
        if (url.Contains("dictionary.com"))
        {
            return !htmlText.Contains("No results found for");
        }
        else if (url.Contains("merriam-webster.com"))
        {
            return !htmlText.Contains("The word you've entered isn't in the dictionary");
        }
        else if (url.Contains("wiktionary.org"))
        {
            return !htmlText.Contains("does not yet have an entry for");
        }

        return true;
    }
}