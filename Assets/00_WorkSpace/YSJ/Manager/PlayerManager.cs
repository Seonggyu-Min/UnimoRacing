using Photon.Pun;
using System.Text;
using UnityEngine;
using YSJ.Util;
using static UnityEngine.Rendering.DebugUI;

public class PlayerManager : SimpleSingleton<PlayerManager>
{
    [SerializeField] private bool _isTest = false;
    private bool _isSetup = false;

    // (필요 시 사용할 예정이라면 유지)
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

        // 테스트 모드일 때 자동 접속
        if (!PhotonNetwork.IsConnected && _isTest)
            PhotonNetwork.ConnectUsingSettings();

        PhotonNetworkManager.Instance.OnActionConnectedToMaster -= OnConnectedToMaster;
        PhotonNetworkManager.Instance.OnActionConnectedToMaster += OnConnectedToMaster;

        PhotonNetworkManager.Instance.OnActionJoinedLobby -= OnJoinedLobby;
        PhotonNetworkManager.Instance.OnActionJoinedLobby += OnJoinedLobby;

        PhotonNetworkManager.Instance.OnActionOnJoinedRoom -= OnJoinedRoom;
        PhotonNetworkManager.Instance.OnActionOnJoinedRoom += OnJoinedRoom;
    }

    private void NetworkPhotonNetworkSetup()
    {

        // 한 번에 배치 세팅
        PhotonNetworkCustomProperties.SetPlayerProps(
            PhotonNetwork.LocalPlayer,
            new System.Collections.Generic.Dictionary<PlayerKey, object>
            {
                { PlayerKey.Level,              -1                      },
                { PlayerKey.Exp,                -1                      },

                { PlayerKey.CarId,              -1                      },
                { PlayerKey.CharacterId,        -1                      },
                { PlayerKey.HopeRaceMapId,      -1                      },

                { PlayerKey.MatchReady,         false                   },

                { PlayerKey.RaceLoaded,         false                   },
                { PlayerKey.RaceIsFinished,     false                   },
                { PlayerKey.RaceFinishedTime,   0.0f                    },

                { PlayerKey.CurrentScene,       SceneID.TitleScene      },
            }
        );

        PrintCustomProperties();
        _isSetup = true;
        this.PrintLog("Setup 완료");
    }

    #region Test Code(Photon PUN2)
    private void OnConnectedToMaster()
    {
        if (_isTest)
        {
            PhotonNetwork.NickName =
                string.IsNullOrEmpty(PhotonNetwork.NickName)
                ? $"Test_User_{UnityEngine.Random.Range(int.MinValue, int.MaxValue)}"
                : PhotonNetwork.NickName;
        }

        this.PrintLog($"Connected To Master\nNickName = {PhotonNetwork.NickName}");

        NetworkPhotonNetworkSetup();

        if (_isTest)
        {
            SetPlayerBaseInfoSelection(
                PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_LEVEL,
                PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_EXP
            );
        }

        PrintCustomProperties("On Connected To Master");
        PhotonNetwork.JoinLobby();
    }

    private void OnJoinedLobby()
    {
        this.PrintLog($"Joined Lobby\nNickName = {PhotonNetwork.NickName}");
        PrintCustomProperties("On Joined Lobby");
    }

    private void OnJoinedRoom()
    {
        this.PrintLog($"Joined Room\nNickName = {PhotonNetwork.NickName}\nRoomName = {PhotonNetwork.CurrentRoom.Name}\n");
        PrintCustomProperties("On Joined Room");
    }
    #endregion

    #region CustomProperties
    public void SetPlayerBaseInfoSelection(int level, int exp)
    {
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.Level, level);
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.Exp, exp);
        PrintCustomProperties("Set PlayerBaseInfo Selection");
    }

    public void SetRaceInfoSelection(int carId, int characterId)
    {
        if (!IsSetup) return;

        PhotonNetworkCustomProperties.SetPlayerProps(
            PhotonNetwork.LocalPlayer,
            new System.Collections.Generic.Dictionary<PlayerKey, object>
            {
                { PlayerKey.CarId,       carId },
                { PlayerKey.CharacterId, characterId },
            }
        );

        PrintCustomProperties("Set RaceInfo Selection");
    }

    public void SetRaceReadySelection(bool isReady)
    {
        if (!IsSetup) return;

        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.MatchReady, isReady);
        PrintCustomProperties("Set Race Ready Selection");
    }

    public void SetRaceHopeRaceMapIdSelection(int hopeRaceMapId)
    {
        if (!IsSetup) return;

        var value = (hopeRaceMapId < 0)
            ? PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_HOPERACEMAP_ID
            : hopeRaceMapId;

        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.HopeRaceMapId, value);
        PrintCustomProperties("Set RaceHopeRaceMapId Selection");
    }

    public void SetRaceLoadedSelection(bool isLoaded = true)
    {
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.RaceLoaded, isLoaded);
        PrintCustomProperties("Set Race Loaded Selection ");
    }
    #endregion

    #region CustomProperties (Print / Clear)
    public void PrintCustomProperties(string methodName = "\n")
    {
        // 이 매니저에서 관리하는 핵심 키만 출력
        var keysToPrint = new[]
        {
            PlayerKey.Level,
            PlayerKey.Exp,

            PlayerKey.CarId,
            PlayerKey.CharacterId,
            PlayerKey.HopeRaceMapId,

            PlayerKey.MatchReady,

            PlayerKey.RaceLoaded,         // KEY_PLAYER_RACE_LOADED
            PlayerKey.RaceIsFinished,     // KEY_PLAYER_RACE_IS_FINISHED
            PlayerKey.RaceFinishedTime,   // KEY_PLAYER_RACE_FINISHED_TIME

            PlayerKey.CurrentScene,       // KEY_PLAYER_CURRENT_SCENE
        };

        var sb = new StringBuilder();
        sb.Append(methodName).Append("\n");

        foreach (var k in keysToPrint)
        {
            try
            {
                var val = PhotonNetworkCustomProperties.GetLocalPlayerProp<object>(k, default);
                sb.Append($"{k} : {val}\n");
            }
            catch
            {
                this.PrintLog($"Type Print: {k}, Value Error");
            }
        }

        this.PrintLog(sb.ToString());
    }

    public void ClearPlayerCustomProperties()
    {
        // null로 세팅하면 해당 키 제거됨
        var keysToClear = new[]
        {
            PlayerKey.Level,
            PlayerKey.Exp,
            PlayerKey.CarId,
            PlayerKey.CharacterId,
            PlayerKey.HopeRaceMapId,
            PlayerKey.MatchReady,
        };

        var dict = new System.Collections.Generic.Dictionary<PlayerKey, object>();
        foreach (var k in keysToClear) dict[k] = null;

        PhotonNetworkCustomProperties.SetPlayerProps(PhotonNetwork.LocalPlayer, dict);
    }
    #endregion
}
