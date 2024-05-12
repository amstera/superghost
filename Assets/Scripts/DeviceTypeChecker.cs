using UnityEngine;

public static class DeviceTypeChecker
{
    public static bool IsTablet()
    {
        float aspectRatio = (float)Screen.height / Screen.width;
        return aspectRatio < 1.5f; // Common tablet aspect ratios are less than 1.5
    }

    public static bool IsiPhoneSE()
    {
        Vector2 resolution = new Vector2(Screen.width, Screen.height);

        Vector2 se1stGen = new Vector2(640, 1136);
        Vector2 se2ndGen = new Vector2(750, 1334);

        return resolution.Equals(se1stGen) || resolution.Equals(se2ndGen);
    }
}