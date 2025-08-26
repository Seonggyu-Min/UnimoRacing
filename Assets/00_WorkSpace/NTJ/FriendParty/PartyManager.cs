using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    [SerializeField] private TMP_Text partyCountText;  // "n/4"
    [SerializeField] private Transform partySlotParent; // 파티 슬롯 부모
    [SerializeField] private GameObject partySlotPrefab; // 파티 슬롯 프리팹

    private List<string> currentParty = new List<string>();
    private const int MaxParty = 4;

    // 파티 초대
    public void InviteToParty(string friendUid, string friendName, int friendLevel, Sprite carIcon)
    {
        if (currentParty.Contains(friendUid))
        {
            Debug.Log($"{friendName}은(는) 이미 파티에 있습니다.");
            return;
        }

        if (currentParty.Count >= MaxParty)
        {
            Debug.Log("파티가 꽉 찼습니다!");
            return;
        }

        currentParty.Add(friendUid);

        GameObject slot = Instantiate(partySlotPrefab, partySlotParent);
        PartySlotUI slotUI = slot.GetComponent<PartySlotUI>();
        // PartyManager 참조를 넘겨서 PartySlotUI에서 RemoveFromParty를 호출할 수 있도록 함
        slotUI.Setup(friendUid, carIcon, friendName, friendLevel, this);

        UpdatePartyCount();
    }

    // 파티 인원 카운트 업데이트
    private void UpdatePartyCount()
    {
        partyCountText.text = $"{currentParty.Count}/{MaxParty}";
    }

    // 파티에서 제거
    public void RemoveFromParty(string friendUid)
    {
        var slot = partySlotParent.GetComponentsInChildren<PartySlotUI>()
                                 .FirstOrDefault(s => s.friendUid == friendUid);
        if (slot != null)
        {
            Destroy(slot.gameObject);
            currentParty.Remove(friendUid); // currentParty 리스트에서도 제거
            UpdatePartyCount();
        }
    }
}
