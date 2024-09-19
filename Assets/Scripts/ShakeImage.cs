using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShakeImage : MonoBehaviour
{
    public Image targetImage; // Image to apply shake effect on
    public float delay = 0f;  // Delay before shaking
    public float shakeDuration = 0.25f;  // Duration of the shake
    public float shakeMagnitude = 3f;    // Magnitude of the shake

    void OnEnable()
    {
        // Store the original position of the image
        if (targetImage != null)
        {
            StartShake();
        }
    }

    public void StartShake()
    {
        if (targetImage != null)
        {
            StartCoroutine(Shake());
        }
    }

    private IEnumerator Shake()
    {
        // Wait for the specified delay before starting the shake
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        float elapsed = 0.0f;
        Vector3 originalPos = targetImage.transform.localPosition;

        while (elapsed < shakeDuration)
        {
            // Generate a new random position within the shake magnitude range
            float x = originalPos.x + Random.Range(-1f, 1f) * shakeMagnitude;
            float y = originalPos.y + Random.Range(-1f, 1f) * shakeMagnitude;

            // Apply the new position
            targetImage.transform.localPosition = new Vector3(x, y, originalPos.z);

            // Increment elapsed time
            elapsed += Time.deltaTime;

            yield return null; // Wait for next frame
        }

        // Reset the image to its original position
        targetImage.transform.localPosition = originalPos;
    }
}