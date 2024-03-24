using UnityEngine;

public class BobbingEffect : MonoBehaviour
{
    public float bobbingSpeed = 2f;
    public float bobbingHeight = 0.5f;

    private float originalY;

    void Start()
    {
        originalY = transform.position.y;
    }

    void Update()
    {
        // Calculate the new Y position
        float newY = originalY + Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}