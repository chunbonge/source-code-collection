using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
enum ItemNumber
{
    FEATHER = 0,
    BACKPACK,
    SCROLL
}

public class CreateRandomItem : MonoBehaviourPunCallbacks
{
    public static int[] itemCount = new int[3];

    public GameObject[] objectsToSpawn; // 생성할 오브젝트 배열
    private bool createItem = false;

    private int spawnObject;
    private SpawnController itemSpawnInfo;

    [Header("개발자 전용")]
    [Tooltip ("체크 시 아이템 박스에서 무조건 아이템이 나오도록 보장해줍니다")]
    [SerializeField] bool isAlwaysSpawnObject = false;


    private PhotonView pv;

    private void Start()
    {
        //RandIteam();
        spawnObject = -1;
        pv = GetComponent<PhotonView>();
    }

    public void InitRandItem()
    {
        for (int i = 0; i < itemCount.Length; i++)
        {
            itemCount[i] = 0;
        }
    }

    public void RandIteam()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            itemSpawnInfo = GameObject.Find("ItemSpawnController").GetComponent<SpawnController>();
            if (Random.Range(1f, 10f) <= (itemSpawnInfo.spawnRate * 10f) || isAlwaysSpawnObject)
            {
                SetRandomSpawnObject();
            }
        }
        
    }

    [PunRPC]
    public void OthersSpawnObject(int newSpawnObject)
    {
        spawnObject = newSpawnObject;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "BombStream" && !createItem)
        {
            createItem = true;
            Destroy(gameObject);
        }
    }

    void SetRandomSpawnObject()
    {
        if(itemCount[(int)ItemNumber.FEATHER] == itemSpawnInfo.maxFeathers && 
            itemCount[(int)ItemNumber.BACKPACK] == itemSpawnInfo.maxBackPacks && 
            itemCount[(int)ItemNumber.SCROLL] == itemSpawnInfo.maxScrolls)
        {
            return;
        }

        int randomItemIndex = Random.Range(0, objectsToSpawn.Length); // 랜덤 인덱스 생성
        itemCount[randomItemIndex]++;

        if (itemCount[(int)ItemNumber.FEATHER] > itemSpawnInfo.maxFeathers)
        {
            itemCount[(int)ItemNumber.FEATHER]--;
            SetRandomSpawnObject();
            return;
        }

        if (itemCount[(int)ItemNumber.BACKPACK] > itemSpawnInfo.maxBackPacks)
        {
            itemCount[(int)ItemNumber.BACKPACK]--;
            SetRandomSpawnObject();
            return;
        }

        if (itemCount[(int)ItemNumber.SCROLL] > itemSpawnInfo.maxScrolls)
        {
            itemCount[(int)ItemNumber.SCROLL]--;
            SetRandomSpawnObject();
            return;
        }

        spawnObject = randomItemIndex;
    }

    public void SpawnRandomObject()
    {
        if (spawnObject != -1)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.InstantiateRoomObject(objectsToSpawn[spawnObject].name,
                    transform.position, Quaternion.identity); // 랜덤 오브젝트 생성
            }
            createItem = true;
        }
        Destroy(gameObject);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            pv.RPC("OthersSpawnObject", RpcTarget.Others, spawnObject);
        }
    }
} 
