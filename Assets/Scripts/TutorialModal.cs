using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class TutorialModal : MonoBehaviour, IPointerClickHandler
{
    public CanvasGroup canvasGroup;

    private SaveObject saveObject;
    private bool dismissed = false;

    void Awake()
    {
        saveObject = SaveManager.Load();

        if (!CanShowModal())
        {
            gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // Check for both mouse clicks and touch input
        if (!dismissed && IsModalTapped())
        {
            dismissed = true;
            HideModal();
        }
    }

    public void ShowModal()
    {
        if (saveObject == null)
        {
            saveObject = SaveManager.Load();
        }

        if (CanShowModal())
        {
            gameObject.SetActive(true);
        }
    }

    // Method to hide the modal with fade out
    public void HideModal(float fadeOutTime = 0.1f)
    {
        if (gameObject.activeSelf)
        {
            // Mark tutorial as dismissed and save the state
            saveObject.HasDismissedTutorialModal = true;
            SaveManager.Save(saveObject);

            StartCoroutine(FadeOut(fadeOutTime));
        }
    }

    // Detect clicks or touches on the modal
    public void OnPointerClick(PointerEventData eventData)
    {
        dismissed = true;
        HideModal();
    }

    // Coroutine to fade out the CanvasGroup over the given duration
    private IEnumerator FadeOut(float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / duration);
            yield return null;
        }

        // Ensure the alpha is set to 0 at the end and deactivate the game object
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    // Check if the modal itself was tapped or clicked (works for mobile and desktop)
    private bool IsModalTapped()
    {
        if (Input.GetMouseButtonDown(0)) // for mouse clicks
        {
            return IsPointerOverModal();
        }
        else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) // for touch input
        {
            return IsPointerOverModal();
        }
        return false;
    }

    // Helper method to check if the pointer is over the modal's RectTransform
    private bool IsPointerOverModal()
    {
        Vector3 mousePosition = Input.mousePosition;
        RectTransform rectTransform = GetComponent<RectTransform>();

        // Convert screen point to local point in the modal's RectTransform
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, mousePosition, null, out localPoint);

        // Check if the point is within the bounds of the RectTransform
        return rectTransform.rect.Contains(localPoint);
    }

    private bool CanShowModal()
    {
        return !saveObject.HasDismissedTutorialModal &&
            saveObject.Statistics.EasyGameWins == 0 &&
            saveObject.Statistics.NormalGameWins == 0 &&
            saveObject.Statistics.HardGameWins == 0;
    }
}