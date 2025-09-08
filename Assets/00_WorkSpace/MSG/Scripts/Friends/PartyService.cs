using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PartyService : Singleton<PartyService>
    {
        #region Fields, Properties and Actions

        private bool _isInParty = false;
        private bool _isLeader = true;
        private string _leaderUid;
        private List<string> _members = new();
        private string _currentPartyId;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;
        public bool IsInParty => _isInParty;
        public bool IsLeader => _isLeader;
        public bool HasOnlyLeader => _members.Count <= 1 && _leaderUid == CurrentUid; // 파티원이 한 명 밖에 없으면 자동 해산하기 위해 사용
        public string LeaderUid => _leaderUid;
        public List<string> Members => _members; // 리더도 포함됨
        public string CurrentPartyId => _currentPartyId;


        public event Action OnPartyChanged;

        #endregion

        private void Awake()
        {
            SingletonInit();
        }

        #region Public API

        public void SetSolo()
        {
            _isInParty = false;
            _isLeader = true;
            _leaderUid = CurrentUid;
            _members.Clear();
            _members.Add(CurrentUid);
            ClearPartyId();
            OnPartyChanged?.Invoke();
        }

        public void SetParty(string leaderUid, IList<string> members)
        {
            Debug.Log($"[PartyService] SetParty 호출됨. 리더: {leaderUid}, 나: {CurrentUid}");

            _isInParty = true;
            _leaderUid = leaderUid;

            _members.Clear();
            if (members != null)
            {
                for (int i = 0; i < members.Count; i++)
                {
                    string uid = members[i];
                    if (!string.IsNullOrEmpty(uid) && !_members.Contains(uid))
                    {
                        _members.Add(uid);
                    }
                }
            }

            if (!string.IsNullOrEmpty(CurrentUid) && !_members.Contains(CurrentUid))
            {
                _members.Add(CurrentUid);
            }

            _isLeader = string.Equals(CurrentUid, _leaderUid);
            OnPartyChanged?.Invoke();
        }

        public void UpdateLeader(string newLeaderUid)
        {
            if (string.IsNullOrEmpty(newLeaderUid)) return;
            _leaderUid = newLeaderUid;
            _isLeader = string.Equals(CurrentUid, _leaderUid);
            OnPartyChanged?.Invoke();
        }

        public void UpdateMembers(IList<string> newMembers)
        {
            _members.Clear();
            if (newMembers != null)
            {
                for (int i = 0; i < newMembers.Count; i++)
                {
                    string uid = newMembers[i];
                    if (!string.IsNullOrEmpty(uid) && !_members.Contains(uid))
                    {
                        _members.Add(uid);
                    }
                }
            }
            if (!string.IsNullOrEmpty(CurrentUid) && !_members.Contains(CurrentUid))
            {
                _members.Add(CurrentUid);
            }

            OnPartyChanged?.Invoke();
        }

        #endregion


        #region Manage Member Methods

        public void AddMember(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return;
            if (!_members.Contains(uid))
            {
                _members.Add(uid);
                OnPartyChanged?.Invoke();
            }
        }
        public void RemoveMember(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return;
            if (_members.Remove(uid))
            {
                OnPartyChanged?.Invoke();
            }
        }
        public bool IsMember(string uid)
        {
            for (int i = 0; i < _members.Count; i++)
            {
                if (_members[i] == uid)
                {
                    return true;
                }
            }
            return false;
        }

        #endregion


        #region Manage Party ID Methods

        public void EnsurePartyIdForLeader(string myUid)
        {
            if (!_isLeader) return; // 리더만 생성

            if (string.IsNullOrEmpty(_currentPartyId))
                _currentPartyId = GeneratePartyId(myUid);
        }

        public void ClearPartyId() => _currentPartyId = null;

        public void SetPartyWithId(string partyId, string leaderUid, IList<string> members)
        {
            _currentPartyId = partyId;
            SetParty(leaderUid, members);
        }

        private string GeneratePartyId(string seedUid)
        {
            return $"{seedUid}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        #endregion
    }
}
