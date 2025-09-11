using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class PartyInviteManager : MonoBehaviour
//{
//    // �̱��� �ν��Ͻ�
//    public static PartyInviteManager Instance { get; private set; }
//
//    // ���� ��� ���� ��Ƽ �ʴ� ����
//    private PartyInviteMsg _pendingInvite;
//
//    // ģ�� ��� UI�� ������Ʈ�ϴ� �Լ��� ���� ����
//    [SerializeField] private FriendSubscriber _friendSubscriber;
//
//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//            DontDestroyOnLoad(gameObject);
//        }
//        else
//        {
//            Destroy(gameObject);
//        }
//    }
//
//    // ChatDM���� �ʴ� �޽����� ������ �� �Լ��� ȣ��
//    public void ReceivePartyInvite(string payload)
//    {
//        _pendingInvite = JsonUtility.FromJson<PartyInviteMsg>(payload);
//        Debug.Log($"[PartyInviteManager] ���ο� ��Ƽ �ʴ밡 �����߽��ϴ�: {_pendingInvite.leaderUid}�κ���");
//
//        // ģ�� ��� UI�� ������Ʈ�Ͽ� �ʴ� ī�尡 ���̰� �մϴ�.
//        if (_friendSubscriber != null)
//        {
//            _friendSubscriber.ShowPartyInvitePanel(_pendingInvite);
//        }
//    }
//
//    // UI���� �ʴ� ������ ������ �� ���
//    public PartyInviteMsg GetPendingInvite()
//    {
//        return _pendingInvite;
//    }
//
//    // �ʴ븦 �����ϰų� ������ �� ȣ���Ͽ� ������ �ʱ�ȭ
//    public void ClearPendingInvite()
//    {
//        _pendingInvite = null;
//        if (_friendSubscriber != null)
//        {
//            _friendSubscriber.ClearPartyInvitePanel();
//        }
//    }
//}