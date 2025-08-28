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
        Invite, // 파티 초대
        Out,    // 리더가 파티원에게 즉시 방 나가기 발신
        Recall,
        Cancel
    }

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


            Action<string, DMType, string> handler = OnDirectMessageReceived;
            if (handler != null) handler(sender, t, body);
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
