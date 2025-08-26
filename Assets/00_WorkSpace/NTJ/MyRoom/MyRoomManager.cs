using DA_Assets.FCU.Model;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

public class MyRoomManager : MonoBehaviour
{
    // ������ ����Ʈ�� �ν����Ϳ��� �Ҵ�
    [SerializeField] private List<CarData> allCarData;
    [SerializeField] private List<CharacterData> allCharacterData;

    // ���� ������ ���� UI
    [SerializeField] private Image equippedCarImage;
    [SerializeField] private TMP_Text equippedCarName;

    // �κ��丮 UI�� ������ �θ�� ������
    [SerializeField] private Transform carInventoryParent;
    [SerializeField] private GameObject carInventoryPrefab;
    [SerializeField] private Transform characterInventoryParent;
    [SerializeField] private GameObject characterInventoryPrefab;

    // ���� ������ ĳ���� UI
    [SerializeField] private Image equippedCharacterImage;
    [SerializeField] private TMP_Text equippedCharacterName;

    // ���� ���� ���� ������ ����
    private CarData currentEquippedCar;
    private CharacterData currentEquippedCharacter;

    private void Start()
    {
        PopulateCarInventory();
        PopulateCharacterInventory();

        // ���� ���� �� ù ��° �������� �ڵ����� ����
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
            var ui = item.GetComponent<CharacterInventoryUI>(); // CharacterInventoryUI ��ũ��Ʈ�� �ʿ��մϴ�.
            ui.Init(charData, this);
        }
    }

    // ���� ���� ����
    public void EquipCar(CarData car)
    {
        // ���� ������ ���� ǥ�� ����
        if (currentEquippedCar != null)
        {
            // TODO: ���� ������ ������ UI���� ������ �����ϴ� ����
        }

        equippedCarImage.sprite = car.carSprite;
        equippedCarName.text = car.carName;
        currentEquippedCar = car;

        // TODO: ���Ӱ� ������ ������ UI�� ������ �߰��ϴ� ����
    }

    // ĳ���� ���� ����
    public void EquipCharacter(CharacterData character)
    {
        // ���� ĳ������ ���� ǥ�� ����
        if (currentEquippedCharacter != null)
        {
            // TODO: ���� ������ ĳ������ UI���� ������ �����ϴ� ����
        }

        equippedCharacterImage.sprite = character.characterSprite;
        equippedCharacterName.text = character.characterName;
        currentEquippedCharacter = character;

        // TODO: ���Ӱ� ������ ĳ������ UI�� ������ �߰��ϴ� ����
    }
}
