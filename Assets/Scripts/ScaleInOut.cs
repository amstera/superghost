using UnityEngine;

public class ScaleInOut : MonoBehaviour
{
    public float minScale = 0.75f;
    public float maxScale = 1.25f;
    public float speed = 1.0f;

    private Vector3 originalScale;
    private float currentScaleFactor = 0.0f;
    private bool scalingUp = true;

    void Start()
    {
        originalScale = transform.localScale;
    }

    void Update()
    {
        // Determine the scale factor based on the direction of scaling
        if (scalingUp)
        {
            currentScaleFactor += speed * Time.deltaTime;
        }
        else
        {
            currentScaleFactor -= speed * Time.deltaTime;
        }

        // Apply the scale factor within the min and max bounds
        transform.localScale = originalScale * Mathf.Lerp(minScale, maxScale, currentScaleFactor);

        // Reverse the direction if limits are reached
        if (currentScaleFactor > 1.0f)
        {
            scalingUp = false;
            currentScaleFactor = 1.0f;
        }
        else if (currentScaleFactor < 0.0f)
        {
            scalingUp = true;
            currentScaleFactor = 0.0f;
        }
    }
}