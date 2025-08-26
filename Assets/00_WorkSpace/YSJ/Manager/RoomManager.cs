using Photon.Pun;
using Photon.Realtime;
using Runtime.UI;
using System;
using System.Text;
using UnityEngine;
using YSJ.Util;
using EventData = ExitGames.Client.Photon.EventData;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public enum RoomPropertyKey
{
    RaceChoosableMapCount,
    RaceMapId,
    FullNotified,           // 풀 노티드(게임 룸 시간
    MatchedReadyStartTime,  // 매치 준비 시작 시간
    RoomState,              // 룸 상태(게임 룸 들어와서 전체적인 흐름)
}

public enum RoomState
{
    WaitPlayer,
    Ready,
    Start,
    Race,
    RaceResult,
}

public enum SceneID
{
    TitleScene,
    InGameScene,
}

public class RoomManager : SimpleSingleton<RoomManager>, IOnEventCallback
{
    private RaceGameConfig raceGameConfig;

    private bool    _isFindRoom        = false;     // 방 찾았는지 여부
    private double  _matchReadyStartTime    = 0.0f;      // 매치 시작 시간

    private int     _playersReadyCount = 0;         // 플레이어 준비 수

    [SerializeField] private PopupOpener _fullPopupOpener;

    public Action OnActionRoomPPTUpdate;

    public bool IsFindRoom => _isFindRoom;
    public int PlayersReadyCount => _playersReadyCount;
    public double RoomMatchReadyStartTime => _matchReadyStartTime;

    public Action OnActionRoomWaitPlayer;
    public Action OnActionRoomReady;
    public Action OnActionRoomStart;
    public Action OnActionRoomRace;
    public Action OnActionRoomRaceResult;

    protected override void Init()
    {
        base.Init();

        raceGameConfig = RaceGameConfig.Load();
        Cleanup();

        // Photon 이벤트 콜백 등록
        PhotonNetwork.AddCallbackTarget(this);

        // 네트워크 매니저 이벤트 구독 (중복 방지 패턴: - 후 +)
        PhotonNetworkManager.Instance.OnActionJoinedLobby -= Cleanup;
        PhotonNetworkManager.Instance.OnActionJoinedLobby += Cleanup;

        PhotonNetworkManager.Instance.OnActionOnJoinedRoom -= JoinedRoom;
        PhotonNetworkManager.Instance.OnActionOnJoinedRoom += JoinedRoom;

        PhotonNetworkManager.Instance.OnActionPlayerEnteredRoom -= PlayerEnteredRoom;
        PhotonNetworkManager.Instance.OnActionPlayerEnteredRoom += PlayerEnteredRoom;

        PhotonNetworkManager.Instance.OnActionPlayerLeftRoom -= PlayerLeftRoom;
        PhotonNetworkManager.Instance.OnActionPlayerLeftRoom += PlayerLeftRoom;

        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate -= PlayerPropertiesUpdate;
        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate += PlayerPropertiesUpdate;

        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate += PlayerPropertiesUpdate;
        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate += PlayerPropertiesUpdate;

        PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate -= RoomPropertiesUpdate;
        PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate += RoomPropertiesUpdate;
    }

    private void OnDestroy()
    {
        // Photon 이벤트 콜백 해제
        PhotonNetwork.RemoveCallbackTarget(this);

        // 구독 해제
        if (PhotonNetworkManager.Instance)
        {
            PhotonNetworkManager.Instance.OnActionJoinedLobby -= Cleanup;
            PhotonNetworkManager.Instance.OnActionOnJoinedRoom -= JoinedRoom;
            PhotonNetworkManager.Instance.OnActionPlayerEnteredRoom -= PlayerEnteredRoom;
            PhotonNetworkManager.Instance.OnActionPlayerLeftRoom -= PlayerLeftRoom;
        }
    }

    #region Network Flow
    public void Cleanup()
    {
        _isFindRoom = false;
        _matchReadyStartTime = 0.0f;
        _playersReadyCount = 0;
    }

    private void JoinedRoom()
    {
        var room = PhotonNetwork.CurrentRoom;
        // 늦게 들어온 유저도 팝업 보장: 이미 플래그가 있으면 바로 표시
        if (room?.CustomProperties != null && room.CustomProperties.ContainsKey(ToKeyString(RoomPropertyKey.FullNotified)))
        {
            this.PrintLog("JoinedRoom > ShowRoomFullPopup");
            ShowRoomFullPopup();
            return;
        }

        // 마스터만 꽉 참 판정
        TryNotifyRoomFull();
    }

    private void PlayerEnteredRoom(Player _)
    {
        TryNotifyRoomFull();
    }

