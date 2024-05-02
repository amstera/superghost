using UnityEngine;

public class RotateEffect : MonoBehaviour
{
    public float rotationSpeed = 1;
    public float rotationAmount = 20;

    void Update()
    {
        float rotationY = Mathf.Sin(Time.time * rotationSpeed) * rotationAmount; 
        transform.localRotation = Quaternion.Euler(0, rotationY, 0);
    }
}
