using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Data;
using JetBrains.Annotations;
using UnityEngine.UIElements;
using Photon.Pun;
using Photon.Realtime;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public enum State
{
	MAIN = 0,
	LOBBY = 1,
	ROOM = 2,
	GAME = 3
}

public class PhotonInit : MonoBehaviourPunCallbacks
{
	public static PhotonInit Instance = null;

	private bool isReady = false;

	private bool isMain = false;
	private bool isLobby = false;
	private bool isWaitingRoom = false;
	private bool isInGame = false;

	[Header("툴팁 관련 프로퍼티")]
	public Canvas ToolTipCanvas;
	public TextEffect toolTipText;

	[Header("메인 로비 관련 프로퍼티")]
	public MainInit mainRoom;

	[Header("로비 관련 프로퍼티")]
	public LobbyInit lobbyRoom;
	public List<RoomInfo> rooms = new List<RoomInfo>();

	[Header("대기방 관련 프로퍼티")]
	public WaitingRoomInit waitingRoom;

	[Header("인게임 관련 프로퍼티")]
	public InGameManger InGameRoom;
	private Player myInfo;
	private bool isGameStart = false;

	void Awake()
    {
		if (Instance == null)
		{
			Instance = this;

			mainRoom = GameObject.Find("MainManager").GetComponent<MainInit>();

			ConnectToServer();

			DontDestroyOnLoad(gameObject);
			DontDestroyOnLoad(ToolTipCanvas);
		}
		else if (Instance != null)
			Destroy(gameObject);
	}

	private void Update()
    {
		if (!isMain && SceneManager.GetActiveScene().name == "MainLevel")
		{
			isMain = true;
			isLobby = false;
			isWaitingRoom = false;
			isInGame = false;
			if (mainRoom == null && GameObject.Find("MainManager") != null)
			{
				mainRoom = GameObject.Find("MainManager").GetComponent<MainInit>();
				mainRoom.SetUIInteractable(true);
			}
		}
		else if (!isLobby && SceneManager.GetActiveScene().name == "LobbyLevel")
		{
			isMain = false;
			isLobby = true;
			isWaitingRoom = false;
			isInGame = false;
			if (lobbyRoom == null && GameObject.Find("LobbyManager") != null)
			{
				lobbyRoom = GameObject.Find("LobbyManager").GetComponent<LobbyInit>();
				lobbyRoom.InitMainLobby(PhotonNetwork.LocalPlayer.NickName);
			}
		}
		else if (!isWaitingRoom && SceneManager.GetActiveScene().name == "WaitingLevel")
		{
			isMain = false;
			isLobby = false;
			isWaitingRoom = true;
			isInGame = false;
			if (waitingRoom == null && GameObject.Find("RoomManager") != null)
			{
				waitingRoom = GameObject.Find("RoomManager").GetComponent<WaitingRoomInit>();
			}
		}
		else if (!isInGame && (SceneManager.GetActiveScene().name == "Level1" || SceneManager.GetActiveScene().name == "Level2" || SceneManager.GetActiveScene().name == "Level3"))
		{
			isMain = false;
			isLobby = false;
			isWaitingRoom = false;
			isInGame = true;
			if (InGameRoom == null && GameObject.Find("InGameManager") != null)
			{
				InGameRoom = GameObject.Find("InGameManager").GetComponent<InGameManger>();
			}

			StartCoroutine(OperateGame());
		}

		NextScene();
	}

    public void ConnectToServer()
    {
		StartCoroutine(TryJoin(State.MAIN));
	}

	IEnumerator TryJoin(State state, int roomNum = -1)
	{
		switch(state)
		{
			case State.MAIN:
				PhotonNetwork.GameVersion = "NetworkProject Server 1.0";
				PhotonNetwork.ConnectUsingSettings();
				break;
			case State.LOBBY:
				PhotonNetwork.LoadLevel("LobbyLevel");
				PhotonNetwork.JoinLobby();
				break;
			case State.ROOM:
				if (roomNum != -1)
				{
					if(rooms[roomNum].PlayerCount >= 4)
                    {
						toolTipText.StartTextEffect("방이 꽉 찼습니다!", Effect.FADE);
						yield break;
                    }
					PhotonNetwork.JoinRoom(rooms[roomNum].Name);
				}
				break;
		}

		toolTipText.StartTextEffect(string.Format("{0}에 접속 중", 
			(state != State.LOBBY) && (state != State.ROOM) 
				? "서버" : (state != State.ROOM) 
					? "로비" : "방" ), 
						Effect.WAIT);

		switch (state)
		{
			case State.MAIN:
				while (PhotonNetwork.IsConnectedAndReady == false)
					yield return null;
				break;
			case State.LOBBY:
				while (PhotonNetwork.InLobby == false)
					yield return null;
				break;
			case State.ROOM:
				while (PhotonNetwork.InRoom == false)
					yield return null;
				break;
		}

		if(state != State.ROOM)
			toolTipText.StartTextEffect("접속 완료!", Effect.FADE);

		if (state == State.MAIN)
		{
			isReady = true;
			mainRoom.SetUIInteractable(true);
		}
	}

