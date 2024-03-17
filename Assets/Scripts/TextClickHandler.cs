using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TextClickHandler : TextMeshProUGUI, IPointerClickHandler
{
    public GameManager gameManager; // Reference to the GameManager to call ProcessTurn
    public bool canClickLeft = true;
    public bool canClickRight = true;

    private Coroutine colorLerpCoroutine;

    public override string text
    {
        get { return base.text; }
        set
        {
            if (base.text != value)
            {
                if (colorLerpCoroutine != null)
                {
                    StopCoroutine(colorLerpCoroutine);
                    colorLerpCoroutine = null;
                }
            }
            base.text = value;
        }
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        RectTransform rectTransform = GetComponent<RectTransform>();

        // Convert the click position to local position relative to the TextMeshProUGUI component
        Vector2 localCursor;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.position, eventData.pressEventCamera, out localCursor))
        {
            return; // If conversion fails, exit
        }

        // Check if the click is within the TextMeshProUGUI component's boundaries
        if (rectTransform.rect.Contains(localCursor))
        {
            // Determine if the click is on the left or right half of the TextMeshProUGUI component
            bool isLeftSide = localCursor.x < 0; // Assuming pivot is at the center (0.5, 0.5)

            if (isLeftSide && canClickLeft)
            {
                gameManager.SelectPosition(GameManager.TextPosition.Left);
            }
            else if (!isLeftSide && canClickRight)
            {
                gameManager.SelectPosition(GameManager.TextPosition.Right);
            }
        }
    }

    public void HighlightNewLetterAtIndex(int newIndex)
    {
        if (colorLerpCoroutine != null)
        {
            StopCoroutine(colorLerpCoroutine);
        }

        colorLerpCoroutine = StartCoroutine(LerpLetterColorAtIndex(newIndex, 0.5f));
    }

    // Coroutine to lerp the color of the letter at a specific index
    IEnumerator LerpLetterColorAtIndex(int index, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            float lerp = Mathf.Clamp01(time / duration);
            Color currentColor = Color.Lerp(Color.yellow, Color.white, lerp);

            // Use TMP's method to replace the color of a specific letter
            SetLetterColorAtIndex(index, currentColor);

            yield return null;
        }

        // Make sure the letter is white after the transition
        SetLetterColorAtIndex(index, Color.white);
    }

    // Sets the color of a letter at a given index
    private void SetLetterColorAtIndex(int index, Color color)
    {
        // Ensure we don't exceed the string's bounds
        if (index < 0 || index >= text.Length) return;

        // Create a TMP VertexColor array to modify colors
        TMP_TextInfo textInfo = this.textInfo;
        Color32[] newVertexColors;
        int materialIndex = textInfo.characterInfo[index].materialReferenceIndex;

        // Get the vertex colors of the first character (assuming it's using the same material as the rest of the text)
        newVertexColors = textInfo.meshInfo[materialIndex].colors32;

        // Determine the vertex indices of the letter
        int vertexIndex = textInfo.characterInfo[index].vertexIndex;

        // Apply the new color
        newVertexColors[vertexIndex + 0] = color;
        newVertexColors[vertexIndex + 1] = color;
        newVertexColors[vertexIndex + 2] = color;
        newVertexColors[vertexIndex + 3] = color;

        // Update the mesh with the new color
        this.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}