using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EffectsPopUp : MonoBehaviour
{
    public ActiveEffectsText activeEffectsText;
    public TextMeshProUGUI titleText, descriptionText;
    public CanvasGroup canvasGroup;
    public Image backgroundImage;
    public Button sellButton;
    public float fadeInDuration = 0.25f;
    public float fadeOutDuration = 0.25f;
    public AudioSource clickAudioSource;

    private void Awake()
    {
        canvasGroup.alpha = 0;
    }

    private void Update()
    {
        if (canvasGroup.alpha > 0 && Input.GetButtonDown("Fire1"))
        {
            Hide();
        }
    }

    public void Show(string title, string description, string extraInfoText, Vector3 position, Color color)
    {
        clickAudioSource?.Play();
        canvasGroup.interactable = true;

        titleText.text = title;
        descriptionText.text = description;
        if (!string.IsNullOrEmpty(extraInfoText))
        {
            descriptionText.text += $" ({extraInfoText})";
        }
        transform.localPosition = position;
        backgroundImage.color = color;

        StopAllCoroutines();
        StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 1f, fadeInDuration));
    }

    public void Hide()
    {
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float counter = 0f;
        while (counter < duration)
        {
            counter += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, counter / duration);
            yield return null;
        }
    }
}
