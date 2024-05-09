using UnityEngine;
using UnityEngine.UI;

public class ShinyLightAnimation : MonoBehaviour
{
    public float minIntensity = 0.5f;
    public float maxIntensity = 1f;
    public float pulseSpeed = 1f;
    public float waveAmplitude = 10f;
    public float waveFrequency = 1f;

    private Image image;
    private Vector3 initialPosition;
    private Vector3 initialScale;

    private void Start()
    {
        image = GetComponent<Image>();
        initialPosition = image.rectTransform.anchoredPosition;
        initialScale = image.rectTransform.localScale;
    }

    private void Update()
    {
        // Pulsate the light intensity
        float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        image.color = new Color(image.color.r, image.color.g, image.color.b, intensity);

        // Calculate the wave effect
        float waveEffect = Mathf.Sin(Time.time * waveFrequency) * waveAmplitude;

        // Apply the wave effect to the position and scale
        Vector3 newPosition = initialPosition + new Vector3(waveEffect, 0f, 0f);
        Vector3 newScale = initialScale + new Vector3(waveEffect * 0.1f, waveEffect * 0.1f, 0f);

        // Update the position and scale of the light
        image.rectTransform.anchoredPosition = newPosition;
        image.rectTransform.localScale = newScale;
    }
}
