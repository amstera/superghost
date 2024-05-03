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
    private float multiplier;
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

    public void Show(int currency, string substring, Difficulty difficulty, bool showNewIndicator)
    {
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        shopNewItemsText.gameObject.SetActive(showNewIndicator);

        this.currency = currency;
        this.substring = substring;
        multiplier = difficulty == Difficulty.Hard ? 1.5f : difficulty == Difficulty.Easy ? 0.5f : 1;

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
                var availableShopItems = shopItems.Where(item => !previousShopItemIds.Contains(item.id)).ToList();
                FilterAvailableShopItems(availableShopItems);
                Shuffle(availableShopItems);

                var helpers = availableShopItems.Where(item => item.type == ShopItemType.Helper).ToList();
                var pointsItems = availableShopItems.Where(item => item.type == ShopItemType.Points).ToList();

                ShopItemInfo SelectWeightedRandom(List<ShopItemInfo> items, float favoredWeight = 1.2f)
                {
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
        int restockCost = (int)RoundHalfUp(10 * totalCostPercentage);
        if (currency >= restockCost)
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
            var details = new ShopItemEffectDetails(id, shopItemInfo.title, shopItem.backgroundImage.color);
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
        if (currency <= 5)
        {
            availableShopItems.RemoveAll(s => s.id == 17); // remove Price Cut if you don't have enough currency
        }
        if (gameManager.criteriaText.GetCurrentCriteria().Any(c => c is NoComboLetters))
        {
            availableShopItems.RemoveAll(s => s.id == 1); // remove Shuffle 2x Points if it's that criteria level
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
            shopItemPrefabs[i].Initialize(shopItem.id, shopItem.title, shopItem.body, shopItem.warning, cost, currency, GetInteractable(shopItem.id), GetAdditionalInteractableCriteria(shopItem.id), IsActive(shopItem.id), GetExtraInfoText(shopItem.id), shopItem.iconSprite, (item) => BuyPressed(item), () => GetCoroutine(shopItem.id, cost));
        }

        int restockCost = (int)RoundHalfUp(10 * totalCostPercentage);
        bool canAffordReshuffle = currency >= restockCost;
        shuffleButton.interactable = canAffordReshuffle;
        var reshuffleText = shuffleButton.GetComponentInChildren<TextMeshProUGUI>();
        reshuffleText.text = $"Restock (<color={(canAffordReshuffle ? "green" : "red")}>${restockCost}</color>)";
        reshuffleText.color = new Color(reshuffleText.color.r, reshuffleText.color.g, reshuffleText.color.b, canAffordReshuffle ? 1 : 0.5f);
    }

    private IEnumerator GetCoroutine(int id, int cost)
    {
        switch (id)
        {
            case 0:
                return DoAction(id, cost, () => gameManager.ShowHint(), true, false);
            case 1:
                return DoAction(id, cost, () => gameManager.ShuffleComboLetters(), true, false);
            case 2:
                return DoAction(id, cost, () => gameManager.EnableMultiplier(), false, true);
            case 3:
                return DoAction(id, cost, () => gameManager.EnableEvenMultiplier(), false, true);
            case 4:
                return DoAction(id, cost, () => gameManager.EnableDoubleWealth(), false, true);
            case 5:
                return DoAction(id, cost, () => gameManager.DoDoubleTurn(), false, true);
            case 6:
                return DoAction(id, cost, () => gameManager.ResetWord(), true, false);
            case 7:
                return DoAction(id, cost, () => gameManager.EnableLongWordMultiplier(), false, true);
            case 8:
                return DoAction(id, cost, () => gameManager.UndoTurn(), true, false);
            case 9:
                return DoAction(id, cost, () => gameManager.EnableDoubleBluff(), false, true);
            case 10:
                return DoAction(id, cost, () => gameManager.EnableChanceMultiplier(), false, true);
            case 11:
                return DoAction(id, cost, () => gameManager.RestoreLife(true), true, false);
            case 12:
                return DoAction(id, cost, () => gameManager.RestoreLife(false), true, false);
            case 13:
                return DoAction(id, cost, () => gameManager.EnableMoneyLose(), false, true);
            case 14:
                return DoAction(id, cost, () => gameManager.LoseLifeMoney(), true, false);
            case 15:
                return DoAction(id, cost, () => gameManager.EnableOddMultiplier(), false, true);
            case 16:
                return DoAction(id, cost, () => gameManager.EnableDoubleEnded(), false, true);
            case 17:
                return DoAction(id, cost, () => ApplyDiscount(0.5f), false, true);
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
                return gameEnded ? 2 : (roundsWon + 1) * 2;
            case 4:
                return (int)RoundHalfUp(5 * multiplier);
            case 5:
                return (int)RoundHalfUp((substringLength + 1) * multiplier);
            case 6:
                return (gameManager.ResetWordUses + 1) * 4;
            case 7:
                return gameEnded ? 2 : (roundsWon + 1) * 2;
            case 8:
                return (int)RoundHalfUp((substringLength + 1) * 1.5f * multiplier);
            case 9:
                return (int)RoundHalfUp(5 * multiplier);
            case 10:
                return 5 + (int)RoundHalfUp(roundsWon * 1.5f);
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
                return gameManager.IsDoneRound();
            case 2:
                return !gameManager.HasBonusMultiplier;
            case 3:
                return !gameManager.HasEvenWordMultiplier;
            case 4:
                return !gameManager.HasDoubleWealth;
            case 5:
                return gameManager.IsPlayerTurn();
            case 6:
                return gameManager.IsPlayerTurn();
            case 7:
                return !gameManager.HasLongWordMultiplier;
            case 8:
                return gameManager.IsPlayerTurn();
            case 9:
                return !gameManager.HasDoubleBluff;
            case 10:
                return gameManager.ChanceMultiplier == 1;
            case 11:
                return gameManager.IsPlayerTurn();
            case 12:
                return gameManager.IsPlayerTurn();
            case 13:
                return !gameManager.HasLoseMoney;
            case 14:
                return gameManager.IsPlayerTurn();
            case 15:
                return !gameManager.HasOddWordMultiplier;
            case 16:
                return !gameManager.HasDoubleEndedMultiplier;
            case 17:
                return totalCostPercentage == 1;
        }

        return false;
    }

    private bool GetAdditionalInteractableCriteria(int id)
    {
        switch (id)
        {
            case 0:
                return true;
            case 1:
                return gameManager.comboText.gameObject.activeSelf;
            case 2:
                return true;
            case 3:
                return true;
            case 4:
                return true;
            case 5:
                return !gameManager.HasDoubleTurn;
            case 6:
                return gameManager.gameWord.Length > 0;
            case 7:
                return true;
            case 8:
                return gameManager.gameWord.Length > 0;
            case 9:
                return true;
            case 10:
                return true;
            case 11:
                return !gameManager.playerLivesText.HasFullLives();
            case 12:
                return !gameManager.aiLivesText.HasFullLives();
            case 13:
                return true;
            case 14:
                return gameManager.playerLivesText.LivesRemaining() > 1;
            case 15:
                return true;
            case 16:
                return true;
            case 17:
                return true;
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
            case 11:
                return false;
            case 12:
                return false;
            case 13:
                return gameManager.HasLoseMoney;
            case 14:
                return false;
            case 15:
                return gameManager.HasOddWordMultiplier;
            case 16:
                return gameManager.HasDoubleEndedMultiplier;
            case 17:
                return totalCostPercentage != 1;
        }

        return false;
    }

    private string GetExtraInfoText(int id)
    {
        switch (id)
        {
            case 10:
                if (gameManager.ChanceMultiplier < 1)
                {
                    return $"<color=red>{gameManager.ChanceMultiplier}x</color>";
                }
                return $"{gameManager.ChanceMultiplier}x";
        }

        return "";
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