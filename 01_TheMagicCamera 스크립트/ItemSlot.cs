using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private bool bExist = false;
    private Item myItem;
    Vector3 originPos;

    Transform startParent;

    public GameObject inventoryObject;

    // Start is called before the first frame update
    private void Start()
    {
        inventoryObject = GameObject.FindGameObjectWithTag("Inventory");
    }

    public void SetMyItem(Item item, int pos)
    {
        myItem = item;
        if (myItem.GetItemName() == "Rock")
        {
            GameObject.Find("RockShowing").GetComponent<UpdateItemPos>().notify(pos);
        }
        else if (myItem.GetItemName() == "Mushroom")
        {
            GameObject.Find("MushroomShowing").GetComponent<UpdateItemPos>().notify(pos);
        }
        else if (myItem.GetItemName() == "WaterDrop")
        {
            GameObject.Find("WaterDropShowing").GetComponent<UpdateItemPos>().notify(pos);
        }
        else if (myItem.GetItemName() == "Cloud")
        {
            GameObject.Find("CloudShowing").GetComponent<UpdateItemPos>().notify(pos);
        }
        else if (myItem.GetItemName() == "Bush")
        {
            GameObject.Find("BushShowing").GetComponent<UpdateItemPos>().notify(pos);
        }

        bExist = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (bExist)
        {
            startParent = transform.parent;
            transform.SetParent(GameObject.FindGameObjectWithTag("UI Canvas").transform);
            originPos = transform.position;
            inventoryObject.SetActive(false);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (bExist)
        {
            transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (bExist)
        {
            myItem.CreateObject();
            transform.position = originPos;
            transform.SetParent(startParent);
        }
    }
}
