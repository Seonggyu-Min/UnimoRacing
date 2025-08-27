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
    [SerializeField] private TMP_Text equippedCharacterName;
    [SerializeField] private Image equippedCarImage;
    [SerializeField] private TMP_Text equippedCarName;

    // �κ��丮 �г�
    [Header("Inventory Panels")]
    [SerializeField] private GameObject characterInventoryPanel;
    [SerializeField] private GameObject carInventoryPanel;

    // �κ��丮 ��ȯ ��ư
    [Header("Toggle Buttons")]
    [SerializeField] private Button characterToggleButton;
    [SerializeField] private Button carToggleButton;

    [SerializeField] private Button closeButton; // �ݱ� ��ư �߰�

    // MyRoomManager ��ũ��Ʈ ����
    private MyRoomManager myRoomManager;

    private void Start()
    {
        // MyRoomManager ��ũ��Ʈ�� ������ �����մϴ�.
        myRoomManager = GetComponent<MyRoomManager>();
        
        // ��ư�� ������ �߰�
        characterToggleButton.onClick.AddListener(() => SetInventoryPanel(true));
        carToggleButton.onClick.AddListener(() => SetInventoryPanel(false));
        
        // �ݱ� ��ư�� Close() �Լ� ����
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(() => Close());
        }

        // ���� �� ĳ���� �κ��丮�� ����
        SetInventoryPanel(true);
    }

    // PopupBase�� Open �޼��带 �������̵��Ͽ� �˾��� ���ϴ�.
    public override void Open()
    {
        base.Open(); // �˾� ������Ʈ Ȱ��ȭ
    }

    // �κ��丮 �г� Ȱ��ȭ/��Ȱ��ȭ
    public void SetInventoryPanel(bool isCharacterPanel)
    {
        // ĳ���� �κ��丮 �г� Ȱ��ȭ
        characterInventoryPanel.SetActive(isCharacterPanel);
        // ���� �κ��丮 �г� Ȱ��ȭ
        carInventoryPanel.SetActive(!isCharacterPanel);

        // ��ư ��ȣ�ۿ� ���� ���ο� ���� ����
        // Ȱ��ȭ�� ��ư�� ��ȣ�ۿ� �Ұ����ϰ� �����, ������ Ȱ��ȭ ���·� ����
        characterToggleButton.interactable = !isCharacterPanel;
        carToggleButton.interactable = isCharacterPanel;
    }

    // ���� UI ������Ʈ �޼��� (MyRoomManager���� ȣ��)
    public void UpdateEquippedUI(Sprite characterSprite, string characterName, Sprite carSprite, string carName)
    {
        equippedCharacterImage.sprite = characterSprite;
        equippedCharacterName.text = characterName;
        equippedCarImage.sprite = carSprite;
        equippedCarName.text = carName;
    }
}
