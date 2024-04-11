using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class ShopItem : MonoBehaviour
{
    public int id;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    public TextMeshProUGUI warningText;
    public Color normalWarningColor;
    public Color blockedWarningColor;
    public Button buyButton;

    private int cost;
    private Func<IEnumerator> coroutine;

    public void Initialize(int id, string title, string body, string warning, int cost, int currency, bool interactable, Action<ShopItem> onBuyPressed, Func<IEnumerator> coroutine)
    {
        this.id = id;
        this.cost = cost;
        this.coroutine = coroutine;

        bool canAfford = currency >= cost;
        titleText.text = $"{title} - <color={(canAfford ? "green" : "red")}>${cost}</color>";
        bodyText.text = body;
        if (interactable)
        {
            warningText.text = warning;
        }
        else
        {
            warningText.text = $"<sprite index=0> {warning}";
        }
        warningText.color = interactable ? normalWarningColor : blockedWarningColor;
        buyButton.interactable = interactable && canAfford;

        buyButton.onClick.RemoveAllListeners();
        if (onBuyPressed != null)
        {
            buyButton.onClick.AddListener(() =>
            {
                buyButton.interactable = false;
                onBuyPressed(this);
            });
        }
    }

    public int GetCost()
    {
        return cost;
    }

    public Func<IEnumerator> GetCoroutine()
    {
        return coroutine;
    }
}

[Serializable]
public class ShopItemInfo
{
    public int id;
    public string title, body, warning;
}