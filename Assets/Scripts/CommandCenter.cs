using UnityEngine;
using UnityEngine.UI.ProceduralImage;

public class CommandCenter : MonoBehaviour
{
    public ProceduralImage shopGlowOutline;
    private SaveObject saveObject;

    void Awake()
    {
        saveObject = SaveManager.Load();
    }

    void OnEnable()
    {
        AdjustAlphaBasedOnLevel();
    }

    public void AdjustAlphaBasedOnLevel()
    {
        float alpha = Mathf.Lerp(0, 130, saveObject.CurrentLevel / 9f) / 255f;

        Color currentColor = shopGlowOutline.color;
        currentColor.a = alpha;
        shopGlowOutline.color = currentColor;
    }
}
