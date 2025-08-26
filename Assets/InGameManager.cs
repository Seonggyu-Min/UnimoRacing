using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;
using YSJ.Util;
using EventData = ExitGames.Client.Photon.EventData;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public enum MatchState { WaitPlayer, Countdown, Racing, Finish, PostGame }

public class InGameManager : SimpleSingletonPun<InGameManager>, IOnEventCallback
{
    [Header("Configs")]
    [SerializeField] private GameRulesSO rules;

    private const byte EV_COUNTDOWN_TICK = 10;
    private const byte EV_RACE_GO        = 11;

    public event Action<int> OnCountdownTick;
    public event Action OnRaceGo;
    public event Action<MatchState> OnStateChanged; // 상태에 따른 액션 해줘야 될것들(카드, UI 등등등)

    private Coroutine _countdownCo;
    private bool _isMaster => PhotonNetwork.IsMasterClient;
    private Room _room => PhotonNetwork.CurrentRoom;

    #region Key Maping
    private enum RoomKey
    {
        RoomState,
        CountdownStartTime,
        RaceStartTime,
        FinishStartTime,
        FinishCount,
    }

    private enum PlayerKey
    {
        CurrentScene,
        RaceLoaded,
        RaceIsFinished,
        CarId,
        CharacterId,
    }

    private static string ToKeyString(RoomKey key) => key switch
    {
        RoomKey.RoomState               => PhotonNetworkCustomProperties.KEY_ROOM_STATE_TYPE,
        RoomKey.CountdownStartTime      => PhotonNetworkCustomProperties.KEY_ROOM_COUNTDOWN_START_TIME,
        RoomKey.RaceStartTime           => PhotonNetworkCustomProperties.KEY_ROOM_RACE_START_TIME,
        RoomKey.FinishStartTime         => PhotonNetworkCustomProperties.KEY_ROOM_FINISH_START_TIME,
        RoomKey.FinishCount             => PhotonNetworkCustomProperties.KEY_ROOM_FINISH_COUNT,
        _ => key.ToString()
    };

    private static string ToKeyString(PlayerKey key) => key switch
    {
        PlayerKey.CurrentScene      => PhotonNetworkCustomProperties.KEY_PLAYER_CURRENT_SCENE,
        PlayerKey.RaceLoaded        => PhotonNetworkCustomProperties.KEY_PLAYER_RACE_LOADED,
        PlayerKey.RaceIsFinished    => PhotonNetworkCustomProperties.KEY_PLAYER_RACE_IS_FINISHED,
        PlayerKey.CarId             => PhotonNetworkCustomProperties.KEY_PLAYER_CAR_ID,
        PlayerKey.CharacterId       => PhotonNetworkCustomProperties.KEY_PLAYER_CHARACTER_ID,
        _ => key.ToString()
    };
    #endregion

    protected override void Init()
    {
        base.Init();
        PhotonNetwork.AddCallbackTarget(this);

        SetLocalPlayerScene(SceneID.InGameScene);

        if (_room != null)
        {
            if (_isMaster && !HasKey(_room.CustomProperties, ToKeyString(RoomKey.RoomState)))
            {
                SetRoomState(MatchState.WaitPlayer);
            }
            else
            {
                var state = GetRoomState();
                EnterState(state);
            }
        }
    }

    private void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    // 상태머신
    private void EnterState(MatchState state)
    {
        OnStateChanged?.Invoke(state);

        switch (state)
        {
            case MatchState.WaitPlayer:
                if (_isMaster) CheckAndSpawnAllIfReady();
                break;

            case MatchState.Countdown:
                StartCountdown();
                break;

            case MatchState.Racing:
                OnRaceGo?.Invoke();
                break;

            case MatchState.Finish:
                if (_isMaster) StartFinishWindow();
                break;

            case MatchState.PostGame:
                StartCoroutine(Co_PostGameExit());
                break;
        }
    }

