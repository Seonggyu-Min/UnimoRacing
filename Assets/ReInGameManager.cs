using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Text;
using YSJ.Util;
using Hashtable = ExitGames.Client.Photon.Hashtable;

// 인게임 매니저
public class ReInGameManager : SimpleSingletonPun<ReInGameManager>
{
    // private Value
    private RaceState _currentRaceState = RaceState.None;

    private double _countDownStartTime   = -1.0f;
    private double _raceStartTime        = -1.0f;

    private double _raceStartDelayTime        = -1.0f;


    // private Util Value
    private bool IsMasterClient                 => PhotonNetwork.IsMasterClient;
    private Room CurrentRoom                    => PhotonNetwork.CurrentRoom;

    private string RK(RoomKey k)                => PhotonNetworkCustomProperties.ToKeyString(k);
    private string PK(PlayerKey k)              => PhotonNetworkCustomProperties.ToKeyString(k);

    private SceneID GetPlayerSceneID(Player p)  => PhotonNetworkCustomProperties.GetPlayerProp<SceneID>(p, PlayerKey.CurrentScene);
    private bool GetPlayerRaceLoaded(Player p)  => PhotonNetworkCustomProperties.GetPlayerProp<bool>(p, PlayerKey.RaceLoaded);

    // public Value
    public RaceState CurrentRaceState       => _currentRaceState;
    public double   CountDownStartTime      => _countDownStartTime;
    public double   RaceStartTime           => _raceStartTime;

    // public Action
    /// <summary>
    /// 상태 변경 시, 초기 실행
    /// </summary>
    public Action<RaceState> OnStateChanged;

    /// <summary>
    /// 해당 상태로 변경 시, 기본 실행 코드 실행 후 처리
    /// </summary>
    public Action OnRaceState_WaitPlayer;
    /// <summary>
    /// 해당 상태로 변경 시, 기본 실행 코드 실행 후 처리
    /// </summary>
    public Action OnRaceState_LoadPlayers;
    /// <summary>
    /// 해당 상태로 변경 시, 기본 실행 코드 실행 후 처리
    /// </summary>
    public Action OnRaceState_Countdown;
    /// <summary>
    /// 해당 상태로 변경 시, 기본 실행 코드 실행 후 처리
    /// </summary>
    public Action OnRaceState_Racing;
    /// <summary>
    /// 해당 상태로 변경 시, 기본 실행 코드 실행 후 처리
    /// </summary>
    public Action OnRaceState_Finish;
    /// <summary>
    /// 해당 상태로 변경 시, 기본 실행 코드 실행 후 처리
    /// </summary>
    public Action OnRaceState_PostGame;
    /// <summary>
    /// 해당 상태로 변경 시, 기본 실행 코드 실행 후 처리
    /// </summary>
    public Action OnRaceState_FailedGame;

    protected override void Init()
    {
        base.Init();
        this.PrintLog($"Master Client: {IsMasterClient}!");

        _currentRaceState = RaceState.None;

        // 초기 상태 동기화
        if (CurrentRoom != null)
        {
            this.PrintLog($"초기 상태 동기화 실행");
            if (IsMasterClient)
            {
                this.PrintLog($"Master Client > Send Race State");
                SetRaceState(RaceState.WaitPlayer);
            }
            else
            {
                this.PrintLog($"Not Master Client > Get State");
                var state = GetRaceState();
                EnterState(state);
            }
        }
    }

    public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(target, changedProps);

        if (IsMasterClient)
        {
            RaceState nowRaceState = GetRaceState();
            switch (nowRaceState)
            {
                case RaceState.WaitPlayer:
                        IsInRacePlayers();
                    break;

                case RaceState.LoadPlayers:
                        IsRaceLoadeds();
                    break;

                case RaceState.Countdown:
                    break;

                case RaceState.Racing:
                    break;

                case RaceState.Finish:
                    break;

                case RaceState.PostGame:
                    break;

                case RaceState.FailedGame:
                    break;
            }
        }
        else
        {

        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        var sb = new StringBuilder();
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            sb.AppendLine("[RoomProps] 현재 룸이 아님");
        }
        else
        {
            sb.AppendLine("==== [Room CustomProperties] ====");
            foreach (var kv in propertiesThatChanged)
            {
                sb.AppendLine($"{kv.Key} = {kv.Value}");
            }
        }

