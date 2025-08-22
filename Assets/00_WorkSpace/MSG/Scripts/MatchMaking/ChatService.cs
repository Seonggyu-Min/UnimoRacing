using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class ChatService : IChatClientListener
    {
        public ChatClient Client { get; private set; }
        public bool IsConnected => Client != null && Client.CanChat;

        private readonly Dictionary<string, Action<string, object>> _channelHandlers = new();
        private Action<string, object> _dmHandler;
        public event Action Connected;

        public void Connect(string appId, string userId, string region = "KR")
        {
            Client = new ChatClient(this);
            Client.ChatRegion = region;
            Client.Connect(appId, "1.0", new AuthenticationValues(userId));
        }

        public void Service() => Client?.Service();

        public void Subscribe(string channel, int history = 10) => Client?.Subscribe(new[] { channel }, history);
        public void Unsubscribe(string channel) => Client?.Unsubscribe(new[] { channel });

        public void Publish(string channel, object msg) => Client?.PublishMessage(channel, JsonUtility.ToJson(msg));
        public void SendDM(string to, object msg) => Client?.SendPrivateMessage(to, JsonUtility.ToJson(msg));

        public void OnChannelMessage(string channel, Action<string, object> handler) => _channelHandlers[channel] = handler;
        public void OnDM(Action<string, object> handler) => _dmHandler = handler;

        // presence payload는 사용 안할 수도?
        public void SetPresence(object payload) => Client?.SetOnlineStatus(ChatUserStatus.Online, JsonUtility.ToJson(payload));

        // IChatClientListener
        public void OnConnected() 
        {
            Debug.Log("[Chat] Connected");
            Connected?.Invoke();
        }
        public void OnDisconnected() { Debug.Log("[Chat] Disconnected"); }
        public void OnChatStateChange(ChatState state) { }
        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            if (_channelHandlers.TryGetValue(channelName, out var h))
                for (int i = 0; i < messages.Length; i++) h?.Invoke(senders[i], messages[i]);
        }
        public void OnPrivateMessage(string sender, object message, string channelName) => _dmHandler?.Invoke(sender, message);
        public void OnSubscribed(string[] channels, bool[] results) { }
        public void OnUnsubscribed(string[] channels) { }
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
        public void OnUserSubscribed(string channel, string user) { }
        public void OnUserUnsubscribed(string channel, string user) { }
        public void DebugReturn(DebugLevel level, string message)
        {
            if (level == DebugLevel.ERROR)
                Debug.LogError($"[Chat] {message}");
            else if (level == DebugLevel.WARNING)
                Debug.LogWarning($"[Chat] {message}");
            else
                Debug.Log($"[Chat] {message}");
        }


        [Serializable]
        private struct PresenceMsg
        {
            public bool inRoom, inGame;
            public string partyId;
            public int ver;
            public PresenceMsg(bool r, bool g, string p, int v) { inRoom = r; inGame = g; partyId = p; ver = v; }
        }
    }
}