    private void PlayerLeftRoom(Player _)
    {
        // 사람 빠지면 방 다시 받도록 재오픈 + 플래그 제거
        ReopenRoomAndClearFullFlag();
    }

    private void PlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 룸인지 확인
        if (!PhotonNetwork.InRoom) return;

        var players = PhotonNetwork.PlayerList;
        var readyKey = PlayerManager.Instance.ToKeyString(PlayerPropertyKey.Ready);
        _playersReadyCount = 0;
        foreach (var p in players)
        {
            // 플레이어의 커스텀프롬퍼터 || 플레이어가 Ready Key를 가지고 안가지고 있다면
            if (p.CustomProperties == null || !p.CustomProperties.ContainsKey(readyKey))
                continue;

            bool r = (bool)p.CustomProperties[readyKey];
            // 준비된 플레이어들 카운트
            if (r)
                _playersReadyCount++;
        }
        OnActionRoomPPTUpdate?.Invoke();


        if (!PhotonNetwork.IsMasterClient) return;
        Room room = PhotonNetwork.CurrentRoom;

        // 최대 수와 레디된 수가 같다면
        if (_playersReadyCount == room.MaxPlayers)
        {
            _matchReadyStartTime = PhotonNetwork.Time;
            var cp = new Hashtable
            {
                [ToKeyString(RoomPropertyKey.FullNotified           )]  = true,
                [ToKeyString(RoomPropertyKey.RoomState              )]  = RoomState.Race,
                [ToKeyString(RoomPropertyKey.MatchedReadyStartTime  )]  = _matchReadyStartTime,
            };
            room.SetCustomProperties(cp);
        }
    }

    private void RoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // 룸 매치 레디 시작 시간
        if (propertiesThatChanged.ContainsKey(ToKeyString(RoomPropertyKey.MatchedReadyStartTime)))
        {
            double matchStartTime = (double)propertiesThatChanged[ToKeyString(RoomPropertyKey.MatchedReadyStartTime)];
            _matchReadyStartTime = matchStartTime;
        }

        // 룸 상태 처리
        if (propertiesThatChanged.ContainsKey(ToKeyString(RoomPropertyKey.RoomState)))
        {
            RoomState roomStateValue = (RoomState)propertiesThatChanged[ToKeyString(RoomPropertyKey.RoomState)];
            RoomStateMachine(roomStateValue);
        }
    }
    #endregion

    public void MatchAction()
    {
        if (!PhotonNetwork.InLobby) return;

        var opts = new RoomOptions
        {
            MaxPlayers = raceGameConfig.RaceMaxPlayer,  // 방 최대 인원 수
            IsVisible  = true,                          // 로비 노출 여부
            IsOpen     = true,                          // 입장 가능 여부
            CustomRoomProperties = new Hashtable
            {
                { ToKeyString(RoomPropertyKey.RaceChoosableMapCount), raceGameConfig.RaceChoosableMapCount },
                { ToKeyString(RoomPropertyKey.RaceMapId),             PhotonNetworkCustomProperties.VALUE_ROOM_NOT_CHOSEN_RACE_MAP_ID },
            },
        };

        _isFindRoom = PhotonNetwork.JoinRandomOrCreateRoom(
            expectedCustomRoomProperties: null,
            expectedMaxPlayers: 0,
            matchingType: MatchmakingMode.FillRoom,
            typedLobby: TypedLobby.Default,
            sqlLobbyFilter: null,
            roomOptions: opts
        );

        _matchReadyStartTime = PhotonNetwork.Time;
    }

    private void TryNotifyRoomFull()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
        {
            this.PrintLog("마스터 클라이언트가 아니거나 현재 룸이 없습니다.", LogType.Warning);
            return;
        }

        var room = PhotonNetwork.CurrentRoom;

        // 이미 알림 보냈으면 스킵
        if (room.CustomProperties != null && room.CustomProperties.ContainsKey(ToKeyString(RoomPropertyKey.FullNotified)))
            return;

        var cp = new Hashtable();
        // 꽉 찼는지 체크
        if (room.MaxPlayers > 0 && room.PlayerCount >= room.MaxPlayers)
        {
            this.PrintLog("[Room] Full detected. Broadcasting...", LogType.Log);

            // 추가 입장 차단
            if (room.IsOpen) room.IsOpen = false;

            // 알림 브로드캐스트(모두에게)
            PhotonNetwork.RaiseEvent(
                NetEvents.RoomFull,
                null,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new ExitGames.Client.Photon.SendOptions { Reliability = true }
            );

            // 중복 방지 플래그 세팅
            cp[ToKeyString(RoomPropertyKey.FullNotified)] = true;
            cp[ToKeyString(RoomPropertyKey.RoomState)] = RoomState.Ready;
            cp[ToKeyString(RoomPropertyKey.MatchedReadyStartTime)] = PhotonNetwork.Time;
        }
        else
        {
            cp[ToKeyString(RoomPropertyKey.RoomState)] = RoomState.WaitPlayer;
        }

        room.SetCustomProperties(cp);
    }

    private void ReopenRoomAndClearFullFlag()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;

        var room = PhotonNetwork.CurrentRoom;

        // 방 다시 열기
        if (!room.IsOpen) room.IsOpen = true;

        // 플래그 제거
        if (room.CustomProperties != null && room.CustomProperties.ContainsKey(ToKeyString(RoomPropertyKey.FullNotified)))
        {
            var cp = room.CustomProperties;
            cp[ToKeyString(RoomPropertyKey.FullNotified)] = null;
            room.SetCustomProperties(cp);
            this.PrintLog("[Room] Full flag cleared (player left).", LogType.Log);
        }
    }

    private void RoomStateMachine(RoomState state)
    {
        switch (state)
        {
            case RoomState.WaitPlayer:

                break;
            case RoomState.Ready:
                // 레디하는 창 틀어주는거
                // PhotonNetwork.RaiseEvent로 처리
                break;
            case RoomState.Start:
                // 씬 이동
                // PhotonNetwork.LoadLevel($"{SceneID.InGameScene.ToString()}"); // 실사용용
                PhotonNetwork.LoadLevel($"YSJ_{SceneID.InGameScene.ToString()}");// 테스터용
                break;
            case RoomState.Race:
                // 플레이어들이 잘 레이싱 씬에 넘어오고,
                // InGameManager가 정상적으로 플레이어들을 확인한 상태
                // 그리고 레이싱 카운트 다운을 시작한 상태라고 할 수 있다.
                break;
            case RoomState.RaceResult:

                break;

            default:
                break;
        }
    }

    #region Popup Opener
    public void OnEvent(EventData photonEvent)
    {
        //Debug.Log("OnEvent Called");
        if (photonEvent.Code == NetEvents.RoomFull)
        {
            Debug.Log("방이 꽉 찼다는 알림 받음!");
            this.PrintLog("OnEvent > ShowRoomFullPopup");
            ShowRoomFullPopup();
        }
    }

    private void ShowRoomFullPopup()
    {
        if (_fullPopupOpener != null)
        {
            var ui = _fullPopupOpener.OpenPopup();
        }
        else
            Debug.LogWarning("PopupOpener가 설정되지 않았습니다.");
    }

    #endregion


    #region Util
    public string PrintCustomProperties(string methodName = "\n")
    {
        if (PhotonNetwork.CurrentRoom == null)
            return "[Room] No CurrentRoom";

        var values = Enum.GetValues(typeof(RoomPropertyKey));
        var sb = new StringBuilder();
        sb.Append(methodName).Append("\n");

        foreach (var e in values)
        {
            try
            {
                var key = ToKeyString((RoomPropertyKey)e);
                sb.Append($"{e} : {PhotonNetwork.CurrentRoom.CustomProperties[key]}\n");
            }
            catch { this.PrintLog($"Type Print: {e}, Value Error"); }
        }
        return sb.ToString();
    }

    public string PrintCurrentPlayers()
    {
        var sb = new StringBuilder();
        sb.Append("Player List\n");
        foreach (var p in PhotonNetwork.PlayerList)
        {
            try { sb.Append($"Player Name: {p.NickName}\n"); }
            catch { this.PrintLog($"Player Print Error"); }
        }
        return sb.ToString();
    }

    public string ToKeyString(RoomPropertyKey key) => key switch
    {
        RoomPropertyKey.RaceChoosableMapCount       => PhotonNetworkCustomProperties.KEY_RACE_CHOOSABLE_MAP_COUNT,
        RoomPropertyKey.RaceMapId                   => PhotonNetworkCustomProperties.KEY_RACE_MAP_ID,
        RoomPropertyKey.FullNotified                => PhotonNetworkCustomProperties.KEY_ROOM_FULL_FLAG,
        RoomPropertyKey.MatchedReadyStartTime       => PhotonNetworkCustomProperties.KEY_ROOM_MATCH_READY_CHECK_START_TIME,
        RoomPropertyKey.RoomState                   => PhotonNetworkCustomProperties.KEY_ROOM_STATE_TYPE,
        _ => key.ToString()
    };

    public object GetCustomPropertiesValue(RoomPropertyKey key)
    {
        var cpt = PhotonNetwork.CurrentRoom.CustomProperties;
        var keyString = ToKeyString(key);
        if (cpt == null || !cpt.ContainsKey(keyString)) return null;

        object result = cpt[keyString];
        return result;
    }
    #endregion
}
