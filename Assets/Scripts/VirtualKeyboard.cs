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
    public TextMeshProUGUI warningText;

    public AudioClip keyAudioClip;

    private string[] rows = new string[]
    {
        "QWERTYUIOP",
        "ASDFGHJKL",
        "ZXCVBNM"
    };

    private List<Button> allButtons = new List<Button>();
    private bool buttonsDisabled = false;
    private Vector3 originalScale;

    void Start()
    {
        GenerateKeyboard();
        originalScale = allButtons[0].transform.localScale;
    }

    void GenerateKeyboard()
    {
        float padding = 4f; 
        float parentWidth = keyboardParent.GetComponent<RectTransform>().rect.width - (padding * 2);
        float spacing = 5f;

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
        float buttonHeight = 65f; // Adjust the button height as needed

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
        if (buttonsDisabled)
        {
            StartCoroutine(ShakeAnimation(btn.gameObject));
            if (gameManager.selectedPosition == GameManager.TextPosition.None)
            {
                warningText.gameObject.SetActive(true);
            }
            return;
        }

        AudioSource.PlayClipAtPoint(keyAudioClip, Vector3.zero, 1);
        StartCoroutine(PopAnimation(btn.gameObject));
        StartCoroutine(WaitAndProcessTurn(letter));
    }

    IEnumerator WaitAndProcessTurn(char letter)
    {
        yield return new WaitForSeconds(0.15f);
        gameManager.ProcessTurn(letter);
    }

    public void DisableAllButtons()
    {
        foreach (Button btn in allButtons)
        {
            var colors = btn.colors;
            colors.normalColor = colors.disabledColor;
            colors.pressedColor = colors.disabledColor;
            colors.selectedColor = colors.disabledColor;
            btn.colors = colors;

            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            btnText.color = new Color(btnText.color.r, btnText.color.g, btnText.color.b, 0.25f);
            btn.transform.localScale = originalScale;
        }
        buttonsDisabled = true;
    }

    public void EnableAllButtons()
    {
        warningText.gameObject.SetActive(false);

        foreach (Button btn in allButtons)
        {
            var colors = btn.colors;
            colors.normalColor = new Color(colors.normalColor.r, colors.normalColor.g, colors.normalColor.b, 1f);
            colors.pressedColor = colors.normalColor;
            colors.selectedColor = colors.normalColor;
            btn.colors = colors;

            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            btnText.color = new Color(btnText.color.r, btnText.color.g, btnText.color.b, 1f);
        }
        buttonsDisabled = false;
    }

    public void Show()
    {
        foreach (Button btn in allButtons)
        {
            btn.transform.localScale = originalScale;
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        warningText.gameObject.SetActive(false);

        gameObject.SetActive(false);
    }

    IEnumerator PopAnimation(GameObject btnGameObject)
    {
        Vector3 targetScale = originalScale * 1.5f;

        // Scale up
        float currentTime = 0f;
        float duration = 0.15f;
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

    IEnumerator ShakeAnimation(GameObject btnGameObject)
    {
        var originalPosition = btnGameObject.transform.localPosition;
        float duration = 0.5f;
        float magnitude = 10f;

        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            float x = originalPosition.x + Random.Range(-1f, 1f) * magnitude;
            float y = originalPosition.y + Random.Range(-1f, 1f) * magnitude;

            btnGameObject.transform.localPosition = new Vector3(x, y, originalPosition.z);
            yield return null; // Wait for the next frame before continuing the loop
        }

        btnGameObject.transform.localPosition = originalPosition; // Reset to original position
    }
}
