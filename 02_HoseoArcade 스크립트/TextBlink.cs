using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TextBlink : MonoBehaviour
{ 
    Text t_blinkText;
    public float blinkSecond = 0.5f;
	public float IsOnSecond = 0.5f;
	// Start is called before the first frame update
	void Start()
    {
        t_blinkText = GetComponent<Text>();
        StartCoroutine(BlinkText());
    }

    // Update is called once per frame
    public IEnumerator BlinkText()
    {
        while (true)
        {
            t_blinkText.text = "";
            yield return new WaitForSeconds(blinkSecond);
			t_blinkText.text = "INSERT COIN...";
			yield return new WaitForSeconds(IsOnSecond);
		}
    }
}
