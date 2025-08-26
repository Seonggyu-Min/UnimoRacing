using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartySlotUI : MonoBehaviour
{
    public string friendUid;  // ��Ƽ �Ŵ����� ����
    [SerializeField] private Image carIconImage; // ���� ������
    [SerializeField] private TMP_Text nameText;  // �г���
    [SerializeField] private TMP_Text levelText; // ����

    public void Setup(string uid, Sprite icon, string name, int level)
    {
        friendUid = uid;
        carIconImage.sprite = icon;
        nameText.text = name;
        levelText.text = $"Lv.{level}";
    }
}
