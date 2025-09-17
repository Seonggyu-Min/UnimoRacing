using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class PartyInviteManager : MonoBehaviour
//{
//    // 싱글턴 인스턴스
//    public static PartyInviteManager Instance { get; private set; }
//
//    // 현재 대기 중인 파티 초대 정보
//    private PartyInviteMsg _pendingInvite;
//
//    // 친구 목록 UI를 업데이트하는 함수에 대한 참조
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
//    // ChatDM에서 초대 메시지를 받으면 이 함수를 호출
//    public void ReceivePartyInvite(string payload)
//    {
//        _pendingInvite = JsonUtility.FromJson<PartyInviteMsg>(payload);
//        Debug.Log($"[PartyInviteManager] 새로운 파티 초대가 도착했습니다: {_pendingInvite.leaderUid}로부터");
//
//        // 친구 목록 UI를 업데이트하여 초대 카드가 보이게 합니다.
//        if (_friendSubscriber != null)
//        {
//            _friendSubscriber.ShowPartyInvitePanel(_pendingInvite);
//        }
//    }
//
//    // UI에서 초대 정보를 가져갈 때 사용
//    public PartyInviteMsg GetPendingInvite()
//    {
//        return _pendingInvite;
//    }
//
//    // 초대를 수락하거나 거절한 후 호출하여 데이터 초기화
//    public void ClearPendingInvite()
//    {
//        _pendingInvite = null;
//        if (_friendSubscriber != null)
//        {
//            _friendSubscriber.ClearPartyInvitePanel();
//        }
//    }
//}