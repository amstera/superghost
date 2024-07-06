public class RewardedAd : AdBase
{
    void Start() // Removed the 'protected override'
    {
        // Set default surfacing ID for iOS
        surfacingId = "Rewarded_iOS";

        #if UNITY_ANDROID
            surfacingId = "Rewarded_Android";
        #endif

        base.LoadAd(); // Call LoadAd from the base class
    }
}