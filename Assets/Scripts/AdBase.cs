using System;
using Unity.Services.Analytics;
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
        if (isAdReady)
        {
            return;
        }

        if (Advertisement.isInitialized)
        {
            Advertisement.Load(surfacingId, this);
        }
        else
        {
            RetryLoadAd(5f); // Retry after 5 seconds if not initialized
        }
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
        OnAdFailed?.Invoke();
        isAdReady = false;
        RetryLoadAd(5f); // Retry after 5 seconds if loading fails
    }

    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        OnAdFailed?.Invoke();
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        isAdReady = false;
        LoadAd(); // Automatically reload ad for future use

        bool userRewarded = showCompletionState == UnityAdsShowCompletionState.COMPLETED;
        if (userRewarded)
        {
            OnAdCompleted?.Invoke(); // user finished watching ad

            var gamesPlayedEvent = new CustomEvent("watchedAd") { };
            AnalyticsService.Instance.RecordEvent(gamesPlayedEvent);
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

    private void RetryLoadAd(float retryDelay)
    {
        Invoke(nameof(LoadAd), retryDelay);
    }
}