using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.UI.ProceduralImage;

public class ShopItem : MonoBehaviour
{
    public int id;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI titleTextOverlay;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI warningText;
    public Color normalWarningColor;
    public Color blockedWarningColor;
    public Button buyButton;
    public ProceduralImage backgroundImage, outlineImage;
    public Image iconImage;
    public TextMeshProUGUI extraInfoText;

    private int cost;
    private Func<IEnumerator> coroutine;

    public void Initialize(int id, string title, string body, string warning, int cost, int currency, bool interactable, bool additionalInteractableCriteria, bool isActive, string extraInfoText, Sprite iconSprite, Action<ShopItem> onBuyPressed, Func<IEnumerator> coroutine)
    {
        this.id = id;
        this.cost = cost;
        this.coroutine = coroutine;

        bool canAfford = currency >= cost;
        cost = Math.Max(cost, 0);

        Color.RGBToHSV(backgroundImage.color, out float H, out float S, out float V);
        V = Mathf.Clamp(V + 0.25f, 0, 1);
        Color brighterColor = Color.HSVToRGB(H, S, V);
        var color = ColorUtility.ToHtmlStringRGB(brighterColor);

        StartCoroutine(UpdateTitle(title, cost, isActive, canAfford, color));

        outlineImage.color = brighterColor;

        bodyText.text = body;
        bodyText.lineSpacing = bodyText.text.Contains("¤") ? -30f : -3f;

        if (interactable)
        {
            warningText.text = warning;
        }
        else
        {
            warningText.text = $"<sprite index=0> {warning}";
        }
        warningText.color = interactable ? normalWarningColor : blockedWarningColor;

        buyButton.interactable = interactable && canAfford && additionalInteractableCriteria;
        var buyButtonText = buyButton.GetComponentInChildren<TextMeshProUGUI>();
        buyButtonText.color = new Color(buyButtonText.color.r, buyButtonText.color.g, buyButtonText.color.b, buyButton.interactable ? 1 : 0.5f);

        Color complementaryColor = new Color(1 - brighterColor.r, 1 - brighterColor.g, 1 - brighterColor.b);
        iconImage.sprite = iconSprite;
        iconImage.color = complementaryColor;

        if (!string.IsNullOrEmpty(extraInfoText))
        {
            this.extraInfoText.text = extraInfoText;
        }
        else
        {
            this.extraInfoText.text = "";
        }

        buyButton.onClick.RemoveAllListeners();
        if (onBuyPressed != null)
        {
            buyButton.onClick.AddListener(() =>
            {
                buyButton.interactable = false;
                StartCoroutine(ButtonPopAnimation());
                onBuyPressed(this);
            });
        }

        AdjustTextPositions(isActive);
    }

    public int GetCost()
    {
        return cost;
    }

    public Func<IEnumerator> GetCoroutine()
    {
        return coroutine;
    }

    private IEnumerator UpdateTitle(string title, int cost, bool isActive, bool canAfford, string color)
    {
        titleText.text = "";
        titleTextOverlay.text = "";

        var costTextActive = "<sprite=0>";
        var costTextNonActive = $"{cost}¤";
        var costText = isActive ? costTextActive : costTextNonActive;
        var padding = "20, 20, 12, 12";

        // Set the final text with chosen font size
        string coloredCostText = $"<color={(canAfford ? "green" : "red")}>{costText}</color>";
        titleText.text = $"<mark color=#{color} padding=\"{padding}\">{title}</mark> - {coloredCostText}";
        titleTextOverlay.text = $"{title} - {coloredCostText}";

        yield return null;
    }

    private void AdjustTextPositions(bool isActive)
    {
        if (isActive)
        {
            titleText.rectTransform.offsetMin = new Vector2(0, titleText.rectTransform.offsetMin.y);
            titleText.rectTransform.offsetMax = new Vector2(-13.5f, titleText.rectTransform.offsetMax.y);
            titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, 80);

            titleTextOverlay.rectTransform.offsetMin = new Vector2(0, titleTextOverlay.rectTransform.offsetMin.y);
            titleTextOverlay.rectTransform.offsetMax = new Vector2(-13.5f, titleTextOverlay.rectTransform.offsetMax.y);
            titleTextOverlay.rectTransform.anchoredPosition = new Vector2(titleTextOverlay.rectTransform.anchoredPosition.x, 80);
        }
        else
        {
            titleText.rectTransform.offsetMin = new Vector2(21, titleText.rectTransform.offsetMin.y);
            titleText.rectTransform.offsetMax = new Vector2(-19, titleText.rectTransform.offsetMax.y);
            titleText.rectTransform.anchoredPosition = new Vector2(titleText.rectTransform.anchoredPosition.x, 84);

            titleTextOverlay.rectTransform.offsetMin = new Vector2(21, titleTextOverlay.rectTransform.offsetMin.y);
            titleTextOverlay.rectTransform.offsetMax = new Vector2(-19, titleTextOverlay.rectTransform.offsetMax.y);
            titleTextOverlay.rectTransform.anchoredPosition = new Vector2(titleTextOverlay.rectTransform.anchoredPosition.x, 84);
        }
    }

    private IEnumerator ButtonPopAnimation()
    {
        Vector3 originalScale = buyButton.transform.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        float duration = 0.1f;

        // Scale up
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            buyButton.transform.localScale = Vector3.Lerp(originalScale, targetScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Scale back down
        elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            buyButton.transform.localScale = Vector3.Lerp(targetScale, originalScale, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        buyButton.transform.localScale = originalScale;
    }
}

[Serializable]
public class ShopItemInfo
{
    public int id;
    public string title, body, warning;
    public ShopItemType type;
    public Sprite iconSprite;
    public bool isFavored;
}

[Serializable]
public enum ShopItemType
{
    Points = 0,
    Helper = 1,
    Money = 2
}