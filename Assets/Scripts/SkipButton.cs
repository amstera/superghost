using UnityEngine;
using UnityEngine.UI;

public class SkipButton : MonoBehaviour
{
    public Button parentTextButton, button;
    public Sprite skipIcon, lockIcon;
    public ScaleInOut scaleInOut;
    public GameObject skipText;
    public bool canSkip;

    public void Set(bool canSkip)
    {
        this.canSkip = canSkip;

        parentTextButton.interactable = canSkip;
        button.interactable = canSkip;
        button.GetComponent<Image>().sprite = canSkip ? skipIcon : lockIcon;
        button.GetComponent<Image>().enabled = canSkip;
        button.transform.localPosition = new Vector2(button.transform.localPosition.x, canSkip ? 6.2f : 3f);
        scaleInOut.enabled = canSkip;
        skipText.SetActive(canSkip);

        RectTransform rect = button.GetComponent<RectTransform>();

        rect.sizeDelta = new Vector2(canSkip ? 24 : 18, canSkip ? 24 : 22);
    }
}