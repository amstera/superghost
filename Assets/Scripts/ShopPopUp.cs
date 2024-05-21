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
    public TextMeshProUGUI shopNewItemsText, discountText;
    public ActiveEffectsText activeEffectsText;

    public AudioSource clickAudioSource, moneyAudioSource;

    public float fadeDuration = 0.25f;
    public float scaleDuration = 0.25f;

    private Vector3 originalScale;
    private int currency;
    private string substring;
    private float totalCostPercentage = 1;
    private List<ShopItemInfo> visibleShopItems = new List<ShopItemInfo>();
    private HashSet<int> previousShopItemIds = new HashSet<int>();
    private SaveObject saveObject;

    private void Awake()
    {
        originalScale = popUpGameObject.transform.localScale;
        saveObject = SaveManager.Load();
        ResetPopUp();
    }

    public void Show(int currency, string substring, bool showNewIndicator)
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        shopNewItemsText.gameObject.SetActive(showNewIndicator);

        this.currency = currency;
        this.substring = substring;

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

    public void RefreshView()
    {
        if (canvasGroup.interactable) // if it's showing
        {
            currency = gameManager.currency;
            currencyText.SetPoints(currency);
            InitializeShopItems();
        }
    }

    public List<ShopItemInfo> GetVisibleShopItems()
    {
        return visibleShopItems;
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
                int currentLevel = saveObject.CurrentLevel;
                var availableShopItems = shopItems.Where(item => !previousShopItemIds.Contains(item.id)).ToList();
                FilterAvailableShopItems(availableShopItems);
                Shuffle(availableShopItems);

                var helpers = availableShopItems.Where(item => item.type == ShopItemType.Helper).ToList();
                var pointsItems = availableShopItems.Where(item => item.type == ShopItemType.Points).ToList();

                ShopItemInfo SelectWeightedRandom(List<ShopItemInfo> items, float favoredWeight = 1.25f)
                {
                    if (currentLevel == 0)
                    {
                        favoredWeight += 0.25f;
                    }

                    ShopItemInfo selected = null;
                    double maxWeight = 0;
                    double weight;

                    foreach (var item in items)
                    {
                        weight = Random.Range(0, item.isFavored ? favoredWeight : 1);
                        if (weight > maxWeight)
                        {
                            maxWeight = weight;
                            selected = item;
                        }
                    }

                    return selected;
                }

                var chosenHelper = SelectWeightedRandom(helpers);
                var chosenPointsItem = SelectWeightedRandom(pointsItems);

                availableShopItems.Remove(chosenHelper);
                availableShopItems.Remove(chosenPointsItem);

                var chosenThirdItem = SelectWeightedRandom(availableShopItems);

                var chosenItems = new List<ShopItemInfo> {chosenHelper, chosenPointsItem, chosenThirdItem};
                Shuffle(chosenItems);

                foreach (var item in chosenItems)
                {
                    visibleShopItems.Add(item);
                    if (saveChanges)
                    {
                        saveObject.ShopItemIds.Add(item.id);
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
        int restockCost = (int)RoundHalfUp(6 * totalCostPercentage);
        if (currency >= restockCost)
        {
            StartCoroutine(RefreshShopWithAnimation(false, () => BuyItem(restockCost, null)));
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

    private IEnumerator DoAction(int id, int cost, Action action, bool shouldHide, bool shouldAddEffect)
    {
        if (shouldHide)
        {
            yield return new WaitForSeconds(GetTimeToWait(cost));
        }

        if (shouldAddEffect)
        {
            var shopItem = shopItemPrefabs.Find(s => s.id == id);
            var shopItemInfo = shopItems.Find(s => s.id == id);
            var details = new ShopItemEffectDetails(id, shopItemInfo.title, shopItemInfo.body, shopItem.backgroundImage.color, cost);
            activeEffectsText.AddEffect(details);
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
        cost = Math.Abs(cost);
        return cost == 0 ? 0 : cost == 1 ? 0.2f : cost < 5 ? 0.25f : 0.6f;
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

    public void ApplyDiscount(float amount)
    {
        totalCostPercentage = amount;
        if (amount == 1)
        {
            discountText.gameObject.SetActive(false);
        }
        else
        {
            discountText.gameObject.SetActive(true);
            discountText.text = $"{(1 - amount) * 100}% Off";
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

    private void FilterAvailableShopItems(List<ShopItemInfo> availableShopItems)
    {
        availableShopItems.RemoveAll(a => GetShopItemAdjustableDetails(a.id, 0).IsActive); // remove any shop items from being chosen if they're currently active

        if (currency <= 5)
        {
            availableShopItems.RemoveAll(s => s.id == 17); // remove Price Cut if you don't have enough currency
        }

        if (gameManager.playerLivesText.LivesRemaining() > 1)
        {
            availableShopItems.RemoveAll(s => s.id == 21); // Last Resort only can show up if you have 1 life remaining
        }

        foreach (var criteria in gameManager.criteriaText.GetCurrentCriteria())
        {
            if (criteria is NoComboLetters)
            {
                availableShopItems.RemoveAll(s => s.id == 1); // remove Shuffle 2x Points if it's that criteria level
            }
            else if (criteria is OddLetters)
            {
                availableShopItems.RemoveAll(s => s.id == 3); // remove Even Flow if it's that criteria level
            }
            else if (criteria is EvenLetters)
            {
                availableShopItems.RemoveAll(s => s.id == 15); // remove Odd Flow if it's that criteria level
            }
        }
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
            int cost = (int)RoundHalfUp(GetCost(shopItem.id) * totalCostPercentage);
            var shopItemAdjustableDetails = GetShopItemAdjustableDetails(shopItem.id, cost);
            shopItemPrefabs[i].Initialize(shopItem.id, shopItem.title, shopItem.body, shopItem.warning, cost, currency, shopItemAdjustableDetails.IsInteractable, shopItemAdjustableDetails.IsAdditionalInteractable, shopItemAdjustableDetails.IsActive, shopItemAdjustableDetails.ExtraInfoText, shopItem.iconSprite, (item) => BuyPressed(item), () => shopItemAdjustableDetails.Coroutine);
        }

        int restockCost = (int)RoundHalfUp(6 * totalCostPercentage);
        bool canAffordReshuffle = currency >= restockCost;
        shuffleButton.interactable = canAffordReshuffle;
        var reshuffleText = shuffleButton.GetComponentInChildren<TextMeshProUGUI>();
        reshuffleText.text = $"Restock (<color={(canAffordReshuffle ? "green" : "red")}>${restockCost}</color>)";
        reshuffleText.color = new Color(reshuffleText.color.r, reshuffleText.color.g, reshuffleText.color.b, canAffordReshuffle ? 1 : 0.5f);
    }

    private ShopItemAdjustableDetails GetShopItemAdjustableDetails(int id, int cost)
    {
        switch (id)
        {
            case 0:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.ShowHint(), true, false),
                    gameManager.IsPlayerTurn(), true, false, "");
            case 1:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.ShuffleComboLetters(), true, false),
                    gameManager.IsDoneRound(),
                    gameManager.comboText.gameObject.activeSelf, false, "");
            case 2:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableMultiplier(), false, true),
                    !gameManager.HasBonusMultiplier, true, gameManager.HasBonusMultiplier, "");
            case 3:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableEvenMultiplier(), false, true),
                    !gameManager.HasEvenWordMultiplier, true, gameManager.HasEvenWordMultiplier, "");
            case 4:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableDoubleWealth(), false, true),
                    !gameManager.HasDoubleWealth, true, gameManager.HasDoubleWealth, "");
            case 5:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.DoDoubleTurn(), false, true),
                    gameManager.IsPlayerTurn(), !gameManager.HasDoubleTurn, gameManager.HasDoubleTurn, "");
            case 6:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.ResetWord(), true, false),
                    gameManager.IsPlayerTurn(), gameManager.gameWord.Length > 0, false, "");
            case 7:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableLongWordMultiplier(), false, true),
                    !gameManager.HasLongWordMultiplier, true, gameManager.HasLongWordMultiplier, "");
            case 8:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.UndoTurn(), true, false),
                    gameManager.IsPlayerTurn(), gameManager.gameWord.Length > 0, false, "");
            case 9:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableDoubleBluff(), false, true),
                    !gameManager.HasDoubleBluff, true, gameManager.HasDoubleBluff, "");
            case 10:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableChanceMultiplier(), false, true),
                    gameManager.ChanceMultiplier == 1, true, gameManager.ChanceMultiplier != 1,
                    gameManager.ChanceMultiplier != 1 ? $"{gameManager.ChanceMultiplier}x" : "");
            case 11:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.RestoreLife(true), true, false),
                    gameManager.IsPlayerTurn(), !gameManager.playerLivesText.HasFullLives(), false, "");
            case 12:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.RestoreLife(false), true, false),
                    gameManager.IsPlayerTurn(), !gameManager.aiLivesText.HasFullLives(), false, "");
            case 13:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableMoneyLose(), false, true),
                    !gameManager.HasLoseMoney, true, gameManager.HasLoseMoney, "");
            case 14:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.LoseLifeMoney(), true, false),
                    gameManager.IsPlayerTurn(), gameManager.playerLivesText.LivesRemaining() > 1, false, "");
            case 15:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableOddMultiplier(), false, true),
                    !gameManager.HasOddWordMultiplier, true, gameManager.HasOddWordMultiplier, "");
            case 16:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableDoubleEnded(), false, true),
                    !gameManager.HasDoubleEndedMultiplier, true, gameManager.HasDoubleEndedMultiplier, "");
            case 17:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => ApplyDiscount(0.5f), false, true),
                    totalCostPercentage == 1, true, totalCostPercentage != 1, "");
            case 18:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.MatchAILives(), true, false),
                    gameManager.IsPlayerTurn(),
                    gameManager.playerLivesText.LivesRemaining() != gameManager.aiLivesText.LivesRemaining(), false, "");
            case 19:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableBonusMoney(), false, true),
                    !gameManager.HasBonusMoney, true, gameManager.HasBonusMoney,
                    gameManager.IsGameEnded() ? "$0" : $"${(gameManager.playerLivesText.GetStartLives() - gameManager.playerLivesText.LivesRemaining()) * 3}");
            case 20:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.SkipTurn(), true, false),
                    gameManager.IsPlayerTurn(), true, false, "");
            case 21:
                return new ShopItemAdjustableDetails(
                    DoAction(id, cost, () => gameManager.EnableLastResortMultiplier(), false, true),
                    !gameManager.HasLastResortMultiplier, gameManager.playerLivesText.LivesRemaining() == 1, gameManager.HasLastResortMultiplier, "");
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
                return substringLength + 1;
            case 1:
                return 5;
            case 2:
                return gameEnded ? 3 : (roundsWon + 1) * 3;
            case 3:
                return gameEnded ? 2 : (roundsWon + 1) * 2;
            case 4:
                return 5;
            case 5:
                return substringLength + 1;
            case 6:
                return (gameManager.ResetWordUses + 1) * 4;
            case 7:
                return gameEnded ? 2 : (roundsWon + 1) * 2;
            case 8:
                return (int)RoundHalfUp((substringLength + 1) * 1.25f);
            case 9:
                return gameEnded ? 2 : (roundsWon + 1) * 2;
            case 10:
                return gameEnded ? 5 : 5 + (int)RoundHalfUp(roundsWon * 1.5f);
            case 11:
                return (int)Math.Pow(2, gameManager.PlayerRestoreLivesUses) * 5;
            case 12:
                return (int)Math.Pow(2, gameManager.AIRestoreLivesUses) * 5;
            case 13:
                return 5;
            case 14:
                return -10;
            case 15:
                return gameEnded ? 2 : (roundsWon + 1) * 2;
            case 16:
                return gameEnded ? 2 : (roundsWon + 1) * 2;
            case 17:
                return 5;
            case 18:
                return (int)Math.Pow(2, gameManager.AILivesMatch) * 5;
            case 19:
                return 5;
            case 20:
                return (int)RoundHalfUp((substringLength + 1) * 1.25f);
            case 21:
                return 5;
        }

        return -1;
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

public class ShopItemAdjustableDetails
{
    public IEnumerator Coroutine;
    public bool IsInteractable;
    public bool IsAdditionalInteractable;
    public bool IsActive;
    public string ExtraInfoText;

    public ShopItemAdjustableDetails(IEnumerator coroutine, bool isInteractable, bool isAdditionalInteractable, bool isActive, string extraInfoText)
    {
        Coroutine = coroutine;
        IsInteractable = isInteractable;
        IsAdditionalInteractable = isAdditionalInteractable;
        IsActive = isActive;
        ExtraInfoText = extraInfoText;
    }
}