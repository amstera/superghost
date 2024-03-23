using UnityEngine;
using System.Collections;

public class ShineEffect : MonoBehaviour
{
    public float startX = -55f;
    public float endX = 55f;
    public float moveDuration = 3f;
    public float waitTime = 0f;

    private Coroutine moveShineCoroutine;

    void OnEnable()
    {
        moveShineCoroutine = StartCoroutine(MoveShine());
    }

    void OnDisable()
    {
        if (moveShineCoroutine != null)
        {
            StopCoroutine(moveShineCoroutine);
        }

        transform.localPosition = new Vector3(startX, transform.localPosition.y, transform.localPosition.z);
    }

    IEnumerator MoveShine()
    {
        while (true)
        {
            float elapsedTime = 0f;
            while (elapsedTime < moveDuration)
            {
                float t = elapsedTime / moveDuration;
                float newX = Mathf.SmoothStep(startX, endX, t);
                transform.localPosition = new Vector3(newX, transform.localPosition.y, transform.localPosition.z);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Reset to start position
            transform.localPosition = new Vector3(startX, transform.localPosition.y, transform.localPosition.z);

            yield return new WaitForSeconds(waitTime);
        }
    }
}