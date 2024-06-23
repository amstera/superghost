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
    public ProceduralImage backgroundImage;
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

        var costText = isActive ? $"<sprite=0>" : $"{cost}¤";
        string coloredCostText = $"<color={(canAfford ? "green" : "red")}>{costText}</color>";
        titleText.text = $"<mark color=#{color} padding=\"20, 20, 12, 12\">{title}</mark> - {coloredCostText}";
        titleTextOverlay.text = $"{title} - {coloredCostText}";

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