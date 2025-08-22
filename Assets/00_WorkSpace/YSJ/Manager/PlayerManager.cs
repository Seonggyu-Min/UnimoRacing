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
    #region Const

    private const string CUSTOMPROPERTIES_KEY_PLAYER_LEVEL = "player_level";
    private const string CUSTOMPROPERTIES_KEY_PLAYER_EXP = "player_exp";

    private const string CUSTOMPROPERTIES_KEY_RACE_CAR_ID = "player_carId";
    private const string CUSTOMPROPERTIES_KEY_RACE_CHARACTER_ID = "player_characterId";
    private const string CUSTOMPROPERTIES_KEY_RACE_HOPERACEMAP_ID = "player_HopeRaceMapId";

    private const string CUSTOMPROPERTIES_KEY_ROOM_READY = "player_ready";

    private const int CUSTOMPROPERTIES_VALUE_DEFAULT_LEVEL = 1;
    private const int CUSTOMPROPERTIES_VALUE_DEFAULT_EXP = 0;

    private const int CUSTOMPROPERTIES_VALUE_DEFAULT_CAR_ID = 0;
    private const int CUSTOMPROPERTIES_VALUE_DEFAULT_CHARACTER_ID = 0;
    private const int CUSTOMPROPERTIES_VALUE_DEFAULT_HOPERACEMAP_ID = 0;

    #endregion

    [SerializeField] private bool _isTest = false;
    private bool _isSetup = false;

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

        /*PhotonNetworkManager.Instance.OnActionOnJoinedRoom -= OnJoinedRoom;
        PhotonNetworkManager.Instance.OnActionOnJoinedRoom += OnJoinedRoom;*/
    }
    private void NetworkPhotonNetworkSetup()
    {
        if (IsSetup) return;

        var props = new Hashtable {
            {ToKey(PlayerPropertyKey.Level),            -1},
            {ToKey(PlayerPropertyKey.Exp),              -1},

            {ToKey(PlayerPropertyKey.CarId),            -1},
            {ToKey(PlayerPropertyKey.CharacterId),      -1},
            {ToKey(PlayerPropertyKey.HopeRaceMapId),    -1},

            {ToKey(PlayerPropertyKey.Ready),         false},
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
                CUSTOMPROPERTIES_VALUE_DEFAULT_LEVEL,
                CUSTOMPROPERTIES_VALUE_DEFAULT_EXP);
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
    /*
    private void OnJoinedRoom()
    {
        this.PrintLog($"Joined Room" +
                $"\nNickName = {PhotonNetwork.NickName}" +
                $"\nRoomName = {PhotonNetwork.CurrentRoom.Name}\n\n");

        PrintCustomProperties("On Joined Room");
    }*/

    #endregion

    #region CustomProperties
    // Set
    public void SetPlayerBaseInfoSelection(int level, int exp)
    {
        var props = new Hashtable
        {
            {ToKey(PlayerPropertyKey.Level), level},
            {ToKey(PlayerPropertyKey.Exp), exp},
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PrintCustomProperties("Set PlayerBaseInfo Selection");
    }
    public void SetRaceInfoSelection(int carId, int characterId, bool isReady)
    {
        if (!IsSetup) return;

        var props = new Hashtable
        {
            {ToKey(PlayerPropertyKey.CarId), carId},
            {ToKey(PlayerPropertyKey.CharacterId), characterId},
            {ToKey(PlayerPropertyKey.Ready), isReady},
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PrintCustomProperties("Set RaceInfo Selection");
    }
    public void SetRaceHopeRaceMapIdSelection(int hopeRaceMapId)
    {
        if (!IsSetup) return;

        hopeRaceMapId = (hopeRaceMapId < 0) ? CUSTOMPROPERTIES_VALUE_DEFAULT_HOPERACEMAP_ID : hopeRaceMapId;
        var props = new Hashtable
        {
            {ToKey(PlayerPropertyKey.HopeRaceMapId), hopeRaceMapId},
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
                stringBuilder.Append($"{e} : {PhotonNetwork.LocalPlayer.CustomProperties[ToKey((PlayerPropertyKey)e)]}\n");
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
            {ToKey(PlayerPropertyKey.CarId), null},
            {ToKey(PlayerPropertyKey.CharacterId), null},
            {ToKey(PlayerPropertyKey.Ready), null},
        };*/

        var props = new Hashtable();
        var values = Enum.GetValues(typeof(PlayerPropertyKey));
        foreach (var e in values)
        {
            try
            {
                props.Add(ToKey((PlayerPropertyKey)e), null);
            }
            catch { this.PrintLog($"Type Clear: {e}, Value Error"); }
        }
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    #endregion

    #region Util

    // 해당 클래스 내부에서만 사용하는 유틸
    private string ToKey(PlayerPropertyKey key)
        => key switch
        {
            PlayerPropertyKey.Level => CUSTOMPROPERTIES_KEY_PLAYER_LEVEL,
            PlayerPropertyKey.Exp => CUSTOMPROPERTIES_KEY_PLAYER_EXP,

            PlayerPropertyKey.CarId => CUSTOMPROPERTIES_KEY_RACE_CAR_ID,
            PlayerPropertyKey.CharacterId => CUSTOMPROPERTIES_KEY_RACE_CHARACTER_ID,
            PlayerPropertyKey.HopeRaceMapId => CUSTOMPROPERTIES_KEY_RACE_HOPERACEMAP_ID,

            PlayerPropertyKey.Ready => CUSTOMPROPERTIES_KEY_ROOM_READY,
            _ => key.ToString()
        };

    #endregion
}
