using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryText : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public Button firstPageButton;
    public Button secondPageButton;

    public AudioSource clickAudioSource;

    void Start()
    {
        // Add listeners to buttons
        firstPageButton.onClick.AddListener(() => ShowPage(1));
        secondPageButton.onClick.AddListener(() => ShowPage(2));

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

        if (showButtons)
        {
            ShowPage(1); // Go to the first page automatically when text changes
        }
    }

    private void ShowPage(int pageNumber)
    {
        clickAudioSource?.Play();

        textComponent.pageToDisplay = pageNumber;

        // Disable the button for the current page, enable the other
        firstPageButton.interactable = pageNumber != 1;
        secondPageButton.interactable = pageNumber != 2;
    }
}