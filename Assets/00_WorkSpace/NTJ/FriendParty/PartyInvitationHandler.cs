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
        // ChatDM���� ���� �޽����� ������ �̺�Ʈ�� ó��
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
            // DMType�� PartyInvite�� �� �г� ����
            ShowPartyJoinPanel(senderUid, payload);
        }
    }

    private void ShowPartyJoinPanel(string senderUid, string payloadJson)
    {
        // ���� �г��� �ִٸ� ����
        if (_parent.childCount > 0)
        {
            foreach (Transform child in _parent)
            {
                Destroy(child.gameObject);
            }
        }

        // �� �г� �ν��Ͻ�ȭ �� �ʱ�ȭ
        PartyJoinPanel panel = Instantiate(_partyJoinPanelPrefab, _parent);
        panel.Init(senderUid, payloadJson);
    }
}
