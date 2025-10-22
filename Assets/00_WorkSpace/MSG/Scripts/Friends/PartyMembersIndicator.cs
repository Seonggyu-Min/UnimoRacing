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
        [SerializeField] private GameObject _partyBackground;


        [Header("Refs")]
        [SerializeField] private ChatDM _chatDM;

        Dictionary<string, PartyRequestCard> _memberDict = new();    // key: uid, value: UI 프리팹


        private void OnEnable ()
        {
            RemakeFriendUI(); // 파티 멤버 UID로 UI 생성
            PartyService.Instance.OnPartyChanged += RemakeFriendUI;
        }

        private void OnDisable()
        {
            if (PartyService.Instance != null)
            {
                PartyService.Instance.OnPartyChanged -= RemakeFriendUI;
            }
        }


        private void RemakeFriendUI()
        {
            // 파티에 있지 않으면 전부 early 정리
            if (!PartyService.Instance.IsInParty)
            {
                foreach (var card in _memberDict)
                {
                    Destroy(card.Value.gameObject);
                }

                _memberDict.Clear();

                _partyBackground.gameObject.SetActive(false);
                return;
            }

            _partyBackground.gameObject.SetActive(true);

            // Members에 존재하지만 _memberDict에 없는 uid 기반 UI 생성
            foreach (var uid in PartyService.Instance.Members)
            {
                if (!_memberDict.ContainsKey(uid))
                {
                    PartyRequestCard card = Instantiate(_partyRequestCard, _parent);
                    card.Init(uid, _chatDM, true);
                    _memberDict.Add(uid, card);
                }
            }

            // _memberDict에는 있지만 Members에는 없는 UI 파괴
            List<string> toRemove = new();
            foreach (var uid in _memberDict.Keys)
            {
                if (!PartyService.Instance.Members.Contains(uid))
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
