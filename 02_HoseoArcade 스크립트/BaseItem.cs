using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public abstract class BaseItem : MonoBehaviourPun
{
    public abstract void OperateItemLogic(PlayerController player);

    PhotonView pv;

	private void Start()
    {
        pv = gameObject.GetComponent<PhotonView>();
	}
    void OnTriggerStay2D(Collider2D other) 
    {
        if (other.gameObject.tag == "Player")
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                Debug.Log("∏‘¿Ω");
                if(player.photonView.IsMine && pv != null)
                {
                    OperateItemLogic(player);
                    Destroy(gameObject);
                    pv.RPC("ItemEat", RpcTarget.Others);
                }
			}
        }
        
        if(other.gameObject.tag == "BombStream")
        {
            Destroy(gameObject);         
        }
    }

    [PunRPC]
    public void ItemEat()
    {
        Destroy(gameObject); 
    }

}
