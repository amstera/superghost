using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class Share : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void _Native_Share_iOS(string message);

    public void ShareMessage(List<RecapObject> recap)
    {
        var sharedMessage = GetSharedMessage(recap);

        if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
            _Native_Share_iOS(sharedMessage);
        }
        else
        {
            GUIUtility.systemCopyBuffer = sharedMessage;
            Debug.LogWarning("Native sharing is only available on iOS. Current platform: " + Application.platform); ;
        }
    }

    private string GetSharedMessage(List<RecapObject> recap)
    {
        string pointsText = recap.Last().PlayerLivesRemaining == 0 ? "" : recap.Last().Points == 1 ? "1 pt - " : $"{recap.Last().Points} pts - ";
        string message = $"Wordy Ghost - {pointsText}{recap.Count} rounds";
        foreach (var item in recap)
        {
            message += "\n";
            if (item.PlayerLivesRemaining < 5)
            {
                for (int i = 0; i < 5 - item.PlayerLivesRemaining; i++)
                {
                    message += "🟥";
                }
            }
            if (item.PlayerLivesRemaining > 0)
            {
                for (int i = 0; i < item.PlayerLivesRemaining; i++)
                {
                    message += "🟩";
                }
            }

            message += $" {item.GameWord.ToUpper()}";
            if (!item.IsValidWord)
            {
                message += " ❌";
            }
            message += $" ({item.Points})";
        }

        return message;
    }
}
