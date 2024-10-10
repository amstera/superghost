using System;
using UnityEngine;
using UnityEngine.Advertisements;

public abstract class AdBase : MonoBehaviour, IUnityAdsLoadListener, IUnityAdsShowListener
{
    protected string surfacingId;
    protected bool isAdReady = false;
    private int retryCount = 0;
    private const int maxRetryAttempts = 3;

    public event Action OnAdCompleted;
    public event Action OnAdSkipped;
    public event Action OnAdFailed;

    public virtual void LoadAd()
    {
        if (isAdReady)
        {
            return;
        }

        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("No internet connection. Cannot load ad.");
            OnAdFailed?.Invoke();
            RetryLoadAd(5f);
            return;
        }

        if (Advertisement.isInitialized)
        {
            try
            {
                Advertisement.Load(surfacingId, this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading ad: {ex.Message}");
                OnAdFailed?.Invoke();
                RetryLoadAd(5f);
            }
        }
        else
        {
            Debug.LogWarning("Advertisement not initialized. Retrying...");
            RetryLoadAd(5f);
        }
    }

    public void ShowAd()
    {
        if (isAdReady)
        {
            try
            {
                Advertisement.Show(surfacingId, this);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error showing ad: {ex.Message}");
                OnAdFailed?.Invoke();
            }
        }
        else
        {
            Debug.LogWarning("Ad is not ready to be shown.");
            LoadAd();
            OnAdFailed?.Invoke();
        }
    }

    // IUnityAdsLoadListener implementation
    public void OnUnityAdsAdLoaded(string placementId)
    {
        if (placementId == surfacingId)
        {
            isAdReady = true;
            retryCount = 0; // Reset retry count on successful load
            Debug.Log("Ad loaded successfully.");
        }
    }

    public void OnUnityAdsFailedToLoad(string placementId, UnityAdsLoadError error, string message)
    {
        Debug.LogError($"Failed to load Ad Unit '{placementId}': {error.ToString()} - {message}");
        OnAdFailed?.Invoke();
        isAdReady = false;
        RetryLoadAd(5f);
    }

    // IUnityAdsShowListener implementation
    public void OnUnityAdsShowFailure(string placementId, UnityAdsShowError error, string message)
    {
        Debug.LogError($"Error showing Ad Unit '{placementId}': {error.ToString()} - {message}");
        OnAdFailed?.Invoke();
    }

    public void OnUnityAdsShowComplete(string placementId, UnityAdsShowCompletionState showCompletionState)
    {
        isAdReady = false;
        LoadAd(); // Automatically reload ad for future use

        if (showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            Debug.Log("Ad completed successfully.");
            OnAdCompleted?.Invoke(); // User finished watching the ad
        }
        else
        {
            Debug.Log("Ad was skipped before completion.");
            OnAdSkipped?.Invoke(); // User didn't finish watching the ad
        }
    }

    public void OnUnityAdsShowStart(string placementId)
    {
        Debug.Log($"Ad Unit '{placementId}' started showing.");
        // Optionally handle ad start event
    }

    public void OnUnityAdsShowClick(string placementId)
    {
        Debug.Log($"Ad Unit '{placementId}' was clicked.");
        // Optionally handle ad click event
    }

    private void RetryLoadAd(float retryDelay)
    {
        if (retryCount < maxRetryAttempts)
        {
            retryCount++;
            Debug.Log($"Retrying to load ad. Attempt {retryCount}/{maxRetryAttempts}");
            Invoke(nameof(LoadAd), retryDelay);
        }
        else
        {
            Debug.LogWarning("Max retry attempts reached. Ad will not be loaded.");
        }
    }
}