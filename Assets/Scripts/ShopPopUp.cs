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
    private HashSet<int> previousShopItemIds = new HashSet<int>();

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

    public void RefreshShop(bool saveChanges)
    {
        previousShopItemIds = visibleShopItems.Select(s => s.id).ToHashSet();
        visibleShopItems.Clear();
        GetShopItems(saveChanges, true);
    }

    private void GetShopItems(bool saveChanges = false, bool overrideExistingItems = false)
    {
        if (visibleShopItems.Count == 0)
        {
            var saveObject = SaveManager.Load();
            if (overrideExistingItems && saveChanges)
            {
                saveObject.ShopItemIds.Clear();
            }

            if (saveObject.ShopItemIds.Count == 0 || overrideExistingItems)
            {
                while (visibleShopItems.Count < 3)
                {
                    var randomShopItem = shopItems[Random.Range(0, shopItems.Count)];
                    if (!visibleShopItems.Any(v => v.id == randomShopItem.id) && !previousShopItemIds.Any(p => p == randomShopItem.id))
                    {
                        visibleShopItems.Add(randomShopItem);
                        if (saveChanges)
                        {
                            saveObject.ShopItemIds.Add(randomShopItem.id);
                        }
                    }
                }
            }
            else
            {
                foreach (var id in saveObject.ShopItemIds)
                {
                    visibleShopItems.Add(shopItems.FirstOrDefault(s => s.id == id));
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
            RefreshShop(false);
            BuyItem(10, null);
            StartCoroutine(ScrollToTop());
        }
    }

    private void BuyItem(int cost, ShopItem item)
    {
        moneyAudioSource?.Play();

        if (item != null)
        {
            var coroutine = item.GetCoroutine();
            StartCoroutine(coroutine());
        }

        RefreshPopUp(cost);
    }

    private IEnumerator DoAction(int cost, Action action, bool shouldHide)
    {
        if (shouldHide)
        {
            yield return new WaitForSeconds(GetTimeToWait(cost));
        }

        action();

        if (shouldHide)
        {
            Hide(false);
        }
        else
        {
            yield return null;
        }
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
        return cost == 1 ? 0.2f : cost < 5 ? 0.25f : 0.5f;
    }

    private void RefreshPopUp(int cost)
    {
        currency -= cost;
        gameManager.currency -= cost;
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
            shopItemPrefabs[i].Initialize(shopItem.id, shopItem.title, shopItem.body, shopItem.warning, cost, currency, GetInteractable(shopItem.id), IsActive(shopItem.id), shopItem.iconSprite, (item) => BuyPressed(item), () => GetCoroutine(shopItem.id, cost));
        }

        bool canAffordReshuffle = currency >= 10;
        shuffleButton.interactable = canAffordReshuffle;
        shuffleButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Restock (<color={(canAffordReshuffle ? "green" : "red")}>$10</color>)";
    }

    private IEnumerator GetCoroutine(int id, int cost)
    {
        switch (id)
        {
            case 0:
                return DoAction(cost, () => gameManager.ShowHint(), true);
            case 1:
                return DoAction(cost, () => gameManager.ShuffleComboLetters(), true);
            case 2:
                return DoAction(cost, () => gameManager.EnableMultiplier(), false);
            case 3:
                return DoAction(cost, () => gameManager.EnableEvenMultiplier(), false);
            case 4:
                return DoAction(cost, () => gameManager.EnableDoubleWealth(), false);
            case 5:
                return DoAction(cost, () => gameManager.DoDoubleTurn(), false);
            case 6:
                return DoAction(cost, () => gameManager.ResetWord(), true);
            case 7:
                return DoAction(cost, () => gameManager.EnableLongWordMultiplier(), false);
            case 8:
                return DoAction(cost, () => gameManager.UndoTurn(), true);
            case 9:
                return DoAction(cost, () => gameManager.EnableDoubleBluff(), false);
        }

        return null;
    }

    private int GetCost(int id)
    {
        bool roundEnded = gameManager.IsRoundEnded();
        bool gameEnded = gameManager.IsGameEnded();
        int roundsWon = gameManager.aiLivesText.GetStartLives() - gameManager.aiLivesText.LivesRemaining();

        int substringLength = substring.Length;
        if (roundEnded)
        {
            substringLength = 0;
        }

        switch (id)
        {
            case 0:
                return (int)Mathf.Round((substringLength + 1) * multiplier);
            case 1:
                return 5;
            case 2:
                return gameEnded ? 4 : (roundsWon + 1) * 4;
            case 3:
                return gameEnded ? 3 : (roundsWon + 1) * 3;
            case 4:
                return (int)(Mathf.Round(5 * multiplier));
            case 5:
                return (int)Mathf.Round((substringLength + 1) * multiplier);
            case 6:
                return (gameManager.ResetWordUses + 1) * 5;
            case 7:
                return gameEnded ? 4 : (roundsWon + 1) * 3;
            case 8:
                return (int)Mathf.Round((substringLength + 1) * 1.5f * multiplier);
            case 9:
                return (int)Mathf.Round(5 * multiplier);
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
            case 4:
                return !gameManager.HasDoubleWealth;
            case 5:
                return gameManager.IsPlayerTurn() && !gameManager.HasDoubleTurn;
            case 6:
                return gameManager.IsPlayerTurn();
            case 7:
                return !gameManager.HasLongWordMultiplier;
            case 8:
                return gameManager.IsPlayerTurn() && gameManager.gameWord.Length > 0;
            case 9:
                return !gameManager.HasDoubleBluff;
        }

        return false;
    }

    private bool IsActive(int id)
    {
        switch (id)
        {
            case 0:
                return false;
            case 1:
                return false;
            case 2:
                return gameManager.HasBonusMultiplier;
            case 3:
                return gameManager.HasEvenWordMultiplier;
            case 4:
                return gameManager.HasDoubleWealth;
            case 5:
                return gameManager.HasDoubleTurn;
            case 6:
                return false;
            case 7:
                return gameManager.HasLongWordMultiplier;
            case 8:
                return false;
            case 9:
                return gameManager.HasDoubleBluff;
        }

        return false;
    }
}