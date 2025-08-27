using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;
using EventData = ExitGames.Client.Photon.EventData;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class InGameManager : SimpleSingletonPun<InGameManager>, IOnEventCallback
{
    [Header("Configs")]
    [SerializeField] private InGameRaceRulesConfig rules;

    private const byte EV_COUNTDOWN_TICK = 10;
    private const byte EV_RACE_GO        = 11;

    public event Action<int> OnCountdownTick;
    public event Action OnRaceGo;
    public event Action<RaceState> OnStateChanged;

    private Coroutine _countdownCo;
    private bool _isMaster => PhotonNetwork.IsMasterClient;
    private Room _room => PhotonNetwork.CurrentRoom;

    // 키 헬퍼 (가독성)
    private static string K(RoomKey k) => PhotonNetworkCustomProperties.ToKeyString(k);
    private static string PK(PlayerKey k) => PhotonNetworkCustomProperties.ToKeyString(k);

    protected override void Init()
    {
        base.Init();

        // 로컬 플레이어 씬 상태 세팅
        PlayerSetting();

        // 초기 상태 동기화
        if (_room != null)
        {
            if (_isMaster && !_room.CustomProperties.ContainsKey(K(RoomKey.RaceState)))
            {
                SetRoomState(RaceState.WaitPlayer);
            }
            else
            {
                var state = GetRoomState();
                EnterState(state);
            }
        }
    }

    // 상태머신(초기/변경 시 진입)
    private void EnterState(RaceState state)
    {
        OnStateChanged?.Invoke(state);
        this.PrintLog($"MatchState: {state}");

        switch (state)
        {
            case RaceState.WaitPlayer:
                if (_isMaster) CheckAndSpawnAllIfReady();
                break;

            case RaceState.Countdown:
                StartCountdown();
                break;

            case RaceState.Racing:
                OnRaceGo?.Invoke();
                break;

            case RaceState.Finish:
                if (_isMaster) StartFinishWindow();
                break;

            case RaceState.PostGame:
                StartCoroutine(Co_PostGameExit());
                break;
        }
    }

    // ---- 상태 전환(마스터 전용) + 타임스탬프 세팅 ----
    private void SetRoomState(RaceState next)
    {
        if (!_isMaster || _room == null) return;

        var ht = new Hashtable { [K(RoomKey.RaceState)] = (int)next };

        switch (next)
        {
            case RaceState.Countdown:
                ht[K(RoomKey.CountdownStartTime)] = PhotonNetwork.Time;
                break;

            case RaceState.Racing:
                ht[K(RoomKey.RaceStartTime)] = PhotonNetwork.Time;
                break;

            case RaceState.Finish:
                ht[K(RoomKey.FinishStartTime)] = PhotonNetwork.Time;
                if (!_room.CustomProperties.ContainsKey(K(RoomKey.FinishCount)))
                    ht[K(RoomKey.FinishCount)] = 0;
                break;
        }

        _room.SetCustomProperties(ht);
    }

    // 룸 상태 조회
    private RaceState GetRoomState()
    {
        if (_room == null) return RaceState.WaitPlayer;
        if (_room.CustomProperties.TryGetValue(K(RoomKey.RoomState), out var v) && v is int i)
            return (RaceState)i;
        return RaceState.WaitPlayer;
    }

    // WaitPlayer 로직(마스터만): 전원 인게임 + 로드/스폰 완료 확인
    private void CheckAndSpawnAllIfReady()
    {
        if (!_isMaster || _room == null) return;

        foreach (var p in _room.Players.Values)
        {
            // InGameScene 도착 여부
            if (!(_room != null
                && p.CustomProperties.TryGetValue(PK(PlayerKey.CurrentScene), out var sceneVal)
                && sceneVal is int si && si == (int)SceneID.InGameScene))
                return;

            // 로드/스폰 완료 여부
            if (!(p.CustomProperties.TryGetValue(PK(PlayerKey.RaceLoaded), out var loadedVal)
                && loadedVal is bool loaded && loaded))
                return;
        }

        // 전원 준비 완료 > 카운트다운으로
        SetRoomState(RaceState.Countdown);
    }

    // Countdown
    private void StartCountdown()
    {
        if (_countdownCo != null) StopCoroutine(_countdownCo);
        _countdownCo = StartCoroutine(Co_Countdown());
    }

    private IEnumerator Co_Countdown()
    {
        double t0 = (_room.CustomProperties[K(RoomKey.CountdownStartTime)] as double?) ?? PhotonNetwork.Time;
        float seconds = Mathf.Max(1, rules.countdownSeconds);

        while (true)
        {
            double elapsed = PhotonNetwork.Time - t0;
            int left = Mathf.CeilToInt(seconds - (float)elapsed);

            if (left >= 0 && _isMaster)
            {
                PhotonNetwork.RaiseEvent(EV_COUNTDOWN_TICK, left,
                    new RaiseEventOptions { Receivers = ReceiverGroup.All },
                    SendOptions.SendUnreliable);
            }

            if (left <= 0) break;
            yield return null;
        }

        if (_isMaster)
        {
            PhotonNetwork.RaiseEvent(EV_RACE_GO, null,
                new RaiseEventOptions { Receivers = ReceiverGroup.All },
                SendOptions.SendReliable);

            SetRoomState(RaceState.Racing);
        }
    }

    // Racing 단계: 로컬 플레이어 완주 신고
    public void NotifyLocalPlayerFinished()
    {
        var me = PhotonNetwork.LocalPlayer;
        me.SetCustomProperties(new Hashtable { [PK(PlayerKey.RaceIsFinished)] = true });

        if (_isMaster)
        {
            int finished = GetFinishCount() + 1;
            _room.SetCustomProperties(new Hashtable { [K(RoomKey.FinishCount)] = finished });

            if (GetRoomState() == RaceState.Racing && finished >= 1)
                SetRoomState(RaceState.Finish);
        }
    }

    private int GetFinishCount()
    {
        if (_room.CustomProperties.TryGetValue(K(RoomKey.FinishCount), out var v) && v is int i)
            return i;
        return 0;
    }

    // Finish 윈도우
    private void StartFinishWindow()
    {
        if (!_isMaster) return;
        StartCoroutine(Co_FinishWindow());
    }

    private IEnumerator Co_FinishWindow()
    {
        double t0 = (_room.CustomProperties[K(RoomKey.FinishStartTime)] as double?) ?? PhotonNetwork.Time;
        float wait = Mathf.Max(1, rules.finishSeconds);

        while (true)
        {
            int playerCount = _room.PlayerCount;
            int finished = GetFinishCount();

            if (finished >= playerCount)
            {
                yield return new WaitForSeconds(2f);
                SetRoomState(RaceState.PostGame);
                yield break;
            }

            if (PhotonNetwork.Time - t0 >= wait)
            {
                yield return new WaitForSeconds(2f);
                SetRoomState(RaceState.PostGame);
                yield break;
            }

            yield return null;
        }
    }

    // PostGame
    private IEnumerator Co_PostGameExit()
    {
        yield return new WaitForSeconds(Mathf.Max(0.5f, rules.postGameSeconds));

        // 타이틀로 가기 전, 씬 상태를 Title로 갱신
        SetLocalPlayerScene(SceneID.TitleScene);

        PhotonNetwork.LoadLevel($"YSJ_{SceneID.TitleScene}");
    }

    #region PUN2 Overrides
    public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(target, changedProps);

        // 도착/로드 갱신은 대기 상태에서 스폰 체크 트리거
        if (_isMaster && (
            changedProps.ContainsKey(PK(PlayerKey.CurrentScene)) ||
            changedProps.ContainsKey(PK(PlayerKey.RaceLoaded))
        ))
        {
            CheckAndSpawnAllIfReady();
        }

        // 완주 갱신 → 카운트 및 상태 전이
        if (_isMaster && changedProps.ContainsKey(PK(PlayerKey.RaceIsFinished)))
        {
            int finished = 0;
            foreach (var p in _room.Players.Values)
            {
                if (p.CustomProperties.TryGetValue(PK(PlayerKey.RaceIsFinished), out var v) && v is bool b && b)
                    finished++;
            }
            _room.SetCustomProperties(new Hashtable { [K(RoomKey.FinishCount)] = finished });

            if (GetRoomState() == RaceState.Racing && finished >= 1)
                SetRoomState(RaceState.Finish);
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);

        if (propertiesThatChanged.ContainsKey(K(RoomKey.RoomState)))
        {
            var state = GetRoomState();
            EnterState(state);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        if (_isMaster && GetRoomState() == RaceState.WaitPlayer)
            CheckAndSpawnAllIfReady();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        if (_isMaster)
        {
            if (GetRoomState() == RaceState.WaitPlayer)
                CheckAndSpawnAllIfReady();

            if (GetRoomState() is RaceState.Finish or RaceState.Racing)
            {
                int finished = 0;
                foreach (var p in _room.Players.Values)
                {
                    if (p.CustomProperties.TryGetValue(PK(PlayerKey.RaceIsFinished), out var v) && v is bool b && b)
                        finished++;
                }
                _room.SetCustomProperties(new Hashtable { [K(RoomKey.FinishCount)] = finished });

                int playerCount = _room.PlayerCount;
                if (finished >= playerCount)
                    SetRoomState(RaceState.PostGame);
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
    private void SetLocalPlayerScene(SceneID sceneId)
    {
        PhotonNetworkCustomProperties.SetLocalPlayerProp(PlayerKey.CurrentScene, (int)sceneId);
    }

    private void PlayerSetting()
    {
        this.PrintLog("플레이어 데이터 세팅");
        SetLocalPlayerScene(SceneID.InGameScene);
    }
    #endregion
}
