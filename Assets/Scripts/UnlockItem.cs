using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UnlockItem : MonoBehaviour, IPointerClickHandler
{
    public HatType hatType;
    public Image displayImage;
    public Image background;
    public Image outline;
    public TextMeshProUGUI title;
    public TextMeshProUGUI description;
    public Image lockImage;
    public Image checkmarkImage;
    public GameObject newIndicator;
    public StatsPopup statsPopup;

    private bool _unlocked;
    public bool Unlocked
    {
        get { return _unlocked; }
        set
        {
            _unlocked = value;
            UpdateDisplay();
        }
    }

    private bool _enabled;
    public bool Enabled
    {
        get { return _enabled; }
        set
        {
            _enabled = value;
            UpdateDisplay();
        }
    }

    private bool newlyUnlocked;
    private string originalTitleText;

    public void Init(HatType type, bool unlocked, bool newlyUnlocked, bool enabled, Sprite displaySprite, string titleText, string descriptionText, float ratio)
    {
        _unlocked = unlocked;
        _enabled = enabled;
        this.newlyUnlocked = newlyUnlocked;

        hatType = type;
        if (type != HatType.None)
        {
            displayImage.sprite = displaySprite;
        }

        float maxSize = 85f;
        float width, height;
        if (ratio > 1) // Wider than tall
        {
            width = maxSize;
            height = maxSize / ratio;
        }
        else // Taller than wide or equal
        {
            height = maxSize;
            width = maxSize * ratio;
        }
        displayImage.rectTransform.sizeDelta = new Vector2(width, height);

        originalTitleText = titleText;
        title.text = titleText;
        description.text = descriptionText;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (!Unlocked)
        {
            lockImage.gameObject.SetActive(true);
            checkmarkImage.gameObject.SetActive(false);
            displayImage.color = Color.black;
            title.text = "???";
            background.color = new Color32(79, 79, 79, 255); // #626362
            outline.color = Color.white;
        }
        else
        {
            lockImage.gameObject.SetActive(false);
            displayImage.color = Color.white;
            title.text = originalTitleText;
            newIndicator.SetActive(newlyUnlocked && !Enabled && hatType != HatType.None);

            if (Enabled)
            {
                checkmarkImage.gameObject.SetActive(true);
                outline.color = Color.yellow;
                background.color = new Color32(17, 115, 42, 255); // #11732A
            }
            else
            {
                checkmarkImage.gameObject.SetActive(false);
                outline.color = Color.white;
                background.color = new Color32(47, 94, 46, 255); // #2F5E2E
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Unlocked && !Enabled)
        {
            Enabled = true;
            UpdateDisplay();

            // Notify parent to update other unlock items
            statsPopup.OnUnlockItemClicked(this);
        }
    }
}
