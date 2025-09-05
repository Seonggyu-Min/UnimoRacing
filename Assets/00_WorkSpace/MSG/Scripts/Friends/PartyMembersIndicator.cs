using MSG.Deprecated;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PartyMembersIndicator : MonoBehaviour
    {
        [Header("UIs")]
        [SerializeField] private PartyRequestCard _partyRequestCard; // 파티원 보여줄 UI 프리팹
        [SerializeField] private Transform _parent;

        [Header("Refs")]
        [SerializeField] private ChatDM _chatDM;
        [SerializeField] private PartyService _partyService;

        Dictionary<string, PartyRequestCard> _memberDict = new();    // key: uid, value: UI 프리팹


        private void OnEnable ()
        {
            RemakeFriendUI(); // 파티 멤버 UID로 UI 생성
            _partyService.OnPartyChanged += RemakeFriendUI;
        }

        private void OnDisable()
        {
            if (_partyService != null)
            {
                _partyService.OnPartyChanged -= RemakeFriendUI;
            }
        }


        private void RemakeFriendUI()
        {
            // Members에 존재하지만 _memberDict에 없는 uid 기반 UI 생성
            foreach (var uid in _partyService.Members)
            {
                if (!_memberDict.ContainsKey(uid))
                {
                    PartyRequestCard card = Instantiate(_partyRequestCard, _parent);
                    card.Init(uid, _chatDM, _partyService);
                    _memberDict.Add(uid, card);
                }
            }

            // _memberDict에는 있지만 Members에는 없는 UI 파괴
            List<string> toRemove = new();
            foreach (var uid in _memberDict.Keys)
            {
                if (!_partyService.Members.Contains(uid))
                {
                    Destroy(_memberDict[uid].gameObject);
                    toRemove.Add(uid);
                }
            }

            // _memberDict 정리
            foreach (var uid in toRemove)
            {
                _memberDict.Remove(uid);
            }
        }
    }
}
