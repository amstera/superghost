using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TextClickHandler : TextMeshProUGUI, IPointerClickHandler
{
    public GameManager gameManager; // Reference to the GameManager to call ProcessTurn
    public bool canClickLeft = true;
    public bool canClickRight = true;

    public void OnPointerClick(PointerEventData eventData)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        // Convert the click position to local position relative to the TextMeshProUGUI component
        Vector2 localCursor;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
        {
            return; // If conversion fails, exit
        }

        // Determine if the click is on the left or right half of the TextMeshProUGUI component
        bool isLeftSide = localCursor.x < 0; // Assuming pivot is at the center (0.5, 0.5)

        if (isLeftSide)
        {
            if (canClickLeft)
            {
                gameManager.SelectPosition(GameManager.TextPosition.Left);
            }
        }
        else
        {
            if (canClickRight)
            {
                gameManager.SelectPosition(GameManager.TextPosition.Right);
            }
        }
    }
}