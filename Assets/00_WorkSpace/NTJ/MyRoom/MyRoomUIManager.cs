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
        // 여기에 ownedCars 리스트를 MyRoomManager로부터 받아와 프리팹을 생성하는 로직을 구현해야 합니다.
        // 예를 들어:
        // foreach (var car in myRoomManager.GetOwnedCars())
        // {
        //     GameObject item = Instantiate(carInventoryPrefab, carInventoryParent);
        //     var ui = item.GetComponent<CarInventoryUI>();
        //     ui.Init(car, myRoomManager);
        // }
    }
}
