using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyInvitationHandler : MonoBehaviour
{
    [SerializeField] private ChatDM _chatDM;
    [SerializeField] private PartyJoinPanel _partyJoinPanelPrefab;
    [SerializeField] private Transform _parent;

    private void Start()
    {
        // ChatDM에서 직접 메시지를 받으면 이벤트를 처리
        _chatDM.OnDirectMessageReceived += HandleDirectMessage;
    }

    private void OnDestroy()
    {
        if (_chatDM != null)
        {
            _chatDM.OnDirectMessageReceived -= HandleDirectMessage;
        }
    }

    private void HandleDirectMessage(string senderUid, DMType type, string payload)
    {
        if (type == DMType.PartyInvite)
        {
            // DMType이 PartyInvite일 때 패널 생성
            ShowPartyJoinPanel(senderUid, payload);
        }
    }

    private void ShowPartyJoinPanel(string senderUid, string payloadJson)
    {
        // 기존 패널이 있다면 삭제
        if (_parent.childCount > 0)
        {
            foreach (Transform child in _parent)
            {
                Destroy(child.gameObject);
            }
        }

        // 새 패널 인스턴스화 및 초기화
        PartyJoinPanel panel = Instantiate(_partyJoinPanelPrefab, _parent);
        panel.Init(senderUid, payloadJson);
    }
}
