using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PartyService : MonoBehaviour
    {
        #region Fields, Properties and Actions

        private bool _isInParty;
        private bool _isLeader;
        private string _leaderUid;
        private List<string> _members = new List<string>();

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;
        public bool IsInParty => _isInParty;
        public bool IsLeader => _isLeader;
        public string LeaderUid => _leaderUid;
        public IReadOnlyList<string> Members => _members;

        public event Action OnPartyChanged;

        #endregion


        #region Public API

        public void SetSolo()
        {
            _isInParty = false;
            _isLeader = true;
            _leaderUid = CurrentUid;
            _members.Clear();
            _members.Add(CurrentUid);
            OnPartyChanged?.Invoke();
        }

        public void SetParty(string leaderUid, IList<string> members)
        {
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

            _isLeader = string.Equals(CurrentUid, _leaderUid, StringComparison.Ordinal);
            OnPartyChanged?.Invoke();
        }

        public void UpdateLeader(string newLeaderUid)
        {
            if (string.IsNullOrEmpty(newLeaderUid)) return;
            _leaderUid = newLeaderUid;
            _isLeader = string.Equals(CurrentUid, _leaderUid, StringComparison.Ordinal);
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


        #region Convenience Methods

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
    }
}
