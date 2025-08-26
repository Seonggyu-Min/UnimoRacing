using DA_Assets.FCU.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class MyRoomManager : MonoBehaviour
{
    // ������ ����Ʈ
    [SerializeField] private List<CarData> allCarData;
    [SerializeField] private List<CharacterData> allCharacterData;

    // UI ���� ��ũ��Ʈ ����
    private MyRoomUIManager uiManager;

    // �κ��丮 UI�� ������ �θ�� ������
    [SerializeField] private Transform carInventoryParent;
    [SerializeField] private GameObject carInventoryPrefab;
    [SerializeField] private Transform characterInventoryParent;
    [SerializeField] private GameObject characterInventoryPrefab;

    // ���� ������ ������ ����
    private CarData currentEquippedCar;
    private CharacterData currentEquippedCharacter;

    private void Start()
    {
        // UI ���� ��ũ��Ʈ ���� ��������
        uiManager = GetComponent<MyRoomUIManager>();

        // �κ��丮 ä���
        PopulateCarInventory();
        PopulateCharacterInventory();

        // ���� ���� �� �⺻ ������ ����
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

    // ���� ���� ����
    public void EquipCar(CarData car)
    {
        currentEquippedCar = car;
        UpdateEquippedUI();
    }

    // ĳ���� ���� ����
    public void EquipCharacter(CharacterData character)
    {
        currentEquippedCharacter = character;
        UpdateEquippedUI();
    }

    // ���� UI ������Ʈ �Լ�
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
