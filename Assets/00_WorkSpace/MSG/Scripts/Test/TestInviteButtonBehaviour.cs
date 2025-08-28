using Firebase.Database;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class TestInviteButtonBehaviour : MonoBehaviour
    {
        //private TMP_Text label;
        //private Button joinPartyButton;
        //private string _targetUid;
        //private string _displayName;
        //private PartyServices _party;

        //private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        //public void Init(string targetUid, string displayName = null)
        //{
        //    _party = FindObjectOfType<PartyServices>();
        //    label = GetComponentInChildren<TMP_Text>();
        //    joinPartyButton = GetComponent<Button>();

        //    _targetUid = targetUid;
        //    _displayName = string.IsNullOrEmpty(displayName) ? targetUid : displayName;

        //    if (label != null) label.text = $"{_displayName} (파티 합류)";
        //    if (joinPartyButton != null)
        //    {
        //        joinPartyButton.onClick.RemoveAllListeners();
        //        joinPartyButton.onClick.AddListener(OnClickInviteButton);
        //    }
        //}

        //private void OnClickInviteButton()
        //{
        //    if (_party == null) 
        //    { 
        //        Debug.LogWarning("PartyServices 없음"); 
        //        return;
        //    }
        //    if (string.IsNullOrEmpty(_targetUid)) 
        //    { 
        //        Debug.LogWarning("target uid 없음");
        //        return; 
        //    }

        //    string partyId = ComposePairPartyId(CurrentUid, _targetUid);

        //    _party.JoinParty(partyId);

        //    Debug.Log($"JoinParty 호출: partyId={partyId}, me={CurrentUid}, target={_targetUid}");
        //}

        //private string ComposePairPartyId(string a, string b)
        //{
        //    if (string.CompareOrdinal(a, b) < 0) return $"pair:{a}_{b}";
        //    else return $"pair:{b}_{a}";
        //}
    }
}
