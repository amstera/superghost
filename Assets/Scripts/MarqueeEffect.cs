using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI.ProceduralImage;

public class MarqueeEffect : MonoBehaviour
{
    public GameObject circles;
    private List<ProceduralImage> proceduralImages;
    private float highlightDuration = 0.4f;
    private float delayBetween = 0.1f;

    private void Awake()
    {
        proceduralImages = circles.GetComponentsInChildren<ProceduralImage>().ToList();
        proceduralImages.Remove(GetComponent<ProceduralImage>()); // exclude parent
    }

    private void OnEnable()
    {
        StartCoroutine(RunMarqueeEffect());
    }

    private IEnumerator RunMarqueeEffect()
    {
        while (true)
        {
            foreach (ProceduralImage image in proceduralImages)
            {
                StartCoroutine(AnimateImage(image));
                yield return new WaitForSeconds(delayBetween);
            }
        }
    }

    private IEnumerator AnimateImage(ProceduralImage image)
    {
        float elapsedTime = 0;
        Color startColor = Color.white;
        Color endColor = new Color32(109, 147, 255, 255);

        // Lerp to red
        while (elapsedTime < highlightDuration / 2)
        {
            image.color = Color.Lerp(startColor, endColor, elapsedTime / (highlightDuration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        image.color = endColor;

        elapsedTime = 0;

        // Lerp back to white
        while (elapsedTime < highlightDuration / 2)
        {
            image.color = Color.Lerp(endColor, startColor, elapsedTime / (highlightDuration / 2));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        image.color = startColor;
    }
}
