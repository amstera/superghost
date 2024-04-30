using UnityEngine;
using UnityEngine.UI;

public class SkipButton : MonoBehaviour
{
    public Button button;
    public Sprite skipIcon, lockIcon;

    public void Set(bool canSkip)
    {
        button.interactable = canSkip;
        button.GetComponent<Image>().sprite = canSkip ? skipIcon : lockIcon;

        RectTransform rect = button.GetComponent<RectTransform>();

        rect.sizeDelta = new Vector2(canSkip ? 24 : 20, canSkip ? 24 : 22);
    }
}
