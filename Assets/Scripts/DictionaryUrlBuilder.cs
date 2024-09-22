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

    public static async Task<string> BuildDictionaryUrlAsync(string word)
    {
        string wordLower = word.ToLower();
        using (HttpClient client = new HttpClient())
        {
            foreach (string url in dictionaryUrls)
            {
                string finalUrl = string.Format(url, wordLower);
                try
                {
                    // Attempt to get the response
                    HttpResponseMessage response = await client.GetAsync(finalUrl);

                    // Check if the response is successful
                    if (response.IsSuccessStatusCode)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();

                        // If the word is found, return the valid URL
                        if (IsWordFound(responseText, finalUrl))
                        {
                            return finalUrl;
                        }
                    }
                    else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // If it's a 404, continue to the next URL
                        continue;
                    }
                }
                catch (HttpRequestException e)
                {
                    // Log the request exception (network failure or other errors)
                    Debug.Log($"Request error: {e.Message}");

                    // Handle no internet connection or similar socket issues
                    if (e.InnerException is System.Net.Sockets.SocketException)
                    {
                        Debug.Log("No internet connection, using fallback URL.");
                        return string.Format(fallbackUrl, wordLower);
                    }
                }
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