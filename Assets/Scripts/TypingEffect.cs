using System.Collections;
using TMPro;
using UnityEngine;

public class TypingEffect : MonoBehaviour
{
    public float TypingSpeed = 0.045f;
    public bool PlayOnEnable = true;

    private TextMeshProUGUI text;

    private Coroutine _typingCoroutine;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (PlayOnEnable && text != null)
        {
            StartTyping();
        }
    }

    public void StartTyping()
    {
        if (text != null)
        {
            string fullText = text.text;
            text.text = string.Empty;

            if (_typingCoroutine != null)
            {
                StopCoroutine(_typingCoroutine);    
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