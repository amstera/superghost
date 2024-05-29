using UnityEngine;
using UnityEngine.UI;

public class SkipButton : MonoBehaviour
{
    public Button parentTextButton, button;
    public Sprite skipIcon, lockIcon;
    public ScaleInOut scaleInOut;
    public GameObject skipText;

    public void Set(bool canSkip)
    {
        parentTextButton.interactable = canSkip;
        button.interactable = canSkip;
        button.GetComponent<Image>().sprite = canSkip ? skipIcon : lockIcon;
        button.transform.localPosition = new Vector2(button.transform.localPosition.x, canSkip ? 5.5f : 3f);
        scaleInOut.enabled = canSkip;
        skipText.SetActive(canSkip);

        RectTransform rect = button.GetComponent<RectTransform>();

        rect.sizeDelta = new Vector2(canSkip ? 24 : 18, canSkip ? 24 : 22);
    }
}