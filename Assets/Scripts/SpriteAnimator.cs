using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] sprites; // Array of sprites to cycle through
    public float imageDuration = 1f; // Time in seconds to display each image

    private Image imageComponent; // The Image component to change the sprites on
    private int currentSpriteIndex = 0; // Current index of the sprite array
    private float timer; // Timer to track when to change to the next sprite

    void Start()
    {
        imageComponent = GetComponent<Image>(); // Get the Image component
        if (sprites.Length > 0)
        {
            imageComponent.sprite = sprites[currentSpriteIndex]; // Set the initial sprite
        }
    }

    void Update()
    {
        if (sprites.Length == 0) return; // If there are no sprites, do nothing

        timer += Time.deltaTime; // Increment timer by the time passed since last frame

        if (timer >= imageDuration)
        {
            timer = 0f; // Reset timer
            currentSpriteIndex++; // Move to the next sprite
            if (currentSpriteIndex >= sprites.Length) // If we're past the last sprite...
            {
                currentSpriteIndex = 0; // ...loop back to the first sprite
            }
            imageComponent.sprite = sprites[currentSpriteIndex]; // Update the sprite
        }
    }
}