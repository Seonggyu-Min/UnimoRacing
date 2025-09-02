using DA_Assets.FCU.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class MyRoomManager : MonoBehaviour
{
    // 데이터 리스트
    [SerializeField] private List<CarData> allCarData;
    [SerializeField] private List<CharacterData> allCharacterData;

    // UI 관리 스크립트 참조
    [SerializeField] private MyRoomUIManager uiManager;

    // 인벤토리 UI를 생성할 부모와 프리팹
    [SerializeField] private Transform carInventoryParent;
    [SerializeField] private GameObject carInventoryPrefab;
    [SerializeField] private Transform characterInventoryParent;
    [SerializeField] private GameObject characterInventoryPrefab;

    // 현재 장착된 아이템 데이터 변수
    private CarData currentEquippedCar;
    private CharacterData currentEquippedCharacter;

    // 상단에 표시될 2D 이미지용 Image 컴포넌트
    [Header("Display Images")]
    [SerializeField] private Image characterDisplayImage;
    [SerializeField] private Image carDisplayImage;

    private void Start()
    {
        // UI 관리 스크립트 참조 가져오기
        uiManager = GetComponent<MyRoomUIManager>();

        // 인벤토리 채우기
        PopulateCarInventory();
        PopulateCharacterInventory();

        // 게임 시작 시 기본 아이템 장착 (데이터가 있는 경우)
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
            var ui = item.GetComponent<CharacterInventoryUI>();
            ui.Init(charData, this);
        }
    }

    // 차량 장착 로직
    public void EquipCar(CarData car)
    {
        // 현재 장착 데이터 업데이트
        currentEquippedCar = car;

        // 2D 이미지 업데이트
        if (carDisplayImage != null && car.carSprite != null)
        {
            carDisplayImage.sprite = car.carSprite;
            carDisplayImage.enabled = true;
        }

        UpdateEquippedUI();
    }

    // 캐릭터 장착 로직
    public void EquipCharacter(CharacterData character)
    {
        // 현재 장착 데이터 업데이트
        currentEquippedCharacter = character;

        // 2D 이미지 업데이트
        if (characterDisplayImage != null && character.characterSprite != null)
        {
            characterDisplayImage.sprite = character.characterSprite;
            characterDisplayImage.enabled = true;
        }

        UpdateEquippedUI();
    }

    // 장착 UI 업데이트 함수
    private void UpdateEquippedUI()
    {
        uiManager.UpdateEquippedUI(
            currentEquippedCharacter.characterSprite,
            currentEquippedCharacter.characterName,
            currentEquippedCar.carSprite,
            currentEquippedCar.carName
        );
    }
}
