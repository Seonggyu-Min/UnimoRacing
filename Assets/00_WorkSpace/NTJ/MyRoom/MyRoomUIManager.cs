using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyRoomUIManager : MonoBehaviour
{
    [SerializeField] private MyRoomManager myRoomManager;
    [SerializeField] private Transform carInventoryParent;
    [SerializeField] private GameObject carInventoryPrefab;

    public void PopulateCarInventory()
    {
        // ���⿡ ownedCars ����Ʈ�� MyRoomManager�κ��� �޾ƿ� �������� �����ϴ� ������ �����ؾ� �մϴ�.
        // ���� ���:
        // foreach (var car in myRoomManager.GetOwnedCars())
        // {
        //     GameObject item = Instantiate(carInventoryPrefab, carInventoryParent);
        //     var ui = item.GetComponent<CarInventoryUI>();
        //     ui.Init(car, myRoomManager);
        // }
    }
}
