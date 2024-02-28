using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VirtualKeyboard : MonoBehaviour
{
    public GameManager gameManager;
    public GameObject buttonPrefab;
    public Transform keyboardParent;

    private string[] rows = new string[]
    {
        "QWERTYUIOP",
        "ASDFGHJKL",
        "ZXCVBNM"
    };

    private List<Button> allButtons = new List<Button>(); // Store all buttons for easy access

    void Start()
    {
        GenerateKeyboard();
    }

    void GenerateKeyboard()
    {
        float padding = 5f; 
        float parentWidth = keyboardParent.GetComponent<RectTransform>().rect.width - (padding * 2);
        float spacing = 7.5f;

        // Find the longest row to base centering calculations on
        int maxRowLength = 0;
        foreach (string row in rows)
        {
            if (row.Length > maxRowLength)
            {
                maxRowLength = row.Length;
            }
        }

        // Calculate the button width based on the longest row and the available parent width
        float buttonWidth = (parentWidth - (maxRowLength - 1) * spacing) / maxRowLength;
        float buttonHeight = 60f; // Adjust the button height as needed

        for (int i = 0; i < rows.Length; i++)
        {
            // Calculate the starting X position for the current row to center it
            float rowLength = rows[i].Length;
            float rowWidth = rowLength * buttonWidth + (rowLength - 1) * spacing;
            float startPositionX = (parentWidth - rowWidth) / 2f + padding / 2f; // Add half of the padding to the start position

            for (int j = 0; j < rowLength; j++)
            {
                GameObject buttonObj = Instantiate(buttonPrefab, keyboardParent);
                RectTransform btnRect = buttonObj.GetComponent<RectTransform>();

                // Set the button's size
                btnRect.sizeDelta = new Vector2(buttonWidth, buttonHeight);

                // Set the button's position
                float xPos = startPositionX + j * (buttonWidth + spacing);
                btnRect.anchoredPosition = new Vector2(xPos, -i * (buttonHeight + spacing));

                // Set the letter on the button and add it to the button list
                char letter = rows[i][j];
                buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = letter.ToString();
                Button btn = buttonObj.GetComponent<Button>();
                btn.onClick.AddListener(() => ButtonClicked(letter, btn));
                allButtons.Add(btn);
            }
        }
    }

    public void ButtonClicked(char letter, Button btn)
    {
        StartCoroutine(PopAnimation(btn.gameObject));
        gameManager.ProcessTurn(letter);
    }

    public void DisableAllButtons()
    {
        foreach (Button btn in allButtons)
        {
            btn.interactable = false;
            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            btnText.color = new Color(btnText.color.r, btnText.color.g, btnText.color.b, 0.25f);
        }
    }

    public void EnableAllButtons()
    {
        foreach (Button btn in allButtons)
        {
            btn.interactable = true;
            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            btnText.color = new Color(btnText.color.r, btnText.color.g, btnText.color.b, 1f);
        }
    }

    IEnumerator PopAnimation(GameObject btnGameObject)
    {
        Vector3 originalScale = btnGameObject.transform.localScale;
        Vector3 targetScale = originalScale * 1.1f;

        // Scale up
        float currentTime = 0f;
        float duration = 0.1f;
        while (currentTime < duration)
        {
            btnGameObject.transform.localScale = Vector3.Lerp(originalScale, targetScale, currentTime / duration);
            currentTime += Time.deltaTime;
            yield return null;
        }

        // Scale down
        currentTime = 0f;
        while (currentTime < duration)
        {
            btnGameObject.transform.localScale = Vector3.Lerp(targetScale, originalScale, currentTime / duration);
            currentTime += Time.deltaTime;
            yield return null;
        }
    }

}
