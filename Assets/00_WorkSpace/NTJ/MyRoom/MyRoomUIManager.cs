using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// PopupBase�� ��ӹ޾� �˾� ����� ����
public class MyRoomUIManager : PopupBase
{
    // ������ ������ ǥ�� UI
    [Header("Equipped Items UI")]
    [SerializeField] private Image equippedCharacterImage;
    [SerializeField] private Image equippedCarImage;

    // �κ��丮 �г�
    [Header("Inventory Panels")]
    [SerializeField] private GameObject characterInventoryPanel;
    [SerializeField] private GameObject carInventoryPanel;

    // �κ��丮 ��ȯ ��ư
    [Header("Toggle Buttons")]
    [SerializeField] private Toggle characterToggleButton;
    [SerializeField] private Toggle carToggleButton;

    [SerializeField] private Button closeButton; // �ݱ� ��ư �߰�

    // MyRoomManager ��ũ��Ʈ ����
    private MyRoomManager myRoomManager;

    private void Start()
    {
        // MyRoomManager ��ũ��Ʈ ���� ��������
        myRoomManager = GetComponent<MyRoomManager>();

        // OnValueChanged ������ �߰�: ����� ������ ���� �г��� Ȱ��ȭ�ϵ��� �����մϴ�.
        characterToggleButton.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                SetInventoryPanel(true);
            }
        });

        carToggleButton.onValueChanged.AddListener((isOn) =>
        {
            if (isOn)
            {
                SetInventoryPanel(false);
            }
        });

        // �ݱ� ��ư�� Close() �Լ� ����
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => Close());
        }

        // ���� �� ĳ���� �κ��丮�� �����ϰ�, �ش� ����� 'On' ���·� ����ϴ�.
        SetInventoryPanel(true);
        characterToggleButton.isOn = true;
    }

    // �κ��丮 �г� Ȱ��ȭ/��Ȱ��ȭ
    private void SetInventoryPanel(bool isCharacterPanel)
    {
        // ĳ���� �κ��丮 �г� Ȱ��ȭ
        characterInventoryPanel.SetActive(isCharacterPanel);
        // ���� �κ��丮 �г� Ȱ��ȭ
        carInventoryPanel.SetActive(!isCharacterPanel);
    }

    // ���� UI ������Ʈ �޼��� (MyRoomManager���� ȣ��)
    public void UpdateEquippedUI(Sprite characterSprite, string characterName, Sprite carSprite, string carName)
    {
        equippedCharacterImage.sprite = characterSprite;
        equippedCarImage.sprite = carSprite;
    }
}
