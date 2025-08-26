using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    [SerializeField] private TMP_Text partyCountText;  // "n/4"
    [SerializeField] private Transform partySlotParent; // ��Ƽ ���� �θ�
    [SerializeField] private GameObject partySlotPrefab; // ��Ƽ ���� ������

    private List<string> currentParty = new List<string>();
    private const int MaxParty = 4;

    // ��Ƽ �ʴ�
    public void InviteToParty(string friendUid, string friendName, int friendLevel, Sprite carIcon)
    {
        if (currentParty.Contains(friendUid))
        {
            Debug.Log($"{friendName}��(��) �̹� ��Ƽ�� �ֽ��ϴ�.");
            return;
        }

        if (currentParty.Count >= MaxParty)
        {
            Debug.Log("��Ƽ�� �� á���ϴ�!");
            return;
        }

        currentParty.Add(friendUid);

        GameObject slot = Instantiate(partySlotPrefab, partySlotParent);
        PartySlotUI slotUI = slot.GetComponent<PartySlotUI>();
        // PartyManager ������ �Ѱܼ� PartySlotUI���� RemoveFromParty�� ȣ���� �� �ֵ��� ��
        slotUI.Setup(friendUid, carIcon, friendName, friendLevel, this);

        UpdatePartyCount();
    }

    // ��Ƽ �ο� ī��Ʈ ������Ʈ
    private void UpdatePartyCount()
    {
        partyCountText.text = $"{currentParty.Count}/{MaxParty}";
    }

    // ��Ƽ���� ����
    public void RemoveFromParty(string friendUid)
    {
        var slot = partySlotParent.GetComponentsInChildren<PartySlotUI>()
                                 .FirstOrDefault(s => s.friendUid == friendUid);
        if (slot != null)
        {
            Destroy(slot.gameObject);
            currentParty.Remove(friendUid); // currentParty ����Ʈ������ ����
            UpdatePartyCount();
        }
    }
}
