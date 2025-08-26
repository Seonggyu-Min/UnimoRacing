using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartySlotUI : MonoBehaviour
{
    public string friendUid;  // 파티 매니저와 연결
    [SerializeField] private Image carIconImage; // 차량 아이콘
    [SerializeField] private TMP_Text nameText;  // 닉네임
    [SerializeField] private TMP_Text levelText; // 레벨

    public void Setup(string uid, Sprite icon, string name, int level)
    {
        friendUid = uid;
        carIconImage.sprite = icon;
        nameText.text = name;
        levelText.text = $"Lv.{level}";
    }
}
