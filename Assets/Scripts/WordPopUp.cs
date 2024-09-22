using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class WordPopUp : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public BlockPopUp blockPopup;
    public ReportPopUp reportPopUp;
    public Button defineButton, blockButton, reportButton;
    public float fadeInDuration = 0.25f;
    public float fadeOutDuration = 0.25f;
    public AudioSource clickAudioSource;
    public RectTransform popUpRectTransform;

    private string word, url;

    private void Awake()
    {
        Hide();
    }

    private void Update()
    {
        if (canvasGroup.alpha > 0 && IsPointerPressedOutsidePopUp())
        {
            Hide();
        }
    }

    public void Show(Vector3 position, string word, string url, bool isValidWord)
    {
        clickAudioSource?.Play();

        transform.localPosition = position;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        this.word = word;
        this.url = url;

        bool showReport = url.Contains("greenteagaming.com", System.StringComparison.InvariantCultureIgnoreCase);
        if (showReport)
        {
            defineButton.gameObject.SetActive(false);
            blockButton.interactable = false;
            reportButton.gameObject.SetActive(true);
        }
        else
        {
            defineButton.gameObject.SetActive(true);
            blockButton.interactable = isValidWord;
            reportButton.gameObject.SetActive(false);
        }

        StopAllCoroutines();
        StartCoroutine(FadeCanvasGroup(canvasGroup, canvasGroup.alpha, 1f, fadeInDuration));
    }

    public void OpenDefinition()
    {
        if (!string.IsNullOrEmpty(url))
        {
            clickAudioSource?.Play();
            Hide();

            Application.OpenURL(url);
        }
    }

    public void OpenBlockWord()
    {
        if (!string.IsNullOrEmpty(word))
        {
            clickAudioSource?.Play();
            Hide();

            blockPopup.Show(word);
        }
    }

    public void OpenReport()
    {
        if (!string.IsNullOrEmpty(url))
        {
            clickAudioSource?.Play();
            Hide();

            reportPopUp.Show(word, url);
        }
    }

    public void Hide()
    {
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private bool IsPointerPressedOutsidePopUp()
    {
        Vector2 inputPosition = GetInputPosition();
        if (inputPosition != Vector2.zero)
        {
            return !RectTransformUtility.RectangleContainsScreenPoint(popUpRectTransform, inputPosition, Camera.main);
        }
        return false;
    }

    private Vector2 GetInputPosition()
    {
        if (Input.GetMouseButtonDown(0))
        {
            return Input.mousePosition;
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            return Input.GetTouch(0).position;
        }
        return Vector2.zero;
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