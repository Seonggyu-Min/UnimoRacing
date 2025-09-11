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
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.Level, level);
    }
    public void SetPlayerCPExp(int exp)
    {
        if (IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.Exp, exp);
    }
    public void SetPlayerCPKartId(int kartId)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.KartId, kartId);
    }
    public void SetPlayerCPCharacterId(int characterId)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.CharacterId, characterId);
    }
    public void SetPlayerCPHopeRaceMapId(int hopeRaceMapId)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.HopeRaceMapId, hopeRaceMapId);
    }
    public void SetPlayerCPMatchReady(bool isMatchReady)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.MatchReady, isMatchReady);
    }
    public void SetPlayerCPRaceLoaded(bool isRaceLoaded)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.RaceLoaded, isRaceLoaded);
    }
    public void SetPlayerCPRaceIsFinished(bool isRaceIsFinished)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.RaceIsFinished, isRaceIsFinished);
    }
    public void SetPlayerCPRaceFinishedTime(double RaceFinishedTime)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.RaceFinishedTime, RaceFinishedTime);
    }
    public void SetPlayerCPCurrentScene(SceneID sceneId)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.CurrentScene, sceneId);
    }
    public void SetPlayerCPVote(int index)
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.VotedMap, index);
    }
    #endregion

    #region Get CP 
    public void GetPlayerCPLevel()
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.Level, -1);
    }
    public void GetPlayerCPExp()
    {
        if (IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.Exp, -1);
    }
    public void GetPlayerCPKarId()
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.KartId, -1);
    }
    public void GetPlayerCPCharacterId()
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.CharacterId, -1);
    }
    public void GetPlayerCPHopeRaceMapId()
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.HopeRaceMapId, -1);
    }
    public void GetPlayerCPMatchReady()
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.MatchReady, false);
    }
    public void GetPlayerCPRaceLoaded()
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.RaceLoaded, false);
    }
    public void GetPlayerCPRaceIsFinished()
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.RaceIsFinished, false);
    }
    public void GetPlayerCPRaceFinishedTime()
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.RaceFinishedTime, -1.0f);
    }
    public void GetPlayerCPCurrentScene()
    {
        if (!IsSetup) return;
        PhotonNetworkCustomProperties.GetLocalPlayerProp(PlayerKey.CurrentScene, SceneID.None);
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
        PhotonNetworkCustomProperties.LocalPlayerRaceWaitPlayerSetting();
    }
    private void ClearRaceCP()
    {
        // null로 세팅하면 해당 키 제거됨
        var keysToClear = new[]
        {
            PlayerKey.RaceLoaded,
            PlayerKey.RaceIsFinished,
            PlayerKey.RaceFinishedTime,
        };

        var dict = new Dictionary<PlayerKey, object>();
        foreach (var k in keysToClear) dict[k] = null;

        PhotonNetworkCustomProperties.SetPlayerProps(PhotonNetwork.LocalPlayer, dict);
    }

    #endregion

    public void OnPrint(Player targetPlayer, Hashtable changedProps)
    {
        PhotonNetworkCustomProperties.PrintPlayerCustomProperties(PhotonNetwork.LocalPlayer);
    }

}