using ExitGames.Client.Photon;
using Photon.Chat;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSG
{
    public enum DMType
    {
        Invite, // 방으로 들어오라는 명령
        Out,    // 리더가 파티원에게 즉시 방 나가기 발신
        Recall, // 파티룸으로 복귀 명령, Out을 먼저 보내고 방이 생성된 뒤에 호출됨
        Cancel, // 매칭 취소

        PartyInvite,  // 파티에 초대
        PartyAccept,  // 파티 초대에 승낙
        PartyReject,  // 파티 초대에 거절
        PartySync,    // 파티원들 동기화

        PartyLeave,   // 자신이 나가기
        //PartyKick,  // 미구현 상태, 파티원 강퇴
        PartyDisband  // 파티 해산 -> 리더가 나가면 파티 해산으로
    }

    [Serializable] public class PartyInviteMsg { public string partyId; public string leaderUid; public string[] members; }
    [Serializable] public class PartyAcceptMsg { public string partyId; public string accepterUid; }
    [Serializable] public class PartyRejectMsg { public string partyId; public string rejecterUid; public string reason; }
    [Serializable] public class PartySyncMsg { public string partyId; public string leaderUid; public string[] members; }
    [Serializable] public class PartyLeaveMsg { public string leaderUid; public string leaverUid; }
    [Serializable] public class PartyDisbandMsg { public string leaderUid; }


    public class ChatDM : MonoBehaviour, IChatClientListener
    {
        #region Fields, Properties and Actions

        [Header("Photon Chat Settings")]
        [SerializeField] private string _region = "ASIA";
        [SerializeField] private bool _autoReconnect = true;
        [SerializeField] private float _reconnectDelaySec = 2f;

        private ChatClient _client;
        private string _selfUid;
        private bool _connected;
        private bool _connecting;
        private float _nextReconnectAt;

        private readonly Queue<(string toUid, string payload)> _pending = new();
        public event Action<string, DMType, string> OnDirectMessageReceived;

        #endregion


        #region Public Methods

        public void Initialize(string selfUid)
        {
            _selfUid = selfUid;
            EnsureClient();
            TryConnect();
        }

        public void SendInvite(string uid, string roomName) { Send(uid, Build("INVITE", roomName)); }
        public void SendOut(string uid) { Send(uid, Build("OUT", string.Empty)); }
        public void SendRecall(string uid, string homeRoom) { Send(uid, Build("RECALL", homeRoom)); }
        public void SendCancel(string uid) { Send(uid, Build("CANCEL", string.Empty)); }
        public void SendPartyInvite(string toUid, string partyId, string leaderUid, string[] members)
        {
            var msg = new PartyInviteMsg { partyId = partyId, leaderUid = leaderUid, members = members };
            Send(toUid, BuildJson("PARTY_INVITE", msg));
        }
        public void SendPartyAccept(string toUid, string partyId, string accepterUid)
        {
            var msg = new PartyAcceptMsg { partyId = partyId, accepterUid = accepterUid };
            Send(toUid, BuildJson("PARTY_ACCEPT", msg));
        }
        public void SendPartyReject(string toUid, string partyId, string rejecterUid, string reason = "")
        {
            var msg = new PartyRejectMsg { partyId = partyId, rejecterUid = rejecterUid, reason = reason ?? "" };
            Send(toUid, BuildJson("PARTY_REJECT", msg));
        }
        public void SendPartySync(string toUid, string partyId, string leaderUid, string[] members)
        {
            var msg = new PartySyncMsg { partyId = partyId, leaderUid = leaderUid, members = members };
            Send(toUid, BuildJson("PARTY_SYNC", msg));
        }
        public void SendPartyLeave(string leaderUid, string leaverUid)
        {
            var msg = new PartyLeaveMsg { leaderUid = leaderUid, leaverUid = leaverUid };
            Send(leaderUid, BuildJson("PARTY_LEAVE", msg));
        }
        public void SendPartyDisband(string toUid, string leaderUid)
        {
            var msg = new PartyDisbandMsg { leaderUid = leaderUid };
            Send(toUid, BuildJson("PARTY_DISBAND", msg));
        }

        #endregion


        #region Unity Methods

        private void Update()
        {
            if (_client != null) _client.Service();


            if (!_connected && !_connecting && _autoReconnect && Time.realtimeSinceStartup >= _nextReconnectAt)
            {
                TryConnect();
            }
        }
        private void OnDestroy()
        {
            if (_client != null && _client.CanChat)
            {
                _client.Disconnect();
            }
        }

        #endregion


        #region Internal Methods

        private void EnsureClient()
        {
            if (_client != null) return;
            _client = new ChatClient(this);
            _client.ChatRegion = _region;
        }

        private void TryConnect()
        {
            if (string.IsNullOrEmpty(_selfUid)) return;
            if (_connecting) return;

            _connecting = true;
            AuthenticationValues auth = new AuthenticationValues(_selfUid);
            _client.Connect(
            Photon.Pun.PhotonNetwork.PhotonServerSettings.AppSettings.AppIdChat,
            Photon.Pun.PhotonNetwork.AppVersion,
            auth
            );
        }

        private string Build(string head, string body)
        {
            if (string.IsNullOrEmpty(body)) return head;
            return string.Concat(head, ":", body);
        }
        private string BuildJson(string head, object body)
        {
            string json = JsonUtility.ToJson(body);
            return string.Concat(head, ":", json);
        }

        private void Send(string toUid, string content)
        {
            if (string.IsNullOrEmpty(toUid) || string.IsNullOrEmpty(content)) return;
            if (!_connected)
            {
                _pending.Enqueue((toUid, content));
                return;
            }
            _client.SendPrivateMessage(toUid, content);
        }

        private void FlushPending()
        {
            while (_pending.Count > 0 && _connected)
            {
                (string toUid, string payload) item = _pending.Dequeue();
                _client.SendPrivateMessage(item.toUid, item.payload);
            }
        }

        #endregion


        #region IChatClientListener Methods

        public void OnConnected()
        {
            _connected = true;
            _connecting = false;
            FlushPending();
        }


        public void OnDisconnected()
        {
            _connected = false;
            _connecting = false;
            if (_autoReconnect)
            {
                _nextReconnectAt = Time.realtimeSinceStartup + _reconnectDelaySec;
            }
        }


        public void OnPrivateMessage(string sender, object message, string channelName)
        {
            if (sender == _selfUid)
            {
                Debug.Log("자기 자신에게 보낸 DM이라 return");
                return;
            }

            string raw = message as string;
            if (string.IsNullOrEmpty(raw)) return;

            int idx = raw.IndexOf(':');
            string head = idx >= 0 ? raw.Substring(0, idx) : raw;
            string body = idx >= 0 ? raw.Substring(idx + 1) : string.Empty;

            DMType t = DMType.Cancel;
            if (head == "INVITE") t = DMType.Invite;
            else if (head == "OUT") t = DMType.Out;
            else if (head == "RECALL") t = DMType.Recall;
            else if (head == "CANCEL") t = DMType.Cancel;

            else if (head == "PARTY_INVITE") t = DMType.PartyInvite;
            else if (head == "PARTY_ACCEPT") t = DMType.PartyAccept;
            else if (head == "PARTY_REJECT") t = DMType.PartyReject;
            else if (head == "PARTY_SYNC") t = DMType.PartySync;

            else if (head == "PARTY_LEAVE") t = DMType.PartyLeave;
            else if (head == "PARTY_DISBAND") t = DMType.PartyDisband;

            Action<string, DMType, string> handler = OnDirectMessageReceived;
            if (handler != null)
            {
                handler(sender, t, body);
            }
        }

        #endregion


        #region No Use Methods

        public void DebugReturn(DebugLevel level, string message) { }
        public void OnChatStateChange(ChatState state) { }
        public void OnGetMessages(string channelName, string[] senders, object[] messages) { }
        public void OnSubscribed(string[] channels, bool[] results) { }
        public void OnUnsubscribed(string[] channels) { }
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
        public void OnUserSubscribed(string channel, string user) { }
        public void OnUserUnsubscribed(string channel, string user) { }

        #endregion
    }
}
