using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Effect
{
    FADE = 0,
    WAIT
}

public class TextEffect : MonoBehaviour
{

    [Header("페이드 이펙트 관련 프로퍼티")]
    [SerializeField] private Text myText;
    [SerializeField] private float playTime = 1f;

    public void StartTextEffect(string text, Effect effect)
    {
        StopAllCoroutines();
        myText.text = text;

        if (effect == Effect.FADE)
            StartCoroutine("Fade");
        else if (effect == Effect.WAIT)
            StartCoroutine("Wait");
    }

    public void SetInvisible()
    {
        Color fadeColor = myText.color;
        fadeColor.a = 0f;
        myText.color = fadeColor;
    }

    IEnumerator Fade()
    {
        float time = 0f;
        Color startColor = myText.color;
        Color fadeColor = myText.color;
        startColor.a = 1f;
        fadeColor.a = 0f;

        while (time <= playTime)
        {
            myText.color = Color.Lerp(startColor, fadeColor, time / playTime);
            yield return null;
            time += Time.deltaTime;
        }
        myText.color = fadeColor;
    }

    IEnumerator Wait()
    {
        Color startColor = myText.color;
        Color fadeColor = myText.color;
        startColor.a = 1f;
        fadeColor.a = 0f;

        myText.color = startColor;
        yield return new WaitForSeconds(0.1f);
        myText.text += '.';

        yield return new WaitForSeconds(0.1f);
        myText.text += '.';

        yield return new WaitForSeconds(0.1f);
        myText.text += '.';
    }
}
