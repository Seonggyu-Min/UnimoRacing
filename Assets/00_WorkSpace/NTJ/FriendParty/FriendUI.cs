using MSG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FriendUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image carIconImage; // ���� ������
    [SerializeField] private TMP_Text nameText;  // �г���
    [SerializeField] private TMP_Text levelText; // ����
    [SerializeField] private Button inviteButton; // ��Ƽ �ʴ� ��ư

    private string uid;
    private string friendName;
    private int friendLevel;
    private Sprite friendCarIcon;
    private PartyManager partyManager;

    // ģ�� UI �ʱ�ȭ
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
                Debug.Log($"[FriendUI] {friendName}({uid}) ��Ƽ �ʴ� ��ư Ŭ��!");
                partyManager?.InviteToParty(uid, friendName, friendLevel, friendCarIcon);
            });
        }
    }
}
