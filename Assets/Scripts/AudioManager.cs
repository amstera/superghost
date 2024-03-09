using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public AudioMixer audioMixer;

    void Start()
    {
        var saveObject = SaveManager.Load();
        if (!saveObject.EnableSound)
        {
            MuteMaster();
        }
    }

    public void MuteMaster()
    {
        audioMixer.SetFloat("Volume", -80f); // Mutes the Master group
    }

    public void UnmuteMaster()
    {
        audioMixer.SetFloat("Volume", 0f); // Unmutes the Master group
    }
}
