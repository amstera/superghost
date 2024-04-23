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
    public List<Color> colors = new List<Color>();
    public GameObject newIndicator;

    public AudioSource clickAudioSource, moneyAudioSource;

    public float fadeDuration = 0.25f;
    public float scaleDuration = 0.25f;

    private Vector3 originalScale;
    private int currency;
    private float multiplier;
    private string substring;
    private List<ShopItemInfo> visibleShopItems = new List<ShopItemInfo>();
    private HashSet<int> previousShopItemIds = new HashSet<int>();
    private SaveObject saveObject;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        saveObject = SaveManager.Load();
        ResetPopUp();
    }

    public void Show(int currency, string substring, Difficulty difficulty, bool showNewIndicator)
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        newIndicator.SetActive(showNewIndicator);

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

        Shuffle(colors);
        for(int i = 0; i < shopItemPrefabs.Count; i++)
        {
            var shopItem = shopItemPrefabs[i];
            shopItem.backgroundImage.color = colors[i];
        }
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
            StartCoroutine(RefreshShopWithAnimation(false, () => BuyItem(10, null)));
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

            saveObject.Statistics.UsedShopItemIds[item.id] = saveObject.Statistics.UsedShopItemIds.GetValueOrDefault(item.id) + 1;
            saveObject.RunStatistics.UsedShopItemIds[item.id] = saveObject.RunStatistics.UsedShopItemIds.GetValueOrDefault(item.id) + 1;
            SaveManager.Save(saveObject);
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

    public IEnumerator RefreshShopWithAnimation(bool saveChanges, Action action)
    {
        var scaleTime = scaleDuration * 0.5f;

        // Scale out all shop items
        foreach (var itemPrefab in shopItemPrefabs)
        {
            StartCoroutine(ScaleOut(itemPrefab.gameObject, scaleTime));
        }

        // Wait for all to scale out
        yield return new WaitForSeconds(scaleTime);

        // Proceed with refreshing the shop
        RefreshShop(saveChanges);
        action?.Invoke();

        // Scale in all shop items
        foreach (var itemPrefab in shopItemPrefabs)
        {
            StartCoroutine(ScaleIn(itemPrefab.gameObject, scaleTime));
        }
    }


    private IEnumerator ScaleOut(GameObject target, float duration)
    {
        float currentTime = 0;
        Vector3 startScale = target.transform.localScale;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            target.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, currentTime / duration);
            yield return null;
        }
        target.transform.localScale = Vector3.zero;
    }

    private IEnumerator ScaleIn(GameObject target, float duration)
    {
        float currentTime = 0;
        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            target.transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, currentTime / duration);
            yield return null;
        }
        target.transform.localScale = originalScale;
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
        var reshuffleText = shuffleButton.GetComponentInChildren<TextMeshProUGUI>();
        reshuffleText.text = $"Restock (<color={(canAffordReshuffle ? "green" : "red")}>$10</color>)";
        reshuffleText.color = new Color(reshuffleText.color.r, reshuffleText.color.g, reshuffleText.color.b, canAffordReshuffle ? 1 : 0.5f);
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
            case 10:
                return DoAction(cost, () => gameManager.EnableChanceMultiplier(), false);
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
                return (int)RoundHalfUp((substringLength + 1) * multiplier);
            case 1:
                return 5;
            case 2:
                return gameEnded ? 3 : (roundsWon + 1) * 3;
            case 3:
                return gameEnded ? 3 : (roundsWon + 1) * 3;
            case 4:
                return (int)RoundHalfUp(5 * multiplier);
            case 5:
                return (int)RoundHalfUp((substringLength + 1) * multiplier);
            case 6:
                return (gameManager.ResetWordUses + 1) * 4;
            case 7:
                return gameEnded ? 3 : (roundsWon + 1) * 3;
            case 8:
                return (int)RoundHalfUp((substringLength + 1) * 1.5f * multiplier);
            case 9:
                return (int)RoundHalfUp(5 * multiplier);
            case 10:
                return 5;
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
            case 10:
                return gameManager.ChanceMultiplier == 1;
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
            case 10:
                return gameManager.ChanceMultiplier != 1;
        }

        return false;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            // Swap elements
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    float RoundHalfUp(float number)
    {
        return Mathf.Floor(number + 0.5f);
    }
}