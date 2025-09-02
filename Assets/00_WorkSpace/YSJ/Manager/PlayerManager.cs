using Photon.Pun;
using Photon.Realtime;
using System.Text;
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

            if(PhotonNetwork.IsConnected)
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
        PhotonNetworkManager.Instance.OnActionOnJoinedRoom -= OnJoinedRoom;
        PhotonNetworkManager.Instance.OnActionOnJoinedRoom += OnJoinedRoom;

        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate -= OnPrint;
        PhotonNetworkManager.Instance.OnActionPlayerPropertiesUpdate += OnPrint;
    }
    private void PhotonPlayerSetup()
    {
        if (_isSetup) return;

        // 한 번에 배치 세팅
        PhotonNetworkCustomProperties.SetPlayerProps(
            PhotonNetwork.LocalPlayer,
            new System.Collections.Generic.Dictionary<PlayerKey, object>
            {
                { PlayerKey.HopeRaceMapId,      -1                      },

                { PlayerKey.RaceLoaded,         false                   },
                { PlayerKey.RaceIsFinished,     false                   },
                { PlayerKey.RaceFinishedTime,   0.0f                    },

                // { PlayerKey.CurrentScene,       SceneID.TitleScene      },
            }
        );

        PrintCustomProperties();
        _isSetup = true;
        this.PrintLog("Setup 완료");
    }

    #region Test Code(Photon PUN2)


    private void OnJoinedRoom()
    {
        this.PrintLog($"Joined Room\nNickName = {PhotonNetwork.NickName}\nRoomName = {PhotonNetwork.CurrentRoom.Name}\n");
        PrintCustomProperties("On Joined Room");
    }
    #endregion

    #region CustomProperties
    public void SetPlayerBaseInfoSelection(int level, int exp)
    {
        // PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.Level, level);
        // PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.Exp, exp);
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
    }

    public void SetRaceReadySelection(bool isReady)
    {
        if (!IsSetup) return;

        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.MatchReady, isReady);
    }

    public void SetRaceHopeRaceMapIdSelection(int hopeRaceMapId)
    {
        if (!IsSetup) return;

        var value = (hopeRaceMapId < 0)
            ? PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_HOPERACEMAP_ID
            : hopeRaceMapId;

        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.HopeRaceMapId, value);
    }

    public void SetRaceLoadedSelection(bool isLoaded = true)
    {
        if (!IsSetup) return;

        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.RaceLoaded, isLoaded);
    }
    public void SetSceneIDSelection(SceneID sceneID)
    {
        if (!IsSetup) return;

        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.CurrentScene, (int)sceneID);
    }
    #endregion

    #region CustomProperties (Print / Clear)

    public void OnPrint(Player targetPlayer, Hashtable changedProps)
    {
        PrintCustomProperties();
    }
    public void PrintCustomProperties(string methodName = "\n")
    {
        return;
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
