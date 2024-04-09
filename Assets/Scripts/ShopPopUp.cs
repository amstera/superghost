using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System;
using Random = UnityEngine.Random;
using TMPro;

public class ShopPopUp : MonoBehaviour
{
    public GameManager gameManager;
    public CanvasGroup canvasGroup;
    public GameObject popUpGameObject;
    public ScrollRect scrollRect;
    public PointsText currencyText;
    public Button shuffleButton;
    public List<ShopItemInfo> shopItems = new List<ShopItemInfo>();
    public List<ShopItem> shopItemPrefabs = new List<ShopItem>();

    public AudioSource clickAudioSource, moneyAudioSource;

    public float fadeDuration = 0.25f;
    public float scaleDuration = 0.25f;

    private Vector3 originalScale;
    private int currency;
    private float multiplier;
    private string substring;
    private List<ShopItemInfo> visibleShopItems = new List<ShopItemInfo>();

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        ResetPopUp();
    }

    public void Show(int currency, string substring, Difficulty difficulty)
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        this.currency = currency;
        this.substring = substring;
        multiplier = difficulty == Difficulty.Hard ? 2 : difficulty == Difficulty.Easy ? 0.5f : 1;

        GetShopItems();

        currencyText.SetPoints(currency);

        InitializeShopItems();

        StartCoroutine(FadeIn());
        StartCoroutine(ScaleIn());

        StartCoroutine(ScrollToTop());
    }

    public void RefreshShop()
    {
        visibleShopItems.Clear();
        GetShopItems();
    }

    private void GetShopItems()
    {
        if (visibleShopItems.Count == 0)
        {
            while (visibleShopItems.Count < 3)
            {
                var randomShopItem = shopItems[Random.Range(0, shopItems.Count)];
                if (!visibleShopItems.Any(v => v.id == randomShopItem.id))
                {
                    visibleShopItems.Add(randomShopItem);
                }
            }
        }
    }

    private IEnumerator FadeIn()
    {
        float currentTime = 0;
        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, currentTime / fadeDuration);
            yield return null;
        }
    }

    private IEnumerator ScaleIn()
    {
        popUpGameObject.transform.localScale = Vector3.zero; // Ensure it starts from zero
        float currentTime = 0;
        while (currentTime < scaleDuration)
        {
            currentTime += Time.deltaTime;
            popUpGameObject.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, currentTime / scaleDuration);
            yield return null;
        }
    }

    public void Hide(bool playSound)
    {
        if (playSound)
        {
            clickAudioSource?.Play();
        }

        StopAllCoroutines();
        ResetPopUp();
    }

    public void BuyPressed(ShopItem item)
    {
        int cost = item.GetCost();
        if (currency >= cost)
        {
            BuyItem(cost, item);
        }
    }

    public void ReShuffle()
    {
        if (currency >= 10)
        {
            RefreshShop();
            BuyItem(10, null);
        }
    }

    private void BuyItem(int cost, ShopItem item)
    {
        moneyAudioSource?.Play();

        RefreshPopUp(cost);

        if (item != null)
        {
            var coroutine = item.GetCoroutine();
            StartCoroutine(coroutine());
        }
    }

    private IEnumerator DoAction(int cost, Action action)
    {
        yield return new WaitForSeconds(GetTimeToWait(cost));

        action();
        Hide(false);
    }

    private void ResetPopUp()
    {
        popUpGameObject.transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator ScrollToTop()
    {
        // Wait for end of frame to let the UI update
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 1.0f;
    }

    private float GetTimeToWait(int cost)
    {
        return cost == 1 ? 0.2f : cost < 5 ? 0.35f : 0.65f;
    }

    private void RefreshPopUp(int cost)
    {
        currency -= cost;
        currencyText.AddPoints(-cost);

        InitializeShopItems();
    }

    private void InitializeShopItems()
    {
        if (visibleShopItems.Count != shopItemPrefabs.Count)
        {
            Debug.LogWarning("This can't happen! Shop items should match.");
            return;
        }

        for (int i = 0; i < shopItemPrefabs.Count; i++)
        {
            var shopItem = visibleShopItems[i];
            int cost = GetCost(shopItem.id);
            shopItemPrefabs[i].Initialize(shopItem.id, shopItem.title, shopItem.body, shopItem.warning, cost, currency, GetInteractable(shopItem.id), (item) => BuyPressed(item), () => GetCoroutine(shopItem.id, cost));
        }

        bool canAffordReshuffle = currency >= 10;
        shuffleButton.interactable = canAffordReshuffle;
        shuffleButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Re-Shuffle (<color={(canAffordReshuffle ? "green" : "red")}>$10</color>)";
    }

    private IEnumerator GetCoroutine(int id, int cost)
    {
        switch (id)
        {
            case 0:
                return DoAction(cost, () => gameManager.ShowHint(cost));
            case 1:
                return DoAction(cost, () => gameManager.ShuffleComboLetters(cost));
            case 2:
                return DoAction(cost, () => gameManager.EnableMultiplier(cost));
            case 3:
                return DoAction(cost, () => gameManager.EnableEvenMultiplier(cost));
        }

        return null;
    }

    private int GetCost(int id)
    {
        bool roundEnded = gameManager.IsGameEnded();
        int roundsWon = gameManager.aiLivesText.GetStartLives() - gameManager.aiLivesText.LivesRemaining();

        switch (id)
        {
            case 0:
                return roundEnded ? 0 : (int)Mathf.Round(Mathf.Max(substring.Length * multiplier, 1));
            case 1:
                return 5;
            case 2:
                return gameManager.aiLivesText.IsGameOver() ? 5 : (roundsWon + 1) * 5;
            case 3:
                return gameManager.aiLivesText.IsGameOver() ? 3 : (roundsWon + 1) * 3;
        }

        return -1;
    }

    private bool GetInteractable(int id)
    {
        switch (id)
        {
            case 0:
                return gameManager.IsPlayerTurn();
            case 1:
                return gameManager.IsDoneRound() && gameManager.comboText.gameObject.activeSelf;
            case 2:
                return !gameManager.HasBonusMultiplier;
            case 3:
                return !gameManager.HasEvenWordMultiplier;
        }

        return false;
    }
}