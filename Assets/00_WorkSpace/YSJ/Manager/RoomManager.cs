using Photon.Pun;
using Photon.Realtime;
using Runtime.UI;
using System;
using UnityEngine;
using YSJ.Util;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class RoomManager : SimpleSingleton<RoomManager> // , IOnEventCallback
{
    [SerializeField] private bool _isTest = false;
    [SerializeField] private MSG.NoPartyMatchMaker _matchMaker;
    private MatchingConfig raceGameConfig;

    private RoomState _state              = RoomState.None;
    private bool   _isFindRoom            = false; // 방 찾았는지 여부
    private double _matchReadyStartTime   = 0.0f;  // 매치 준비 시작 시간
    private int    _playersReadyCount     = 0;     // 플레이어 준비 수

    [SerializeField] private PopupOpener _fullPopupOpener;

    public Action OnActionRoomPPTUpdate;

    public bool IsFindRoom => _isFindRoom;
    public int PlayersReadyCount => _playersReadyCount;
    public double RoomMatchReadyStartTime => _matchReadyStartTime;


    /*public Action OnActionRoomWaitPlayer;
    public Action OnActionRoomReady;
    public Action OnActionRoomRace;*/

    /*protected override void Init()
    {
        base.Init();
        // PhotonNetwork.AddCallbackTarget(this); // IOnEventCallback 이것 땜시
        raceGameConfig = MatchingConfig.Load();

        if (_isTest)
        {
            _matchMaker.OnActionWaitPlayer -= TestRoomWaitPlayer;
            _matchMaker.OnActionWaitPlayer += TestRoomWaitPlayer;

            _matchMaker.OnActionMatchReady -= TestRoomMatchReady;
            _matchMaker.OnActionMatchReady += TestRoomMatchReady;

            _matchMaker.OnActionRace -= TestRoomRace;
            _matchMaker.OnActionRace += TestRoomRace;
        }
        else
        {
            // 현재는 이것도 Test 입니다.
            _matchMaker.OnActionWaitPlayer -= TestRoomWaitPlayer;
            _matchMaker.OnActionWaitPlayer += TestRoomWaitPlayer;

            _matchMaker.OnActionMatchReady -= TestRoomMatchReady;
            _matchMaker.OnActionMatchReady += TestRoomMatchReady;

            _matchMaker.OnActionRace -= TestRoomRace;
            _matchMaker.OnActionRace += TestRoomRace;
        }

            PhotonNetworkManager.Instance.OnActionOnJoinedRoom -= JoinedRoom;
        PhotonNetworkManager.Instance.OnActionOnJoinedRoom += JoinedRoom;

        PhotonNetworkManager.Instance.OnActionPlayerEnteredRoom -= PlayerEnteredRoom;
        PhotonNetworkManager.Instance.OnActionPlayerEnteredRoom += PlayerEnteredRoom;

        PhotonNetworkManager.Instance.OnActionPlayerLeftRoom -= PlayerLeftRoom;
        PhotonNetworkManager.Instance.OnActionPlayerLeftRoom += PlayerLeftRoom;

        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate -= PlayerPropertiesUpdate;
        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate += PlayerPropertiesUpdate;

        PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate -= RoomPropertiesUpdate;
        PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate += RoomPropertiesUpdate;
    }

    private void OnDestroy()
    {
        // PhotonNetwork.RemoveCallbackTarget(this); // IOnEventCallback 이것 땜시

        if (PhotonNetworkManager.Instance)
        {
            PhotonNetworkManager.Instance.OnActionOnJoinedRoom -= JoinedRoom;
            PhotonNetworkManager.Instance.OnActionPlayerEnteredRoom -= PlayerEnteredRoom;
            PhotonNetworkManager.Instance.OnActionPlayerLeftRoom -= PlayerLeftRoom;
            PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate -= PlayerPropertiesUpdate;
            PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate -= RoomPropertiesUpdate;
        }
    }

    #region Network Flow
    private void JoinedRoom()
    {
        bool fullNotified = PhotonNetworkCustomProperties.GetRoomProp(RoomKey.MatchFullFlag, false);
        if (fullNotified)
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
        if (!PhotonNetwork.InRoom) return;

        // 준비 인원 수 갱신 (MatchReady)
        _playersReadyCount = 0;
        foreach (var p in PhotonNetwork.PlayerList)
        {
            bool isReady = PhotonNetworkCustomProperties.GetPlayerProp(p, PlayerKey.MatchReady, false);
            if (isReady) _playersReadyCount++;
        }
        OnActionRoomPPTUpdate?.Invoke();

        if (!PhotonNetwork.IsMasterClient) return;
        Room room = PhotonNetwork.CurrentRoom;

        if (_playersReadyCount == room.MaxPlayers)
        {
            _matchReadyStartTime = PhotonNetwork.Time;

            // [룸데이터갱신]
            PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>
            {
                { RoomKey.MatchFullFlag,            true                    },
                { RoomKey.RoomState,                RoomState.Race          },
                { RoomKey.MatchReadyCheckStartTime, _matchReadyStartTime    },
            });
        }
    }

    private void RoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // 룸 상태 처리 (int/enum 타입 모두 안전 파싱)
        var keyRoomState = PhotonNetworkCustomProperties.ToKeyString(RoomKey.RoomState);
        this.PrintLog($"keyRoomState = {keyRoomState}");
        if (propertiesThatChanged.ContainsKey(keyRoomState))
        {
            this.PrintLog($"hav KeyRoomState = {keyRoomState}");

            var raw = propertiesThatChanged[keyRoomState];
            RoomState next;
            if (raw is int i) next = (RoomState)i;
            else if (raw is RoomState rs) next = rs;
            else
            {
                try { next = (RoomState)Convert.ToInt32(raw); }
                catch { next = _state; }
            }

            RoomStateMachine(next);
        }
    }
    #endregion

    public void MatchAction()
    {
        if (!PhotonNetwork.InLobby) return;


        var opts = new RoomOptions
        {
            MaxPlayers = raceGameConfig.RoomRacePlayer,
            IsVisible  = true,
            IsOpen     = true,
            CustomRoomProperties = new Hashtable
            {
                { PhotonNetworkCustomProperties.ToKeyString(RoomKey.RoomState),                 RoomState.WaitPlayer                },

                { PhotonNetworkCustomProperties.ToKeyString(RoomKey.MatchFullFlag),             false                               },
                { PhotonNetworkCustomProperties.ToKeyString(RoomKey.MatchReadyCheckStartTime),  -1                                  },
                { PhotonNetworkCustomProperties.ToKeyString(RoomKey.MatchChoosableMapCount),    raceGameConfig.RaceChoosableMapCount},
                { PhotonNetworkCustomProperties.ToKeyString(RoomKey.MatchRaceMapId),            -1                                  },

                { PhotonNetworkCustomProperties.ToKeyString(RoomKey.RaceState),                 RaceState.None                      },
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

    // 여기서 레디 팝업 띄움
    private void TryNotifyRoomFull()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
        {
            this.PrintLog("마스터 클라이언트가 아니거나 현재 룸이 없습니다.", LogType.Warning);
            return;
        }


        var room = PhotonNetwork.CurrentRoom;

        // 이미 알림 보냈으면 스킵
        bool alreadyNotified = PhotonNetworkCustomProperties.GetRoomProp(RoomKey.MatchFullFlag, false);
        if (alreadyNotified) return;

        var toSet = new Dictionary<RoomKey, object>();

        this.PrintLog("마스터 클라이언트 현재 플레이어 체크");
        // 꽉 찼는지 체크
        if (room.MaxPlayers > 0 && room.PlayerCount >= room.MaxPlayers)
        {
            this.PrintLog("[Room] Full detected. Broadcasting...", LogType.Log);

            // 추가 입장 차단
            if (room.IsOpen) room.IsOpen = false;

            PhotonNetwork.RaiseEvent(
                NetEvents.RoomFull,
                null,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                new ExitGames.Client.Photon.SendOptions { Reliability = true }
            );

            toSet[RoomKey.MatchFullFlag] = true;
            toSet[RoomKey.RoomState] = RoomState.MatchReady;
            toSet[RoomKey.MatchReadyCheckStartTime] = PhotonNetwork.Time;
        }
        else
        {
            toSet[RoomKey.RoomState] = RoomState.WaitPlayer;
        }

        // [룸데이터갱신]
        PhotonNetworkCustomProperties.SetRoomProps(toSet);
    }

    // 나가는 녀석이 이걸 자체적으로 호출하면 에러 -> 마스터에서 처리
    private void ReopenRoomAndClearFullFlag()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null) return;

        var room = PhotonNetwork.CurrentRoom;

        // 방 다시 열기
        if (!room.IsOpen) room.IsOpen = true;

        // FullNotified 플래그 제거
        bool hasFlag = PhotonNetworkCustomProperties.GetRoomProp(RoomKey.MatchFullFlag, false);
        if (hasFlag)
        {
            PhotonNetworkCustomProperties.SetRoomProp(RoomKey.MatchFullFlag, false);
            this.PrintLog("[Room] Full flag cleared (player left).", LogType.Log);
        }
    }

    private void RoomStateMachine(RoomState state)
    {
        this.PrintLog($"RoomState: {_state.ToString()} > {state.ToString()})");
        if (state == _state)
            return;

        _state = state;
        switch (state)
        {
            case RoomState.WaitPlayer:
                OnActionRoomWaitPlayer?.Invoke();
                PhotonNetworkCustomProperties.RoomWaitPlayerStateSetting(raceGameConfig.RoomRacePlayer);

                break;

            case RoomState.MatchReady:
                OnActionRoomReady?.Invoke();
                PhotonNetworkCustomProperties.RoomWaitPlayerStateSetting(0);
                // 필요 시 RaiseEvent 등으로 UI 오픈 신호 처리
                break;

            case RoomState.Race:
                OnActionRoomRace?.Invoke();
                SceneID sceneId = PhotonNetworkCustomProperties.GetLocalPlayerProp<SceneID>(PlayerKey.CurrentScene, SceneID.None);
                if (SceneID.InGameScene == sceneId)
                {
                    this.PrintLog("현재 레이싱 씬에 있습니다.");
                    return;
                }
                break;

            default:
                PhotonNetworkCustomProperties.RoomNoneStateSetting();
                break;
        }
    }

    #region Popup Opener
    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == NetEvents.RoomFull)
        {
            Debug.Log("방이 꽉 찼다는 알림 받음!");
            this.PrintLog("OnEvent > ShowRoomFullPopup");
            ShowRoomFullPopup();
        }
    }

    private void ShowRoomFullPopup()
    {
        if (_fullPopupOpener != null) _fullPopupOpener.OpenPopup();
        else Debug.LogWarning("PopupOpener가 설정되지 않았습니다.");
    }
    #endregion*/

    /* #region Util
     public string PrintCustomProperties(string methodName = "\n")
     {
         if (PhotonNetwork.CurrentRoom == null)
             return "[Room] No CurrentRoom";

         var sb = new StringBuilder();
         sb.Append(methodName).Append("\n");

         foreach (RoomKey e in Enum.GetValues(typeof(RoomKey)))
         {
             try
             {
                 var key = PhotonNetworkCustomProperties.ToKeyString(e);
                 object val = null;
                 PhotonNetwork.CurrentRoom.CustomProperties?.TryGetValue(key, out val);
                 sb.Append($"{e} : {(val != null ? val.ToString() : "null")}\n");
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
     #endregion*/

    #region V2
    protected override void Init()
    {
        base.Init();

        if (_matchMaker != null && _isTest)
        {
            _matchMaker.OnActionWaitPlayer -= TestRoomWaitPlayer;
            _matchMaker.OnActionMatchReady -= TestRoomMatchReady;
            _matchMaker.OnActionRace -= TestRoomRace;

            PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate -= PlayerPropertiesUpdate;
            PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate -= RoomPropertiesUpdate;


            _matchMaker.OnActionWaitPlayer += TestRoomWaitPlayer;
            _matchMaker.OnActionMatchReady += TestRoomMatchReady;
            _matchMaker.OnActionRace += TestRoomRace;

            PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate += PlayerPropertiesUpdate;
            PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate += RoomPropertiesUpdate;

            PhotonNetwork.SendRate          = 60; // 초당 패킷 전송 횟수(기본 20)
            PhotonNetwork.SerializationRate = 20; // OnPhotonSerializeView 호출 빈도(기본 10)
        }
    }

    private void OnDestroy()
    {
        if (_matchMaker != null)
        {
            _matchMaker.OnActionWaitPlayer -= TestRoomWaitPlayer;
            _matchMaker.OnActionMatchReady -= TestRoomMatchReady;
            _matchMaker.OnActionRace -= TestRoomRace;

            PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate -= PlayerPropertiesUpdate;
            PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate -= RoomPropertiesUpdate;
        }
    }

    private void TestRoomWaitPlayer()
    {
        PhotonNetworkCustomProperties.RoomWaitPlayerStateSetting();
        PhotonNetworkCustomProperties.LocalPlayerRoomWaitPlayerSetting();
    }
    private void TestRoomMatchReady()
    {
        PhotonNetworkCustomProperties.RoomMatchReadyStateSetting(0);
        PhotonNetworkCustomProperties.LocalPlayerRoomMatchReadySetting();
    }
    private void TestRoomRace()
    {
        PhotonNetworkCustomProperties.RoomRaceStateSetting();
    }

    private void PlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        this.PrintLog(PhotonNetworkCustomProperties.PrintPlayerCustomProperties(targetPlayer));
    }

    private void RoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        this.PrintLog(PhotonNetworkCustomProperties.PrintRoomCustomProperties());
    }
    #endregion
}
