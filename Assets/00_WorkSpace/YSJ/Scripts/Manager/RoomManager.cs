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

    #region Custom Func
    protected override void Init()
    {
        base.Init();

        PhotonNetwork.SendRate = 60; // 초당 패킷 전송 횟수(기본 20)
        PhotonNetwork.SerializationRate = 30; // OnPhotonSerializeView 호출 빈도(기본 10)

        if (_matchMaker != null && _isTest)
        {
            // 초기 액션 제거 처리
            _matchMaker.OnActionWaitPlayer -= RoomWaitPlayer;
            _matchMaker.OnActionMatchReady -= RoomMatchReady;
            _matchMaker.OnActionRace -= RoomRace;

            PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate -= PlayerPropertiesUpdate;
            PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate -= RoomPropertiesUpdate;


            // 초기 액션 추가 처리
            _matchMaker.OnActionWaitPlayer += RoomWaitPlayer;
            _matchMaker.OnActionMatchReady += RoomMatchReady;
            _matchMaker.OnActionRace += RoomRace;

            PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate += PlayerPropertiesUpdate;
            PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate += RoomPropertiesUpdate;
        }
    }

    private void OnDestroy()
    {
        if (_matchMaker != null)
        {
            _matchMaker.OnActionWaitPlayer -= RoomWaitPlayer;
            _matchMaker.OnActionMatchReady -= RoomMatchReady;
            _matchMaker.OnActionRace -= RoomRace;

            PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate -= PlayerPropertiesUpdate;
            PhotonNetworkManager.Instance.OnActionRoomPropertiesUpdate -= RoomPropertiesUpdate;
        }
    }

    private void RoomWaitPlayer()
    {
        PhotonNetworkCustomProperties.RoomWaitPlayerStateSetting();
        PhotonNetworkCustomProperties.LocalPlayerRoomWaitPlayerSetting();
    }
    private void RoomMatchReady()
    {
        PhotonNetworkCustomProperties.RoomMatchReadyStateSetting(0);
        PhotonNetworkCustomProperties.LocalPlayerRoomMatchReadySetting();
    }
    private void RoomRace()
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
