using UnityEngine;
using TMPro;
using System.Collections;

public class SlamEffect : MonoBehaviour
{
    public float startScale = 2f;
    public float endScale = 1.0f;
    public float duration = 0.25f;
    public float echoDelay = 0.05f;
    public float echoDuration = 0.5f;

    private TextMeshProUGUI textMesh;
    private TextMeshProUGUI echoTextMesh;
    private Vector3 originalScale;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        originalScale = textMesh.transform.localScale;

        // Create echo text mesh
        GameObject echoGameObject = new GameObject("EchoText");
        echoGameObject.transform.SetParent(transform);
        echoGameObject.transform.localPosition = Vector3.zero;
        echoGameObject.transform.localScale = Vector3.one;

        echoTextMesh = echoGameObject.AddComponent<TextMeshProUGUI>();
    }

    void OnEnable()
    {
        textMesh.transform.localScale = originalScale * startScale; // Set initial scale
        ConfigureEchoText();
        echoTextMesh.gameObject.SetActive(true);
        StartCoroutine(Slam());
        StartCoroutine(Echo());
    }

    void OnDisable()
    {
        StopAllCoroutines();

        // Reset the main text to its original scale
        textMesh.transform.localScale = originalScale;

        // Reset the echo text
        echoTextMesh.transform.localScale = originalScale;
        echoTextMesh.gameObject.SetActive(false); // Ensure it's ready for next enable
    }

    IEnumerator Slam()
    {
        float currentTime = 0f;
        while (currentTime < duration)
        {
            float scale = Mathf.Lerp(startScale, endScale, currentTime / duration);
            textMesh.transform.localScale = originalScale * scale;
            currentTime += Time.deltaTime;
            yield return null;
        }
        textMesh.transform.localScale = originalScale; // Ensure it ends at the correct scale
    }

    IEnumerator Echo()
    {
        yield return new WaitForSeconds(echoDelay); // Wait for the main slam to initiate

        float currentTime = 0f;
        while (currentTime < echoDuration)
        {
            float scale = Mathf.Lerp(startScale, endScale, currentTime / echoDuration);
            echoTextMesh.transform.localScale = originalScale * scale;
            echoTextMesh.color = new Color(echoTextMesh.color.r, echoTextMesh.color.g, echoTextMesh.color.b, Mathf.Lerp(0.5f, 0.0f, currentTime / echoDuration));
            currentTime += Time.deltaTime;
            yield return null;
        }

        echoTextMesh.gameObject.SetActive(false); // Disable echo text after animation
    }

    private void ConfigureEchoText()
    {
        echoTextMesh.text = textMesh.text;
        echoTextMesh.font = textMesh.font;
        echoTextMesh.fontSize = textMesh.fontSize;
        echoTextMesh.color = new Color(textMesh.color.r, textMesh.color.g, textMesh.color.b, 0.5f); // Semi-transparent
        echoTextMesh.alignment = textMesh.alignment;
    }
}