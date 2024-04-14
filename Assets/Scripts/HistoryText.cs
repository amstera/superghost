using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HistoryText : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public TextMeshProUGUI textComponent;
    public Button firstPageButton;
    public Button secondPageButton;
    public AudioSource clickAudioSource;

    private Vector2 startDragPosition;
    private Vector2 endDragPosition;

    void Start()
    {
        // Add listeners to buttons
        firstPageButton.onClick.AddListener(() => ShowPage(1, true));
        secondPageButton.onClick.AddListener(() => ShowPage(2, true));

        // Initial setup
        UpdateText(textComponent.text);
    }

    public void UpdateText(string newText)
    {
        textComponent.text = newText;
        Canvas.ForceUpdateCanvases(); // Ensure layout recalculates to get accurate page count
        int totalPages = textComponent.textInfo.pageCount;

        // Hide or show buttons based on page count
        bool showButtons = totalPages > 1;
        firstPageButton.gameObject.SetActive(showButtons);
        secondPageButton.gameObject.SetActive(showButtons);

        ShowPage(1, false);
    }

    private void ShowPage(int pageNumber, bool playSound)
    {
        if (playSound)
        {
            clickAudioSource?.Play();
        }

        textComponent.pageToDisplay = pageNumber;

        // Disable the button for the current page, enable the other
        firstPageButton.interactable = pageNumber != 1;
        secondPageButton.interactable = pageNumber != 2;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        startDragPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // While dragging, we don't need to do anything here, but Unity requires this method for the interface.
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        endDragPosition = eventData.position;
        HandleSwipe();
    }

    private void HandleSwipe()
    {
        float horizontalMovement = endDragPosition.x - startDragPosition.x;

        if (Mathf.Abs(horizontalMovement) > 100)
        {
            if (horizontalMovement > 0 && firstPageButton.gameObject.activeSelf && firstPageButton.interactable)
            {
                ShowPage(1, true);
            }
            else if (horizontalMovement < 0 && secondPageButton.gameObject.activeSelf && secondPageButton.interactable)
            {
                ShowPage(2, true);
            }
        }
    }
}