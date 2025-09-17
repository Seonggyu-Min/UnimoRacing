using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerManager : SimpleSingleton<PlayerManager>
{
    [SerializeField] private bool _isTest = false;
    private bool _isSetup = false;

    public bool IsSetup
    {
        get
        {
            if (!_isSetup)
                this.PrintLog("`Setup` 되지 않았습니다.");

            if (PhotonNetwork.IsConnected)
                PhotonPlayerSetup();
            return _isSetup;
        }
    }

    protected override void Init()
    {
        base.Init();
        PhotonMethedSetup();
    }

    private void PhotonMethedSetup()
    {
        PhotonNetworkManager.Instance.OnActionConnectedToMaster -= PhotonPlayerSetup;
        PhotonNetworkManager.Instance.OnActionConnectedToMaster += PhotonPlayerSetup;

        PhotonNetworkManager.Instance.OnActionOnJoinedRoom -= CreateRaceCP;
        PhotonNetworkManager.Instance.OnActionOnJoinedRoom += CreateRaceCP;

        PhotonNetworkManager.Instance.OnActionLeftRoom -= ClearRaceCP;
        PhotonNetworkManager.Instance.OnActionLeftRoom += ClearRaceCP;

        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate -= OnPrint;
        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate += OnPrint;
    }
    private void PhotonPlayerSetup()
    {
        if (_isSetup) return;

        // 한 번에 배치 세팅
        PhotonNetworkCustomProperties.LocalPlayerSetup();
        PhotonNetworkCustomProperties.PrintPlayerCustomProperties(PhotonNetwork.LocalPlayer);

        _isSetup = true;
        this.PrintLog("Setup 완료");
    }

    #region Set CP 
    public void SetPlayerCPLevel(int level)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.Level, level, debugTag: "SetCP:Level");
    }
    public void SetPlayerCPExp(int exp)
    {
        if (!IsSetup) return; // bugfix: 원래 반대로 되어 있었음
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.Exp, exp, debugTag: "SetCP:Exp");
    }
    public void SetPlayerCPKartId(int kartId)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.KartId, kartId, debugTag: "SetCP:KartId");
    }
    public void SetPlayerCPCharacterId(int characterId)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.CharacterId, characterId, debugTag: "SetCP:CharacterId");
    }
    public void SetPlayerCPHopeRaceMapId(int hopeRaceMapId)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.HopeRaceMapId, hopeRaceMapId, debugTag: "SetCP:HopeRaceMapId");
    }
    public void SetPlayerCPMatchReady(bool isMatchReady)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.MatchReady, isMatchReady, debugTag: "SetCP:MatchReady");
    }
    public void SetPlayerCPRaceLoaded(bool isRaceLoaded)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.RaceLoaded, isRaceLoaded, debugTag: "SetCP:RaceLoaded");
    }
    public void SetPlayerCPRaceIsFinished(bool isRaceIsFinished)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.RaceIsFinished, isRaceIsFinished, debugTag: "SetCP:RaceIsFinished");
    }
    public void SetPlayerCPRaceFinishedTime(double RaceFinishedTime)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.RaceFinishedTime, RaceFinishedTime, debugTag: "SetCP:RaceFinishedTime");
    }
    public void SetPlayerCPCurrentScene(SceneID sceneId)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.CurrentScene, sceneId, debugTag: "SetCP:CurrentScene");
    }
    public void SetPlayerCPVote(int index)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropSafe(PlayerKey.VotedMap, index, debugTag: "SetCP:VotedMap");
    }
    #endregion

    #region Get CP 
    public int GetPlayerCPLevel()
    {
        if (!IsSetup) return -1;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.Level, -1);
    }
    public int GetPlayerCPExp()
    {
        if (!IsSetup) return -1; // bugfix: 원래 반대로 되어 있었음
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.Exp, -1);
    }
    public int GetPlayerCPKarId()
    {
        if (!IsSetup) return -1;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.KartId, -1);
    }
    public int GetPlayerCPCharacterId()
    {
        if (!IsSetup) return -1;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.CharacterId, -1);
    }
    public int GetPlayerCPHopeRaceMapId()
    {
        if (!IsSetup) return -1;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.HopeRaceMapId, -1);
    }
    public bool GetPlayerCPMatchReady()
    {
        if (!IsSetup) return false;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.MatchReady, false);
    }
    public bool GetPlayerCPRaceLoaded()
    {
        if (!IsSetup) return false;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.RaceLoaded, false);
    }
    public bool GetPlayerCPRaceIsFinished()
    {
        if (!IsSetup) return false;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.RaceIsFinished, false);
    }
    public double GetPlayerCPRaceFinishedTime()
    {
        if (!IsSetup) return -1.0f;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.RaceFinishedTime, -1.0f);
    }
    public SceneID GetPlayerCPCurrentScene()
    {
        if (!IsSetup) return SceneID.None;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.CurrentScene, SceneID.None);
    }
    public int GetPlayerCPVoteIndex()
    {
        if (!IsSetup) return 1;
        return PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.VotedMap, 1);
    }
    #endregion

    #region Race CP
    private void CreateRaceCP()
    {
        // OnJoinedRoom 시점: 안전하지만 래퍼로 더 단단하게
        var dict = new Dictionary<PlayerKey, object>()
        {
            { PlayerKey.RaceLoaded, false },
            { PlayerKey.RaceIsFinished, -1 },
            { PlayerKey.RaceFinishedTime, -1d },
        };
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropsSafe(dict, debugTag: "CreateRaceCP");
    }

    private void ClearRaceCP()
    {
        // null로 세팅하면 해당 키 제거됨
        var dict = new Dictionary<PlayerKey, object>()
        {
            { PlayerKey.RaceLoaded, null },
            { PlayerKey.RaceIsFinished, null },
            { PlayerKey.RaceFinishedTime, null },
        };
        PhotonNetworkCustomProperties.TrySetLocalPlayerPropsSafe(dict, debugTag: "ClearRaceCP");
    }
    #endregion

    public void OnPrint(Player targetPlayer, Hashtable changedProps)
    {
        PhotonNetworkCustomProperties.PrintPlayerCustomProperties(PhotonNetwork.LocalPlayer);
    }
}