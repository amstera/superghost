using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ColorCycleEffect : MonoBehaviour
{
    public Color defaultColor = new Color32(112, 255, 0, 255); // 70FF00
    private TextMeshProUGUI textMesh;
    private float hue;
    private float saturation;
    private float brightness;

    void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        if (textMesh == null)
        {
            Debug.LogError("TextMeshProUGUI component not found on the GameObject.");
            return;
        }

        // Convert default color to HSV
        Color.RGBToHSV(defaultColor, out hue, out saturation, out brightness);
        textMesh.color = defaultColor; // Set initial color

        var textWiggle = GetComponent<TextWiggleEffect>();
        if (textWiggle != null)
        {
            StartCoroutine(ToggleTextWiggle(textWiggle));
        }
    }

    void Update()
    {
        // Rotate hue over time
        hue += Time.deltaTime * 0.2f; // Adjust rotation speed here
        hue = hue % 1.0f; // Wrap hue around the 0-1 range

        // Convert back to RGB and apply the new color
        textMesh.color = Color.HSVToRGB(hue, saturation, brightness);
    }

    void OnEnable()
    {
        textMesh = textMesh == null ? GetComponent<TextMeshProUGUI>() : textMesh;
        textMesh.color = defaultColor;
        Color.RGBToHSV(defaultColor, out hue, out saturation, out brightness);
    }

    void OnDisable()
    {
        textMesh.color = defaultColor;
    }

    private IEnumerator ToggleTextWiggle(TextWiggleEffect textWiggle)
    {
        textWiggle.enabled = false;

        yield return new WaitForEndOfFrame();

        textWiggle.enabled = true;
    }
}
