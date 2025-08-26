using MSG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image carIconImage; // 차량 아이콘
    [SerializeField] private TMP_Text nameText;  // 닉네임
    [SerializeField] private TMP_Text levelText; // 레벨
    [SerializeField] private Button inviteButton; // 파티 초대 버튼

    private string uid;
    private string friendName;
    private int friendLevel;
    private Sprite friendCarIcon;
    private PartyManager partyManager;

    // 친구 UI 초기화
    public void Init(string uid, string name, int level, Sprite carIcon, PartyManager partyMgr)
    {
        this.uid = uid;
        this.friendName = name;
        this.friendLevel = level;
        this.friendCarIcon = carIcon;
        this.partyManager = partyMgr;

        if (carIconImage != null) carIconImage.sprite = carIcon;
        if (nameText != null) nameText.text = name;
        if (levelText != null) levelText.text = $"Lv.{level}";

        if (inviteButton != null)
        {
            inviteButton.onClick.RemoveAllListeners();
            inviteButton.onClick.AddListener(() =>
            {
                Debug.Log($"[FriendUI] {friendName}({uid}) 파티 초대 버튼 클릭!");
                partyManager?.InviteToParty(uid, friendName, friendLevel, friendCarIcon);
            });
        }
    }
}
