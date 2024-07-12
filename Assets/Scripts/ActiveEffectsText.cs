using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI.ProceduralImage;

public class ActiveEffectsText : MonoBehaviour, IPointerClickHandler
{
    public TextMeshProUGUI text;
    public TextMeshProUGUI textOverlay;
    public ProceduralImage background, outline;
    public GameObject deleteButton;
    public EffectsPopUp effectsPopUp;

    private List<ShopItemEffectDetails> activeEffects = new List<ShopItemEffectDetails>();

    public void AddEffect(ShopItemEffectDetails shopItem)
    {
        activeEffects.Add(shopItem);
        UpdateDisplay();
    }

    public List<ShopItemEffectDetails> GetEffects()
    {
        return activeEffects;
    }

    public void RemoveEffect(int id)
    {
        activeEffects.RemoveAll(e => e.id == id);
        UpdateDisplay();
    }

    public void ClearAll()
    {
        activeEffects.Clear();
        UpdateDisplay();
    }

    public void MatchEffects(ActiveEffectsText aet)
    {
        activeEffects.Clear();
        foreach (var effect in aet.GetEffects())
        {
            activeEffects.Add(effect);
        }

        UpdateDisplay();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(textOverlay, eventData.position, Camera.main);
        if (linkIndex != -1)
        {
            // Get the link info and parse the link id, which is the effect id
            TMP_LinkInfo linkInfo = textOverlay.textInfo.linkInfo[linkIndex];
            int effectId = int.Parse(linkInfo.GetLinkID());

            // Find and log the effect based on the effectId
            var clickedEffect = activeEffects.FirstOrDefault(effect => effect.id == effectId);
            if (clickedEffect != null)
            {
                Vector2 localPos;
                RectTransform textOverlayRect = textOverlay.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(textOverlayRect, eventData.position, eventData.pressEventCamera, out localPos);
                localPos.x -= 20f;
                localPos.y -= 105;

                GetColor(clickedEffect.color, out Color brighterColor);
                effectsPopUp.Show(clickedEffect.title, clickedEffect.description, localPos, brighterColor);
            }
        }
        else
        {
            effectsPopUp.Hide();
        }
    }

    private void UpdateDisplay()
    {
        text.text = "";
        textOverlay.text = "";

        foreach (var effect in activeEffects)
        {
            var shortTitle = GetShortTitle(effect.title);
            var color = GetColor(effect.color, out _);

            text.text += $"<mark=#{color} padding=15,15,15,15>{shortTitle}</mark>  ";
            textOverlay.text += $"<link=\"{effect.id}\">{shortTitle}</link>  ";
        }

        text.text.Trim();
        textOverlay.text.Trim();

        deleteButton.SetActive(activeEffects.Count > 0);
        outline.gameObject.SetActive(activeEffects.Count > 0);
        outline.color = activeEffects.Count >= 5 ? new Color32(80, 0, 0, 240) : new Color32(54, 54, 54, 240);
        background.color = new Color(background.color.r, background.color.g, background.color.b, activeEffects.Count > 0 ? 135f/255 : 0);
    }

    private string GetShortTitle(string title)
    {
        // Split the title into words
        var words = title.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // Take the first letter or digit of each word
        var shortTitle = string.Concat(words.Select(w =>
            char.IsDigit(w[0]) ? w.Substring(0, 1) : w.Substring(0, 1).ToUpper()));

        return shortTitle;
    }

    private string GetColor(Color color, out Color brighterColor)
    {
        Color.RGBToHSV(color, out float H, out float S, out float V);
        V = Mathf.Clamp(V + 0.25f, 0, 1);
        brighterColor = Color.HSVToRGB(H, S, V);

        return ColorUtility.ToHtmlStringRGB(brighterColor);
    }
}

public class ShopItemEffectDetails
{
    public ShopItemEffectDetails(int id, string title, string description, Color color, int cost)
    {
        this.id = id;
        this.title = title;
        this.description = description;
        this.color = color;
        this.cost = cost;
    }

    public int id;
    public string title;
    public string description;
    public int cost;
    public Color color;
}
