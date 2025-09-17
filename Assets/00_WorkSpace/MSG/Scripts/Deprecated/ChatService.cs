using ExitGames.Client.Photon;
using Photon.Chat;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG.Deprecated
{
    public class ChatService : IChatClientListener
    {
        public ChatClient Client { get; private set; }
        public bool IsConnected => Client != null && Client.CanChat;

        private readonly Dictionary<string, Action<string, object>> _channelHandlers = new(); // 채널별 Action 맵핑
        private Action<string, object> _dmHandler; // dm Action 등록용
        public event Action Connected; // Chat 서버 연결 시 호출되는 Action


        // 접속 시도
        public void Connect(string appId, string userId, string region = "asia")
        {
            Client = new ChatClient(this);
            Client.ChatRegion = region;
            Client.Connect(appId, "1.0", new AuthenticationValues(userId));
        }

        public void Service() => Client?.Service();

        // 채널 구독
        public void Subscribe(string channel, int history = 10) => Client?.Subscribe(new[] { channel }, history);

        // 채널 구독 해제
        public void Unsubscribe(string channel) => Client?.Unsubscribe(new[] { channel });

        // 채널 메시지 발행
        public void Publish(string channel, object msg) => Client?.PublishMessage(channel, JsonUtility.ToJson(msg));

        // 개인 메시지 발행
        public void SendDM(string to, object msg) => Client?.SendPrivateMessage(to, JsonUtility.ToJson(msg));

        // 채널 메시지 수신 시의 콜백 등록
        public void OnChannelMessage(string channel, Action<string, object> handler) => _channelHandlers[channel] = handler;

        // 개인 메시지 수신 시의 콜백 등록
        public void OnDM(Action<string, object> handler) => _dmHandler = handler;


        // 서버 연결 시 콜백
        public void OnConnected()
        {
            Debug.Log("[Chat] Connected");
            Connected?.Invoke();
        }

        // 서버 연결 끊길 시 콜백
        public void OnDisconnected() => Debug.Log("[Chat] Disconnected");

        // 상태 변화 시
        public void OnChatStateChange(ChatState state) { }

        // 채널 메시지 묶음 수신 처리
        public void OnGetMessages(string channelName, string[] senders, object[] messages)
        {
            if (_channelHandlers.TryGetValue(channelName, out var h))
            {
                for (int i = 0; i < messages.Length; i++)
                {
                    h?.Invoke(senders[i], messages[i]);
                }
            }
        }

        // 개인 메시지 수신 처리
        public void OnPrivateMessage(string sender, object message, string channelName) => _dmHandler?.Invoke(sender, message);

        // 로그 처리
        public void DebugReturn(DebugLevel level, string message)
        {
            if (level == DebugLevel.ERROR)
                Debug.LogError($"[Chat] {message}");
            else if (level == DebugLevel.WARNING)
                Debug.LogWarning($"[Chat] {message}");
            else
                Debug.Log($"[Chat] {message}");
        }



        // presence payload는 사용 안할 수도?
        public void SetPresence(object payload) => Client?.SetOnlineStatus(ChatUserStatus.Online, JsonUtility.ToJson(payload));
        public void OnSubscribed(string[] channels, bool[] results) { }
        public void OnUnsubscribed(string[] channels) { }
        public void OnStatusUpdate(string user, int status, bool gotMessage, object message) { }
        public void OnUserSubscribed(string channel, string user) { }
        public void OnUserUnsubscribed(string channel, string user) { }


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
