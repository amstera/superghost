using UnityEngine;

public class BobbingEffect : MonoBehaviour
{
    public float bobbingSpeed = 2f;
    public float bobbingHeight = 0.5f;

    private float originalY;

    void Start()
    {
        originalY = transform.localPosition.y;
    }

    void Update()
    {
        // Calculate the new Y position
        float newY = originalY + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        transform.localPosition = new Vector3(transform.localPosition.x, newY, transform.localPosition.z);
    }
}