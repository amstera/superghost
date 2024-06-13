using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TextClickHandler : TextMeshProUGUI, IPointerClickHandler
{
    public GameManager gameManager;
    public string word;
    public bool canMoveLeft = true;
    public bool canMoveRight = true;

    private WordPopUp wordPopup;
    private Coroutine colorLerpCoroutine;
    private Coroutine popCoroutine;

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
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(this, eventData.position, eventData.pressEventCamera);
        if (linkIndex != -1)
        {
            // Link was clicked
            TMP_LinkInfo linkInfo = textInfo.linkInfo[linkIndex];

            // Extract the URL/id from the linkInfo
            string url = linkInfo.GetLinkID();

            // Open the URL if it's not empty
            if (!string.IsNullOrEmpty(url))
            {
                if (wordPopup == null)
                {
                    wordPopup = GetComponentInChildren<WordPopUp>();
                }

                RectTransform rectTrans = GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTrans, eventData.position, eventData.pressEventCamera, out Vector2 localPos);
                localPos = new Vector2(0, localPos.y - 80);

                wordPopup.Show(localPos, word, url);
                return;
            }
        }

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

            if (isLeftSide && canMoveLeft)
            {
                gameManager.SelectPosition(GameManager.TextPosition.Left);
            }
            else if (!isLeftSide && canMoveRight)
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

    public void Pop(float duration = 0.2f)
    {
        if (popCoroutine != null)
        {
            StopCoroutine(popCoroutine);
        }

        popCoroutine = StartCoroutine(PopEffect(duration));
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

    IEnumerator PopEffect(float duration)
    {
        Vector3 zeroScale = Vector3.zero;  // Start from absolute zero
        var originalScale = transform.localScale; // Store the original scale
        Vector3 overshootScale = originalScale * 1.1f; // Slightly larger than original for overshoot
        float elapsedTime = 0f;

        // Scale up from zero to overshoot scale
        while (elapsedTime < duration * 0.6f) // Faster to overshoot
        {
            float proportionCompleted = elapsedTime / (duration * 0.6f);
            float easeOutProgress = 1 - Mathf.Pow(1 - proportionCompleted, 2); // Applying ease-out quadratic effect

            transform.localScale = Vector3.Lerp(zeroScale, overshootScale, easeOutProgress);
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        // Settle back to the original scale
        elapsedTime = 0f;
        while (elapsedTime < duration * 0.4f) // Slower to settle
        {
            transform.localScale = Vector3.Lerp(overshootScale, originalScale, elapsedTime / (duration * 0.4f));
            elapsedTime += Time.deltaTime;

            yield return null;
        }

        // Ensure it sets back to original scale
        transform.localScale = originalScale;
    }

}