    public void ConnectToLobby()
    {
        if(PhotonNetwork.IsConnected == true)
        {
			StartCoroutine(TryJoin(State.LOBBY));
		}
		else
        {
			toolTipText.StartTextEffect("로비 진입에 실패하였습니다!\n서버 연결을 확인해주세요", Effect.FADE);
        }
    }

	public void LobbyConnected()
    {
		StartCoroutine(TryJoin(State.LOBBY));
	}

    //로비에 입장하였을 때 호출되는 콜백함수
    public override void OnJoinedLobby()
	{

	}

	public override void OnLeftLobby()
	{
		if (SceneManager.GetActiveScene().name == "LobbyLevel")
			PhotonNetwork.LoadLevel("MainLevel");

	}
	public override void OnLeftRoom()
	{
		if (SceneManager.GetActiveScene().name == "WaitingLevel")
		{
			Debug.Log("로비 나가기");
			PhotonNetwork.LoadLevel("LobbyLevel");
		}
	}

	//룸을 생성했을 떄의 콜백 함수
	public override void OnCreatedRoom()
	{
		Debug.Log("Finish make a Room");
	}

    public void SetPlayerName(string name)
    {
		if (!isReady)
			return;

		if (name == "")
		{
			toolTipText.StartTextEffect("이름을 정해주세요!", Effect.FADE);
			return;
		}

		PhotonNetwork.LocalPlayer.NickName = name;
		ConnectToLobby();

    }

	public void NextScene()
	{
        if(Input.GetKeyDown(KeyCode.F1))
        {
            PhotonNetwork.JoinOrCreateRoom("room1", new RoomOptions { MaxPlayers = 4 }, null);

			PhotonNetwork.LoadLevel("Level1");
		}
		else if (Input.GetKeyDown(KeyCode.F2))
		{
			PhotonNetwork.LoadLevel("Level2");
		}
		else if(Input.GetKeyDown(KeyCode.F3))
		{
			PhotonNetwork.LoadLevel("Level3");
		}
	}

	public override void OnJoinedRoom()
	{
		PhotonNetwork.LoadLevel("WaitingLevel");
	}

	IEnumerator OperateGame()
    {
		while(InGameRoom == null)
        {
			yield return new WaitForSeconds(0.2f);
        }

		isGameStart = true;
		InGameRoom.GameStart(myInfo);
		InGameRoom.StartCoroutine(InGameRoom.CreatePlayer(myInfo));
    }

	public bool CreateRoom(string roomName, string pw, bool isPassword)
    {
		if(rooms.Count == 6)
        {
			toolTipText.StartTextEffect("서버에 방이 꽉 찼습니다!", Effect.FADE);
			return false;
        }

		RoomOptions myRoomOptions = new RoomOptions();
		myRoomOptions.MaxPlayers = 4;

		string myRoomName = string.Empty;
		if (isPassword)
		{
			myRoomName = roomName == "" ? "[P]GameRoom" + rooms.Count + 1 : "[P]" + roomName;
			myRoomOptions.CustomRoomProperties = new Hashtable()
			{
				{ "password", pw }
			};
			myRoomOptions.CustomRoomPropertiesForLobby = new string[] { "password" };
		}
		else
		{
			myRoomName = roomName == "" ? "GameRoom" + rooms.Count + 1 : roomName;
		}

		StartCoroutine(TryJoin(State.ROOM));
		PhotonNetwork.CreateRoom(myRoomName, myRoomOptions);
		return true;
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
		int roomCount = roomList.Count;
		for(int i = 0; i < roomCount; i++)
        {
			if (!roomList[i].RemovedFromList)
			{
				if (!rooms.Contains(roomList[i])) rooms.Add(roomList[i]);
				else rooms[rooms.IndexOf(roomList[i])] = roomList[i];
			}
			else if (rooms.IndexOf(roomList[i]) != -1) rooms.RemoveAt(rooms.IndexOf(roomList[i]));
        }

		lobbyRoom.RoomListRenewal(rooms);
    }

	public void EnterRoom(int roomNum, bool b_password, string password = "")
    {
		if(!b_password)
        {
			if (rooms[roomNum].CustomProperties["password"] == null)
			{
				StartCoroutine(TryJoin(State.ROOM, roomNum));
				lobbyRoom.RoomListRenewal(rooms);
			}
			else
				lobbyRoom.ShowPasswordPanel(true);
        }
		else
        {
			if ((string)rooms[roomNum].CustomProperties["password"] == password)
			{
				StartCoroutine(TryJoin(State.ROOM, roomNum));
				lobbyRoom.RoomListRenewal(rooms);
				lobbyRoom.ShowPasswordPanel(false);
			}
			else
				toolTipText.StartTextEffect("비밀번호가 틀렸습니다!", Effect.FADE);
        }
    }

	public void SetPlayerForGame(Player player) { myInfo = player; }
	public void ResetPlayerInfo() { myInfo = null; }
	public bool GetIsGameStart() { return isGameStart; }
	public void SetIsGameStart(bool b_GameStart) { isGameStart = b_GameStart; } 
	public void SetPlayerPropertyState(string property, bool state)
	{
		Hashtable tempProperties = myInfo.CustomProperties;
		tempProperties[property] = state;
		myInfo.SetCustomProperties(tempProperties);
	}
}