        var state = GetRaceState();
        EnterState(state);
    }

    // 데이터 보내기
    private void SetRaceState(RaceState next)
    {
        if (!IsMasterClient || CurrentRoom == null) return;
        this.PrintLog($"Send Race State: {next}");

        switch (next)
        {
            // 어떤 데이터를 받았을 때 해당 상태로 변하냐
            // 1. 해당 씬에 들어와 InGameManager가 실행되면서 값이 변경될 때
            case RaceState.WaitPlayer:
                PhotonNetworkCustomProperties.RaceWaitPlayerSetting();

                break;

            // 어떤 데이터를 받았을 때 해당 상태로 변하냐
            // 1. 캐릭터들이 전부 해당 씬으로 넘어 왔다는 정보를 받았을 때
            case RaceState.LoadPlayers:
                PhotonNetworkCustomProperties.RaceLoadPlayersSetting();
                break;

            // 어떤 데이터를 받았을 때 해당 상태로 변하냐
            // 1. 캐릭터 생성이 다 되었고, 카트가 다 생성 되었을 때
            case RaceState.Countdown:
                PhotonNetworkCustomProperties.RaceCountdownSetting();
                break;

            // 어떤 데이터를 받았을 때 해당 상태로 변하냐
            // 1. 쿨다운 시간이 끝났을 때
            case RaceState.Racing:
                PhotonNetworkCustomProperties.RaceRacingSetting();
                break;

            // 어떤 데이터를 받았을 때 해당 상태로 변하냐
            // 1. 쿨다운 시간이 끝났을 때
            case RaceState.Finish:
                PhotonNetworkCustomProperties.RaceFinishSetting();
                break;

            case RaceState.PostGame:
                PhotonNetworkCustomProperties.RacePostGameSetting();
                break;

            case RaceState.FailedGame:
                PhotonNetworkCustomProperties.RaceFailedGameSetting();
                break;
        }
    }

    // 상태머신(초기/변경 시 진입)
    private void EnterState(RaceState state)
    {
        if(_currentRaceState == state)
        {
            this.PrintLog($"Enter state(crr: {_currentRaceState} / enter: {state}) is the same as the current state.", UnityEngine.LogType.Warning);
            return;
        }

        // 상태 변경 시, 즉시 실행
        OnStateChanged?.Invoke(state);
        this.PrintLog($"Enter Race State: {state} (crr: {_currentRaceState} / enter: {state})");

        switch (state)
        {
            case RaceState.WaitPlayer:
                LocalPlayerSetting();
                
                // 후 처리
                OnRaceState_WaitPlayer?.Invoke();
                break;

            case RaceState.LoadPlayers:
                IsRaceLoadeds();

                // 후 처리
                OnRaceState_LoadPlayers?.Invoke();
                break;

            case RaceState.Countdown:
                double setServerTime = PhotonNetwork.Time;
                _countDownStartTime = PhotonNetworkCustomProperties.GetRoomProp<double>(RoomKey.CountdownStartTime, setServerTime, 
                    () => { this.PrintLog($"카운트 다운 서버 시간 > Get Success {setServerTime}");},
                    () => { this.PrintLog("카운트 다운 서버 시간 Get Failed");   }
                );

                _raceStartTime = PhotonNetworkCustomProperties.GetRoomProp<double>(RoomKey.RaceStartTime, setServerTime,
                    () => { this.PrintLog($"레이싱 시작 서버 시간 > Get Success {setServerTime}"); },
                    () => { this.PrintLog("레이싱 시작 서버 시간 Get Failed"); }
                );

                StartCoroutine(CO_EnterCountDown());

                // 후 처리
                OnRaceState_Countdown?.Invoke();
                break;

            case RaceState.Racing:

                

                // 후 처리
                OnRaceState_Racing?.Invoke();
                break;

            case RaceState.Finish:

                // 후 처리
                OnRaceState_Finish?.Invoke();
                break;

            case RaceState.PostGame:
                
                // 후 처리
                OnRaceState_PostGame?.Invoke();
                break;

            case RaceState.FailedGame:
                
                // 후 처리
                OnRaceState_FailedGame?.Invoke();
                break;
        }

        _currentRaceState = state;
    }


    // 룸 상태 조회
    private RaceState GetRaceState()
    {
        if (CurrentRoom == null) return RaceState.WaitPlayer;
        if (CurrentRoom.CustomProperties.TryGetValue(RK(RoomKey.RaceState), out var v) && v is int i)
            return (RaceState)i;
        return RaceState.WaitPlayer;
    }

    private void SetLocalPlayerScene(SceneID sceneId)
    {
        PlayerManager.Instance.SetSceneIDSelection(sceneId);
    }
    private void LocalPlayerSetting()
    {
        this.PrintLog("플레이어 데이터 세팅");
        SetLocalPlayerScene(SceneID.InGameScene);
        PhotonNetworkCustomProperties.LocalPlayerRaceWaitPlayerSetting();
    }

    private void IsInRacePlayers()
    {
        this.PrintLog("Checked >>>>>>>>>>>>> IsInRacePlayers");
        if (!IsMasterClient || CurrentRoom == null) return;

        this.PrintLog($"Action >>>>>>>>>>>>> IsInRacePlayers {CurrentRoom.Players.Values.Count}");
        foreach (var p in CurrentRoom.Players.Values)
        {
            PhotonNetworkCustomProperties.PrintPlayerCustomProperties(p);
            var sceneId = GetPlayerSceneID(p);
            if (sceneId == SceneID.None) return;

            PhotonNetworkCustomProperties.PrintPlayerCustomProperties(p);
        }

        SetRaceState(RaceState.LoadPlayers);
    }
    private void IsRaceLoadeds()
    {
        this.PrintLog("Checked >>>>>>>>>>>>> IsRaceLoadeds");
        if (!IsMasterClient || CurrentRoom == null) return;

        this.PrintLog($"Action >>>>>>>>>>>>> IsRaceLoadeds {CurrentRoom.Players.Values.Count}");
        foreach (var p in CurrentRoom.Players.Values)
        {
            PhotonNetworkCustomProperties.PrintPlayerCustomProperties(p);
            var raceLoaded = GetPlayerRaceLoaded(p);
            if (!raceLoaded) return;

            PhotonNetworkCustomProperties.PrintPlayerCustomProperties(p);
        }

        SetRaceState(RaceState.Countdown);
    }

    private IEnumerator CO_EnterCountDown()
    {
        double delay = PhotonNetwork.Time - _countDownStartTime;
        this.PrintLog($"카운트 다운[Count Down]                          \n" +
            $"< _Client_ > {_countDownStartTime}                        \n" +
            $"< _Server Get Time 1_ > {PhotonNetwork.Time}              \n" +
            $"< _Server Get Time 2_ > {PhotonNetwork.Time}              \n" +
            $"< **Delay** > {delay}                                     \n");

        // 받은 시작 시간으로 시간을 확인하다가, 시작 시간이 된다면 Racing으로 전환한다.
        while (true)
        {
            if (_raceStartTime <= PhotonNetwork.Time)
                break;
            yield return null;
        }

        _raceStartDelayTime = PhotonNetwork.Time - _raceStartTime;
        this.PrintLog($"레이싱 시작[Race Start Time]                     \n" +
            $"< _Client_ > {_raceStartTime}                             \n" +
            $"< _Server Get Time 1_ > {PhotonNetwork.Time}              \n" +
            $"< _Server Get Time 2_ > {PhotonNetwork.Time}              \n" +
            $"< **Race Start Delay Time** > {_raceStartDelayTime}       \n" +
            $"< **Race Start Delay Time** > {PhotonNetwork.Time - _countDownStartTime}\n");

        SetRaceState(RaceState.Racing);
    }
}
