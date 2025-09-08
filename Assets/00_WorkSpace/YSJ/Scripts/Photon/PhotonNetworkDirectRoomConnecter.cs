using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class PhotonNetworkDirectRoomConnector : MonoBehaviourPunCallbacks
{
    [Header("Connect")]
    [Tooltip("PhotonServerSettings 값 대신 여기서 고정 리전을 강제하고 싶다면 설정 (예: \"asia\", \"usw\", \"eu\")")]
    public string fixedRegion = ""; // 빈 값이면 PhotonServerSettings 값을 그대로 사용

    [Tooltip("게임 버전. 매칭 분리 용도로 사용")]
    public string gameVersion = "test-0.1.0";

    [Header("Room")]
    [Tooltip("참가할 방 이름. 비워두면 랜덤 조인 시도")]
    public string roomName = "Dev_Test_Room";

    [Tooltip("Join 실패 시 방 자동 생성")]
    public bool createIfNotExists = true;

    [Tooltip("씬 자동 동기화 (마스터가 LoadLevel하면 다른 클라이언트도 따라감)")]
    public bool automaticallySyncScene = false;

    [Header("Create Room Options")]
    [Tooltip("최대 인원")]
    public byte maxPlayers = 8;

    [Tooltip("공개 방 여부 (로비에서 보이게)")]
    public bool isVisible = true;

    [Tooltip("로비에 나타나는 커스텀 프로퍼티 키")]
    public string[] lobbyPropsKeys = new string[] { "mode", "map" };

    [Tooltip("생성 시 기본 커스텀 프로퍼티")]
    public string defaultMode = "dev";
    public string defaultMap = "track_01";

    const string LOG_PREFIX = "[PUN2-DirectConnector] ";

    void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = automaticallySyncScene;
        PhotonNetwork.GameVersion = gameVersion;

        PhotonNetwork.SendRate          = 60;   // 초당 패킷 전송 횟수(기본 20)
        PhotonNetwork.SerializationRate = 30;   // OnPhotonSerializeView 호출 빈도(기본 10)

        if (!string.IsNullOrEmpty(fixedRegion))
        {
            // 런타임에서 서버 설정의 FixedRegion을 교체 (선택사항)
            PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = fixedRegion;
            Debug.Log($"{LOG_PREFIX}Force FixedRegion = {fixedRegion}");
        }
    }

    void Start()
    {
        Connect();
    }

    public void Connect()
    {
        if (PhotonNetwork.IsConnected)
        {
            Debug.Log($"{LOG_PREFIX}Already connected. Trying to (re)join a room...");
            TryJoin();
            return;
        }

        Debug.Log($"{LOG_PREFIX}Connecting using settings... AppId/Region/Protocol from PhotonServerSettings, GameVersion = {PhotonNetwork.GameVersion}");
        bool ok = PhotonNetwork.ConnectUsingSettings();
        if (!ok)
        {
            Debug.LogError($"{LOG_PREFIX}ConnectUsingSettings() returned false (check PhotonServerSettings).");
        }
    }

    void TryJoin()
    {
        if (!string.IsNullOrEmpty(roomName))
        {
            Debug.Log($"{LOG_PREFIX}Joining named room: {roomName}");
            PhotonNetwork.JoinRoom(roomName);
        }
        else
        {
            Debug.Log($"{LOG_PREFIX}Joining random room (no roomName provided).");
            PhotonNetwork.JoinRandomRoom();
        }
    }

    RoomOptions BuildRoomOptions()
    {
        var ro = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = isVisible,
            IsOpen = true,
            PublishUserId = true
        };

        // 기본 커스텀 프로퍼티
        var ht = new ExitGames.Client.Photon.Hashtable
        {
            { "mode", defaultMode },
            { "map",  defaultMap }
        };
        ro.CustomRoomProperties = ht;
        ro.CustomRoomPropertiesForLobby = lobbyPropsKeys;

        return ro;
    }

    // =========================
    // Connection Callbacks
    // =========================
    public override void OnConnected()
    {
        Debug.Log($"{LOG_PREFIX}OnConnected (to NameServer).");
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"{LOG_PREFIX}OnConnectedToMaster | Region={PhotonNetwork.CloudRegion} UserId={PhotonNetwork.LocalPlayer?.UserId}");
        TryJoin();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"{LOG_PREFIX}OnDisconnected | Cause={cause}");
    }

    public override void OnRegionListReceived(RegionHandler regionHandler)
    {
        Debug.Log($"{LOG_PREFIX}OnRegionListReceived | Best={regionHandler.BestRegion?.Code} Regions={string.Join(",", regionHandler.EnabledRegions.ConvertAll(r => r.Code))}");
    }

    // =========================
    // Lobby / Matchmaking
    // =========================
    public override void OnJoinedLobby()
    {
        Debug.Log($"{LOG_PREFIX}OnJoinedLobby");
    }

    public override void OnLeftLobby()
    {
        Debug.Log($"{LOG_PREFIX}OnLeftLobby");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"{LOG_PREFIX}OnCreateRoomFailed | Code={returnCode} Msg={message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"{LOG_PREFIX}OnJoinRoomFailed | Code={returnCode} Msg={message}");

        if (createIfNotExists && !string.IsNullOrEmpty(roomName))
        {
            Debug.Log($"{LOG_PREFIX}Creating room because join failed. Room={roomName}");
            PhotonNetwork.CreateRoom(roomName, BuildRoomOptions(), TypedLobby.Default);
        }
        else if (createIfNotExists && string.IsNullOrEmpty(roomName))
        {
            string newRoom = $"Auto_{Random.Range(1000, 9999)}";
            Debug.Log($"{LOG_PREFIX}Creating random-named room because join failed. Room={newRoom}");
            PhotonNetwork.CreateRoom(newRoom, BuildRoomOptions(), TypedLobby.Default);
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"{LOG_PREFIX}OnJoinRandomFailed | Code={returnCode} Msg={message}");

        if (createIfNotExists)
        {
            string newRoom = !string.IsNullOrEmpty(roomName) ? roomName : $"Auto_{Random.Range(1000, 9999)}";
            Debug.Log($"{LOG_PREFIX}Creating room because random join failed. Room={newRoom}");
            PhotonNetwork.CreateRoom(newRoom, BuildRoomOptions(), TypedLobby.Default);
        }
    }

    public override void OnCreatedRoom()
    {
        Debug.Log($"{LOG_PREFIX}OnCreatedRoom | Name={PhotonNetwork.CurrentRoom?.Name}");
    }

    public override void OnJoinedRoom()
    {
        var room = PhotonNetwork.CurrentRoom;
        Debug.Log($"{LOG_PREFIX}OnJoinedRoom | Name={room?.Name} PlayerCount={room?.PlayerCount} IsVisible={room?.IsVisible} IsOpen={room?.IsOpen}");
        foreach (var kv in room.CustomProperties)
        {
            Debug.Log($"{LOG_PREFIX}RoomProp {kv.Key} = {kv.Value}");
        }
    }

    public override void OnLeftRoom()
    {
        Debug.Log($"{LOG_PREFIX}OnLeftRoom");
    }

    // =========================
    // In-Room
    // =========================
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"{LOG_PREFIX}OnPlayerEnteredRoom | {newPlayer.NickName} ({newPlayer.UserId})");
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"{LOG_PREFIX}OnPlayerLeftRoom | {otherPlayer.NickName} ({otherPlayer.UserId})");
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        foreach (var k in changedProps.Keys)
        {
            Debug.Log($"{LOG_PREFIX}OnPlayerPropertiesUpdate | {targetPlayer.NickName} {k}={changedProps[k]}");
        }
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        foreach (var k in propertiesThatChanged.Keys)
        {
            Debug.Log($"{LOG_PREFIX}OnRoomPropertiesUpdate | {k}={propertiesThatChanged[k]}");
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"{LOG_PREFIX}OnMasterClientSwitched | NewMaster={newMasterClient.NickName}");
    }

    // =========================
    // Utility (테스트용 수동 API)
    // =========================
    [ContextMenu("Force Reconnect")]
    public void ForceReconnect()
    {
        Debug.Log($"{LOG_PREFIX}ForceReconnect()");
        if (PhotonNetwork.IsConnected) PhotonNetwork.Disconnect();
        Connect();
    }

    [ContextMenu("Leave Room")]
    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.Log($"{LOG_PREFIX}LeaveRoom()");
            PhotonNetwork.LeaveRoom();
        }
    }
}
