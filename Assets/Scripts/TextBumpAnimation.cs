using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class TextBumpAnimation : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float bumpHeight = 2.5f;
    public float animationDuration = 0.25f;

    private string previousText = "";
    private List<int> animatedIndices = new List<int>();

    void Update()
    {
        if (textMesh.text != previousText)
        {
            StopAllCoroutines(); // Stop any ongoing animations
            if (string.IsNullOrEmpty(textMesh.text))
            {
                textMesh.text = "";
                previousText = "";
                return;
            }

            DetermineAnimatedIndices();
            StartCoroutine(AnimateTextChange());
            previousText = textMesh.text;
        }
    }


    private void DetermineAnimatedIndices()
    {
        animatedIndices.Clear();
        if (textMesh.text.Length <= 2) return;  // No animation if the text is too short.

        string newTextPart = GetNewTextPart(previousText, textMesh.text);
        int startIndex = textMesh.text.IndexOf(newTextPart);
        int endIndex = startIndex + newTextPart.Length;

        // Adjust startIndex and endIndex to skip the first and last characters of the string
        startIndex = Mathf.Max(startIndex, 1);  // Start from the second character if possible
        endIndex = Mathf.Min(endIndex, textMesh.text.Length - 1);  // End before the last character

        for (int i = startIndex; i < endIndex; i++)
        {
            animatedIndices.Add(i);
        }
    }

    IEnumerator AnimateTextChange()
    {
        TMP_TextInfo textInfo = textMesh.textInfo;
        Vector3[][] originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            originalVertices[m] = textInfo.meshInfo[m].vertices.Clone() as Vector3[];
        }

        float elapsedTime = 0;
        while (elapsedTime < animationDuration)
        {
            float bumpAmount = Mathf.Sin(Mathf.PI * elapsedTime / animationDuration) * bumpHeight;
            foreach (var index in animatedIndices)
            {
                if (index < textInfo.characterCount && textInfo.characterInfo[index].isVisible)
                {
                    int vertexIndex = textInfo.characterInfo[index].vertexIndex;
                    Vector3[] vertices = originalVertices[textInfo.characterInfo[index].materialReferenceIndex];
                    for (int j = 0; j < 4; j++)
                    {
                        textInfo.meshInfo[textInfo.characterInfo[index].materialReferenceIndex].vertices[vertexIndex + j] = vertices[vertexIndex + j] + new Vector3(0, bumpAmount, 0);
                    }
                }
            }
            textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Reset vertices to original positions after animation
        for (int m = 0; m < textInfo.meshInfo.Length; m++)
        {
            textInfo.meshInfo[m].vertices = originalVertices[m];
        }
        textMesh.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private string GetNewTextPart(string oldText, string newText)
    {
        int length = Mathf.Min(oldText.Length, newText.Length);
        for (int i = 0; i < length; i++)
        {
            if (oldText[i] != newText[i])
            {
                return newText.Substring(i);
            }
        }
        return newText.Substring(length);
    }
}