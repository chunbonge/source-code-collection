using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TextAnimation : MonoBehaviour
{
	// 텍스트가 이동할 목표 위치
	public Transform TargetTransform;
	private Vector3 targetPosition; 

	private Vector3 startPosition;
	private Text textTransform;

	//텍스트 애니메이션을 위한 변수
	public float startTime;
	public float maxTime;
	private float currentTime;
	private float s_StartTime;

	private void Start()
	{
		//시작 위치 설정
		textTransform = GetComponent<Text>();
		startPosition = textTransform.transform.position;
		//목표 위치 설정
		targetPosition = TargetTransform.position;
		s_StartTime = startTime;

		StartCoroutine(TextSlideAnimation());
	}
	 
	IEnumerator TextSlideAnimation()
	{
		Vector3 newVec = Vector3.zero;
		currentTime = 0f;

		while (currentTime <= maxTime)
		{
			newVec = Vector3.Lerp(startPosition, targetPosition, (startTime - s_StartTime) /(maxTime - s_StartTime));
			currentTime += Time.deltaTime;

			if(currentTime >= startTime)
			{
				startTime += Time.deltaTime;
			}
			 
			textTransform.transform.position = newVec;
			yield return null;
		}
	}

}
