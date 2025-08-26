using DA_Assets.FCU.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class MyRoomManager : MonoBehaviour
{
    // 데이터 리스트를 인스펙터에서 할당
    [SerializeField] private List<CarData> allCarData;
    [SerializeField] private List<CharacterData> allCharacterData;

    // 현재 장착된 차량 UI
    [SerializeField] private Image equippedCarImage;
    [SerializeField] private TMP_Text equippedCarName;

    // 인벤토리 UI를 생성할 부모와 프리팹
    [SerializeField] private Transform carInventoryParent;
    [SerializeField] private GameObject carInventoryPrefab;
    [SerializeField] private Transform characterInventoryParent;
    [SerializeField] private GameObject characterInventoryPrefab;

    // 현재 장착된 캐릭터 UI
    [SerializeField] private Image equippedCharacterImage;
    [SerializeField] private TMP_Text equippedCharacterName;

    // 현재 장착 중인 데이터 변수
    private CarData currentEquippedCar;
    private CharacterData currentEquippedCharacter;

    private void Start()
    {
        PopulateCarInventory();
        PopulateCharacterInventory();

        // 게임 시작 시 첫 번째 아이템을 자동으로 장착
        if (allCarData.Count > 0) EquipCar(allCarData[0]);
        if (allCharacterData.Count > 0) EquipCharacter(allCharacterData[0]);
    }

    public void PopulateCarInventory()
    {
        foreach (Transform child in carInventoryParent)
            Destroy(child.gameObject);

        foreach (var carData in allCarData)
        {
            GameObject item = Instantiate(carInventoryPrefab, carInventoryParent);
            var ui = item.GetComponent<CarInventoryUI>();
            ui.Init(carData, this);
        }
    }

    public void PopulateCharacterInventory()
    {
        foreach (Transform child in characterInventoryParent)
            Destroy(child.gameObject);

        foreach (var charData in allCharacterData)
        {
            GameObject item = Instantiate(characterInventoryPrefab, characterInventoryParent);
            var ui = item.GetComponent<CharacterInventoryUI>(); // CharacterInventoryUI 스크립트가 필요합니다.
            ui.Init(charData, this);
        }
    }

    // 차량 장착 로직
    public void EquipCar(CarData car)
    {
        // 이전 차량의 선택 표시 제거
        if (currentEquippedCar != null)
        {
            // TODO: 현재 장착된 차량의 UI에서 음영을 제거하는 로직
        }

        equippedCarImage.sprite = car.carSprite;
        equippedCarName.text = car.carName;
        currentEquippedCar = car;

        // TODO: 새롭게 장착된 차량의 UI에 음영을 추가하는 로직
    }

    // 캐릭터 장착 로직
    public void EquipCharacter(CharacterData character)
    {
        // 이전 캐릭터의 선택 표시 제거
        if (currentEquippedCharacter != null)
        {
            // TODO: 현재 장착된 캐릭터의 UI에서 음영을 제거하는 로직
        }

        equippedCharacterImage.sprite = character.characterSprite;
        equippedCharacterName.text = character.characterName;
        currentEquippedCharacter = character;

        // TODO: 새롭게 장착된 캐릭터의 UI에 음영을 추가하는 로직
    }
}
