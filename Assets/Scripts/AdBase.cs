using System;
using UnityEngine;
using UnityEngine.Advertisements;

public abstract class AdBase : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    protected string surfacingId;
    protected bool isAdReady = false;

    public event Action OnAdCompleted;
    public event Action OnAdSkipped;
    public event Action OnAdFailed;

    public virtual void LoadAd()
    {
        Advertisement.Load(surfacingId, this);
    }

    public void ShowAd()
    {
        if (isAdReady)
        {
            Advertisement.Show(surfacingId, this);
        }
        else
        {
            OnAdFailed?.Invoke();
        }
    }

    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (placementId == surfacingId)
        {
            isAdReady = true;
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Error loading Ad on {placementId}: {error} - {message}");
        OnAdFailed?.Invoke();
        isAdReady = false;
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Error showing Ad on {placementId}: {error} - {message}");
        OnAdFailed?.Invoke();
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log($"Ad on {placementId} completed with result {showCompletionState}");
        isAdReady = false;
        LoadAd(); // Automatically reload ad for future use

        bool userRewarded = showCompletionState == UnityAdsShowCompletionState.COMPLETED;
        if (userRewarded)
        {
            OnAdCompleted?.Invoke(); // user finished watching ad
        }
        else
        {
            OnAdSkipped?.Invoke(); // user didn't finish watching ad
        }
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        // Optionally handle ad start event
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        // Optionally handle ad click event
    }
}