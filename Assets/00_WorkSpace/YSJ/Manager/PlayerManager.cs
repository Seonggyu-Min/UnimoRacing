using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Text;
using UnityEngine;
using YSJ.Util;

public enum PlayerPropertyKey
{
    Level,
    Exp,

    CarId,
    CharacterId,
    HopeRaceMapId,

    Ready
}

// 현재 프레전터와 모델 같이 사용하고 있는 클래스 < (데이터 파트와, 데이터 전환 파트를 같이 가지고 있다는 말)
public class PlayerManager : SimpleSingleton<PlayerManager>
{
    [SerializeField] private bool _isTest = false;
    private bool _isSetup = false;

    private string raceCharacterIdString;
    private string raceCarIdString;
    private string hopeRaceMapIdString;

    public bool IsSetup
    {
        get
        {
            if (!_isSetup)
                this.PrintLog("`Setup` 되지 않았습니다.");

            return _isSetup;
        }
    }

    protected override void Init()
    {
        base.Init();

        // 연결 되지 않았다며
        if (!PhotonNetwork.IsConnected && _isTest)
            PhotonNetwork.ConnectUsingSettings();   // 포톤 연결

        PhotonNetworkManager.Instance.OnActionConnectedToMaster -= OnConnectedToMaster;
        PhotonNetworkManager.Instance.OnActionConnectedToMaster += OnConnectedToMaster;

        PhotonNetworkManager.Instance.OnActionJoinedLobby -= OnJoinedLobby;
        PhotonNetworkManager.Instance.OnActionJoinedLobby += OnJoinedLobby;

        PhotonNetworkManager.Instance.OnActionOnJoinedRoom -= OnJoinedRoom;
        PhotonNetworkManager.Instance.OnActionOnJoinedRoom += OnJoinedRoom;
    }
    private void NetworkPhotonNetworkSetup()
    {
        if (IsSetup) return;

        var props = new Hashtable {
            {ToKeyString(PlayerPropertyKey.Level),            -1},
            {ToKeyString(PlayerPropertyKey.Exp),              -1},

            {ToKeyString(PlayerPropertyKey.CarId),            -1},
            {ToKeyString(PlayerPropertyKey.CharacterId),      -1},
            {ToKeyString(PlayerPropertyKey.HopeRaceMapId),    -1},

            {ToKeyString(PlayerPropertyKey.Ready),         false},
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        _isSetup = true;
        this.PrintLog("Setup 완료");
    }

    #region Test Code(Photon PUN2)

    private void OnConnectedToMaster()
    {
        if (_isTest)
        {
            PhotonNetwork.NickName =
                    (string.IsNullOrEmpty(PhotonNetwork.NickName)) ?
                    $"Test_User_{UnityEngine.Random.Range(int.MinValue, int.MaxValue)}" : PhotonNetwork.NickName;
        }

        this.PrintLog($"Connected To Master" +
            $"\nNickName = {PhotonNetwork.NickName}");

        NetworkPhotonNetworkSetup();
        if (_isTest)
        {
            SetPlayerBaseInfoSelection(
                PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_LEVEL,
                PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_EXP);
        }

        PrintCustomProperties("On Connected To Master");
        PhotonNetwork.JoinLobby(); // 로비 연결
    }
    private void OnJoinedLobby()
    {
        this.PrintLog($"Joined Lobby" +
                $"\nNickName = {PhotonNetwork.NickName}");

        PrintCustomProperties("On Joined Lobby");
    }
    
    private void OnJoinedRoom()
    {
        this.PrintLog($"Joined Room" +
                $"\nNickName = {PhotonNetwork.NickName}" +
                $"\nRoomName = {PhotonNetwork.CurrentRoom.Name}\n\n");

        PrintCustomProperties("On Joined Room");
    }

    #endregion

    #region CustomProperties
    // Set
    public void SetPlayerBaseInfoSelection(int level, int exp)
    {
        var props = new Hashtable
        {
            {ToKeyString(PlayerPropertyKey.Level), level},
            {ToKeyString(PlayerPropertyKey.Exp), exp},
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PrintCustomProperties("Set PlayerBaseInfo Selection");
    }

    public void SetRaceInfoSelection()
    {
        SetRaceInfoSelection();
    }

    public void SetRaceInfoSelection(int carId, int characterId)
    {
        if (!IsSetup) return;

        var props = new Hashtable
        {
            {ToKeyString(PlayerPropertyKey.CarId), carId},
            {ToKeyString(PlayerPropertyKey.CharacterId), characterId},
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PrintCustomProperties("Set RaceInfo Selection");
    }
    public void SetRaceReadySelection(bool isReady)
    {
        if (!IsSetup) return;

        var props = new Hashtable
        {
            {ToKeyString(PlayerPropertyKey.Ready), isReady},
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PrintCustomProperties("Set Race Ready Selection");
    }
    public void SetRaceHopeRaceMapIdSelection(int hopeRaceMapId)
    {
        if (!IsSetup) return;

        hopeRaceMapId = (hopeRaceMapId < 0) ? PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_HOPERACEMAP_ID : hopeRaceMapId;
        var props = new Hashtable
        {
            {ToKeyString(PlayerPropertyKey.HopeRaceMapId), hopeRaceMapId},
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PrintCustomProperties("Set RaceHopeRaceMapId Selection");
    }

    // Print
    public void PrintCustomProperties(string methodName = "\n")
    {
        var values = Enum.GetValues(typeof(PlayerPropertyKey));

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(methodName);
        stringBuilder.Append("\n");

        foreach (var e in values)
        {
            try
            {
                stringBuilder.Append($"{e} : {PhotonNetwork.LocalPlayer.CustomProperties[ToKeyString((PlayerPropertyKey)e)]}\n");
            }
            catch { this.PrintLog($"Type Print: {e}, Value Error"); }


        }
        this.PrintLog(stringBuilder.ToString());
    }
    // Clear
    public void ClearPlayerCustomProperties()
    {
        /*var props = new Hashtable
        {
            {ToKeyString(PlayerPropertyKey.CarId), null},
            {ToKeyString(PlayerPropertyKey.CharacterId), null},
            {ToKeyString(PlayerPropertyKey.Ready), null},
        };*/

        var props = new Hashtable();
        var values = Enum.GetValues(typeof(PlayerPropertyKey));
        foreach (var e in values)
        {
            try
            {
                props.Add(ToKeyString((PlayerPropertyKey)e), null);
            }
            catch { this.PrintLog($"Type Clear: {e}, Value Error"); }
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    #endregion

    #region Util

    public string ToKeyString(PlayerPropertyKey key)
        => key switch
        {
            PlayerPropertyKey.Level => PhotonNetworkCustomProperties.KEY_PLAYER_LEVEL,
            PlayerPropertyKey.Exp => PhotonNetworkCustomProperties.KEY_PLAYER_EXP,

            PlayerPropertyKey.CarId => PhotonNetworkCustomProperties.KEY_PLAYER_CAR_ID,
            PlayerPropertyKey.CharacterId => PhotonNetworkCustomProperties.KEY_PLAYER_CHARACTER_ID,
            PlayerPropertyKey.HopeRaceMapId => PhotonNetworkCustomProperties.KEY_PLAYER_HOPERACEMAP_ID,

            PlayerPropertyKey.Ready => PhotonNetworkCustomProperties.KEY_PLAYER_MATCH_READY,
            _ => key.ToString()
        };

    #endregion
}
