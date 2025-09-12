using MSG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PartyCountText : MonoBehaviour
{
    [SerializeField] private TMP_Text _partyMemberCountText;

    private void Start()
    {
        RenewPartyMembers();
        PartyService.Instance.OnPartyChanged += RenewPartyMembers;
    }

    private void OnDestroy()
    {
        if (PartyService.Instance != null)
        {
            PartyService.Instance.OnPartyChanged -= RenewPartyMembers;
        }
    }

    private void RenewPartyMembers()
    {
        int? currentMemberCount = 0;
        currentMemberCount = PartyService.Instance?.Members.Count;
        _partyMemberCountText.text = $"{currentMemberCount}/{RoomMakeHelper.MAX_PLAYERS}";
    }
}
