using UnityEngine;
using UnityEngine.Audio;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public AudioMixer audioMixer;
    public AudioSource bgMusicAudioSource;
    public AudioClip bgMusic, gameEndMusic;
    public static AudioManager instance;

    private SaveObject saveObject;
    private bool isGameStarted = true;
    private bool didWin = false;

    void Awake()
    {
        // Ensure only one instance of AudioManager exists
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        saveObject = SaveManager.Load();
        if (!saveObject.EnableSound)
        {
            MuteMaster();
        }

        if (!saveObject.EnableMusic)
        {
            bgMusicAudioSource.Stop();
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

    public void GameStarted()
    {
        isGameStarted = true;
        if (bgMusicAudioSource.clip != bgMusic && saveObject.EnableMusic)
        {
            StartCoroutine(FadeOutAndIn(bgMusic));
        }
    }

    public void GameEnded(bool didWin)
    {
        isGameStarted = false;
        this.didWin = didWin;
        if (bgMusicAudioSource.clip != gameEndMusic && saveObject.EnableMusic)
        {
            StartCoroutine(FadeOutAndIn(gameEndMusic, targetPitch: didWin ? 1 : 0.8f));
        }
    }

    public void StartMusic()
    {
        if (bgMusicAudioSource.isPlaying)
        {
            return;
        }

        if (isGameStarted)
        {
            StartCoroutine(FadeOutAndIn(bgMusic));
        }
        else
        {
            StartCoroutine(FadeOutAndIn(gameEndMusic, targetPitch: didWin ? 1 : 0.8f));
        }
    }

    public void StopMusic()
    {
        bgMusicAudioSource.Stop();
    }

    private IEnumerator FadeOutAndIn(AudioClip newClip, float fadeDuration = 0.5f, float targetPitch = 1)
    {
        // Check if a clip is currently playing
        if (bgMusicAudioSource.isPlaying)
        {
            // Fade out (pitch shift down)
            for (float t = 0; t < fadeDuration; t += Time.deltaTime)
            {
                float progress = t / fadeDuration;
                bgMusicAudioSource.pitch = 1 - (progress * progress); // Quadratic easing out
                yield return null;
            }
            bgMusicAudioSource.pitch = 0;
        }

        // Switch clip and play
        bgMusicAudioSource.Stop();
        bgMusicAudioSource.clip = newClip;
        bgMusicAudioSource.Play();

        // Fade in (pitch shift up)
        for (float t = 0; t < fadeDuration; t += Time.deltaTime)
        {
            float progress = t / fadeDuration;
            bgMusicAudioSource.pitch = progress * progress; // Quadratic easing in
            yield return null;
        }
        bgMusicAudioSource.pitch = targetPitch;
    }
}