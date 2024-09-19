using System.Collections;
using TMPro;
using UnityEngine;

public class TypingEffect : MonoBehaviour
{
    public float TypingSpeed = 0.045f;
    public bool PlayOnEnable = true;
    public float StartDelay = 0.0f;

    private string fullText;

    private TextMeshProUGUI text;
    private Coroutine _typingCoroutine;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        fullText = text.text;
    }

    private void OnEnable()
    {
        if (PlayOnEnable && text != null)
        {
            StartCoroutine(StartTypingWithDelay());
        }
    }

    private IEnumerator StartTypingWithDelay()
    {
        text.text = string.Empty;

        yield return new WaitForSeconds(StartDelay); 
        StartTyping(fullText);
    }

    public void StartTyping(string fullText)
    {
        if (text != null)
        {
            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);
            }

            if (TypingSpeed == 0)
            {
                text.text = fullText;
                return;
            }

            _typingCoroutine = StartCoroutine(TypeText(fullText));
        }
    }

    private IEnumerator TypeText(string fullText)
    {
        int length = fullText.Length;
        int index = 0;

        while (index < length)
        {
            if (fullText[index] == '<')
            {
                int endTagIndex = fullText.IndexOf('>', index);
                if (endTagIndex != -1)
                {
                    string tag = fullText.Substring(index, endTagIndex - index + 1);
                    text.text += tag;
                    index = endTagIndex + 1;
                }
                else
                {
                    text.text += fullText[index];
                    index++;
                }
            }
            else if (fullText[index] == '\\' && index + 1 < length && fullText[index + 1] == 'n')
            {
                text.text += '\n';
                index += 2;
            }
            else
            {
                text.text += fullText[index];
                index++;
            }

            yield return new WaitForSeconds(TypingSpeed);
        }
    }
}