    // 상태 전환 > 마스터용
    // 상태에 따른 시간 처리용
    private void SetRoomState(MatchState next)
    {
        if (!_isMaster || _room == null) return;

        var ht = new Hashtable { [ToKeyString(RoomKey.RoomState)] = (int)next };
        switch (next)
        {
            case MatchState.Countdown:
                ht[ToKeyString(RoomKey.CountdownStartTime)] = PhotonNetwork.Time;
                break;

            case MatchState.Racing:
                ht[ToKeyString(RoomKey.RaceStartTime)] = PhotonNetwork.Time;
                break;

            case MatchState.Finish:
                ht[ToKeyString(RoomKey.FinishStartTime)] = PhotonNetwork.Time;
                if (!HasKey(_room.CustomProperties, ToKeyString(RoomKey.FinishCount)))
                    ht[ToKeyString(RoomKey.FinishCount)] = 0;
                break;
        }
        _room.SetCustomProperties(ht);
    }

    private MatchState GetRoomState()
    {
        if (_room == null) return MatchState.WaitPlayer;
        if (_room.CustomProperties.TryGetValue(ToKeyString(RoomKey.RoomState), out var v) && v is int i)
            return (MatchState)i;
        return MatchState.WaitPlayer;
    }

    // WaitPlayer 로직
    private void CheckAndSpawnAllIfReady()
    {
        if (!_isMaster) return;

        foreach (var p in _room.Players.Values)
        {
            // 모든 플레이어가 InGameScene에 들어왔는지
            if (!p.CustomProperties.TryGetValue(ToKeyString(PlayerKey.CurrentScene), out var sceneVal) 
                || !(sceneVal is int si) || si != (int)SceneID.InGameScene)
                return;

            // 그리고 스폰/세팅까지 완료했는지
            if (!(p.CustomProperties.TryGetValue(ToKeyString(PlayerKey.RaceLoaded), out var v) && v is bool b && b))
                return;
        }

        // 전원 도착 + 로드/스폰 완료 → 카운트다운
        SetRoomState(MatchState.Countdown);
    }

    // Countdown 로직
    private void StartCountdown()
    {
        if (_countdownCo != null) StopCoroutine(_countdownCo);
        _countdownCo = StartCoroutine(Co_Countdown());
    }

    private IEnumerator Co_Countdown()
    {
        double t0 =
            (_room.CustomProperties[ToKeyString(RoomKey.CountdownStartTime)] as double?)
            ?? PhotonNetwork.Time;

        float seconds = Mathf.Max(1, rules.countdownSeconds);

        while (true)
        {
            double elapsed = PhotonNetwork.Time - t0;
            float left = seconds - Mathf.FloorToInt((float)elapsed);

            if (left >= 0 && _isMaster)
            {
                var reo = new RaiseEventOptions { Receivers = ReceiverGroup.All };
                PhotonNetwork.RaiseEvent(EV_COUNTDOWN_TICK, left, reo, SendOptions.SendUnreliable);
            }

            if (left <= 0) break;
            yield return null;
        }

        if (_isMaster)
        {
            PhotonNetwork.RaiseEvent(EV_RACE_GO, null, new RaiseEventOptions { Receivers = ReceiverGroup.All }, SendOptions.SendReliable);
            SetRoomState(MatchState.Racing);
        }
    }

    // Racing 단계 로직
    public void NotifyLocalPlayerFinished()
    {
        var me = PhotonNetwork.LocalPlayer;
        var pp = new Hashtable { [ToKeyString(PlayerKey.RaceIsFinished)] = true };
        me.SetCustomProperties(pp);

        if (_isMaster)
        {
            int finished = GetFinishCount() + 1;
            _room.SetCustomProperties(new Hashtable { [ToKeyString(RoomKey.FinishCount)] = finished });

            if (GetRoomState() == MatchState.Racing && finished >= 1)
            {
                SetRoomState(MatchState.Finish);
            }
        }
    }

    private int GetFinishCount()
    {
        if (_room.CustomProperties.TryGetValue(ToKeyString(RoomKey.FinishCount), out var v) && v is int i)
            return i;
        return 0;
    }

    // Finish 로직
    private void StartFinishWindow()
    {
        if (!_isMaster) return;
        StartCoroutine(Co_FinishWindow());
    }

