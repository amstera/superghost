using UnityEngine;
using UnityEngine.UI;

public class SkipButton : MonoBehaviour
{
    public Button parentTextButton, button;
    public Sprite skipIcon, lockIcon;
    public ScaleInOut scaleInOut;

    public void Set(bool canSkip)
    {
        parentTextButton.interactable = canSkip;
        button.interactable = canSkip;
        button.GetComponent<Image>().sprite = canSkip ? skipIcon : lockIcon;
        button.transform.localPosition = new Vector2(button.transform.localPosition.x, canSkip ? 2.1f : 2.5f);
        scaleInOut.enabled = canSkip;

        RectTransform rect = button.GetComponent<RectTransform>();

        rect.sizeDelta = new Vector2(canSkip ? 24 : 18, canSkip ? 24 : 22);
    }
}