using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Linq;
using Random = UnityEngine.Random;
using UnityEditor;

public class InGameManger : MonoBehaviourPun
{
	// 게임 시작 시의 로직과 관련
	private Player winner;
	private int count;

	[Header("플레이어 생성 위치 할당")]
	[Tooltip("플레이어 생성 위치 리스트")] public  GameObject[] playerSpawnLocation = new GameObject[4];

	[Header("플레이어 생성 관련")]
	[Tooltip("플레이어 프리팹")] public GameObject[] PlayerPrefabs = new GameObject[4];

	//대기방 전환을 위한 시간 변수 포함
	[Header("승리 플레이어 렌더링 관련")]
	[Tooltip("게임 클리어를 띄울 winner UI캔버스")] public GameObject winnerCanvas;
    [Tooltip("게임 클리어 시 winner UI 로딩까지의 시간")] public float showWinnerUIInvokeTime;
	[Tooltip("승리자 이름 텍스트")] public Text winnerName;
	[Tooltip("승리 이미지를 띄울 위치")] public RectTransform winnerImgPos;
	[Tooltip("승리자 이미지 관련")] public RawImage[] winnerImgs = new RawImage[4];
	[Tooltip("대기방으로 화면 넘어가는 대기 시간")] public float WaitTime;
	[Tooltip("n 초 뒤에 대기방으로 이동합니다 텍스트")] public Text BackToWatingSecond; //시간 문구

	[SerializeField]
	private AudioClip finishSound;
	[SerializeField]
	private AudioClip LevelSound;

	private void Start()
    {
		winner = null;
		count = 0;		
	}

	public void IsGameClear()
    {
		winnerCanvas.SetActive(true);
		SoundManager.Instance.StopBGM();
		SoundManager.Instance.PlayEffectOneShot(finishSound);
		RawImage image = Instantiate(winnerImgs[(int)winner.CustomProperties["characterIndex"]], winnerImgPos);
		image.rectTransform.sizeDelta = new Vector2(165f, 165f);
		winnerName.text = winner.NickName;
		StartCoroutine(BackToWaitingRoom(WaitTime));
    }

	IEnumerator BackToWaitingRoom(float time)
    {
		float waitTime = time;
		
		while(waitTime >= 0f)
        {
			SetWatingSecondText((int)waitTime);
			waitTime -= 1f;
			yield return new WaitForSeconds(1f);
		}

		SceneManager.LoadScene("WaitingLevel");
    }

    public void SetWatingSecondText(int time)
    {
		BackToWatingSecond.text = time + " 초 뒤에 대기방으로 이동합니다...";
	}

	public IEnumerator CreatePlayer(Player myInfo)
	{
		Vector3 spawnPosition = playerSpawnLocation[(int)myInfo.CustomProperties["waitingIndex"]].transform.position;
		PhotonNetwork.Instantiate(PlayerPrefabs[(int)myInfo.CustomProperties["characterIndex"]].name, 
			spawnPosition, Quaternion.identity, 0);

		yield return null;
	}

    public void GameStart(Player myInfo)
	{
		SoundManager.Instance.PlayBGM(LevelSound);

		StartCoroutine(CheckWinner());

        if (PhotonNetwork.IsMasterClient)
		{
            CreateRandomItem[] creatRand;
            creatRand = GameObject.FindObjectsOfType<CreateRandomItem>();
            for (int i = 0; i < creatRand.Length; i++)
            {
				creatRand[i].InitRandItem();
                creatRand[i].RandIteam();
            }
        }
            
    }

	IEnumerator CheckWinner()
    {
		while (true)
		{
			count = CheckPlayerAlive();
			if (count <= 1) break;
			yield return null;
		}

		if(count == 0)
        {
			Debug.Log("플레이어가 다 뒤졌습니다 다시 시작해라");
        }
		else
        {
			foreach(Player player in PhotonNetwork.PlayerList)
            {
				if((bool)player.CustomProperties["isDie"] == false)
                {
					winner = player;
				}
            }
			Invoke("IsGameClear", showWinnerUIInvokeTime);
		}
	}

	public int CheckPlayerAlive()
	{
		int aliveCount = PhotonNetwork.PlayerList.Length;
		foreach (Player player in PhotonNetwork.PlayerList)
		{
			if ((bool)player.CustomProperties["isDie"])
			{
				aliveCount -= 1;
			}
		}
		return aliveCount;
	}
} 