    private IEnumerator Co_FinishWindow()
    {
        double t0 =
            (_room.CustomProperties[ToKeyString(RoomKey.FinishStartTime)] as double?)
            ?? PhotonNetwork.Time;

        float wait = Mathf.Max(1, rules.finishSeconds);

        while (true)
        {
            int playerCount = _room.PlayerCount;
            int finished = GetFinishCount();

            if (finished >= playerCount)
            {
                yield return new WaitForSeconds(2f);
                SetRoomState(MatchState.PostGame);
                yield break;
            }

            if (PhotonNetwork.Time - t0 >= wait)
            {
                yield return new WaitForSeconds(2f);
                SetRoomState(MatchState.PostGame);
                yield break;
            }

            yield return null;
        }
    }

    // PostGame 로직
    private IEnumerator Co_PostGameExit()
    {
        yield return new WaitForSeconds(Mathf.Max(0.5f, rules.postGameSeconds));

        // 타이틀로 가기 전, 씬 상태를 Title로 갱신
        SetLocalPlayerScene(SceneID.TitleScene);

        PhotonNetwork.LoadLevel($"YSJ_{SceneID.TitleScene.ToString()}"); // 테스터용
    }

    #region 
    // 플레이어/룸 프로퍼티 변경
    public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(target, changedProps);

        //  씬 도착/변경도 대기 체크 트리거
        if (_isMaster && (
            changedProps.ContainsKey(ToKeyString(PlayerKey.CurrentScene)) ||
            changedProps.ContainsKey(ToKeyString(PlayerKey.RaceLoaded))
        ))
        {
            CheckAndSpawnAllIfReady();
        }

        if (_isMaster && changedProps.ContainsKey(ToKeyString(PlayerKey.RaceIsFinished)))
        {
            int finished = 0;
            foreach (var p in _room.Players.Values)
            {
                if (p.CustomProperties.TryGetValue(ToKeyString(PlayerKey.RaceIsFinished), out var v) && v is bool b && b)
                    finished++;
            }
            _room.SetCustomProperties(new Hashtable { [ToKeyString(RoomKey.FinishCount)] = finished });

            if (GetRoomState() == MatchState.Racing && finished >= 1)
                SetRoomState(MatchState.Finish);
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        if (propertiesThatChanged.ContainsKey(ToKeyString(RoomKey.RoomState)))
        {
            var state = GetRoomState();
            EnterState(state);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (_isMaster && GetRoomState() == MatchState.WaitPlayer)
            CheckAndSpawnAllIfReady();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        if (_isMaster)
        {
            if (GetRoomState() == MatchState.WaitPlayer)
                CheckAndSpawnAllIfReady();

            if (GetRoomState() == MatchState.Finish || GetRoomState() == MatchState.Racing)
            {
                int finished = 0;
                foreach (var p in _room.Players.Values)
                {
                    if (p.CustomProperties.TryGetValue(ToKeyString(PlayerKey.RaceIsFinished), out var v) && v is bool b && b)
                        finished++;
                }
                _room.SetCustomProperties(new Hashtable { [ToKeyString(RoomKey.FinishCount)] = finished });

                int playerCount = _room.PlayerCount;
                if (finished >= playerCount)
                {
                    SetRoomState(MatchState.PostGame);
                }
            }
        }
    }
    #endregion

    // RaiseEvent 수신(UI 싱크용)
    public void OnEvent(EventData photonEvent)
    {
        switch (photonEvent.Code)
        {
            case EV_COUNTDOWN_TICK:
                int left = (int)photonEvent.CustomData;
                OnCountdownTick?.Invoke(left);
                break;

            case EV_RACE_GO:
                OnRaceGo?.Invoke();
                break;
        }
    }

    #region Util
    private bool HasKey(Hashtable ht, string key)
    {
        return ht != null && ht.ContainsKey(key);
    }

    // 로컬 플레이어의 현재 씬 상태 주입(이걸로 현재 씬으로 넘어와 로드가 된 상태인지 파악)
    private void SetLocalPlayerScene(SceneID sceneId)
    {
        var ht = new Hashtable { [ToKeyString(PlayerKey.CurrentScene)] = (int)sceneId };
        PhotonNetwork.LocalPlayer.SetCustomProperties(ht);
    }
    #endregion
}
