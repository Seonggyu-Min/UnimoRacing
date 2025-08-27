using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections.Generic;

public static class PhotonNetworkCustomProperties
{
    #region ROOM
    // ROOM
    public const string KEY_ROOM_STATE_TYPE                     = "room_roomStateType";             // 룸 상태 타입

    // MATCH
    public const string KEY_MATCH_FULL_FLAG                     = "room_fullNotified";              // 방이 다 찼음을 알림
    public const string KEY_MATCH_READY_CHECK_START_TIME        = "room_matchReadyCheckStartTime";  // 매치 레디 체크 시작 시간

    public const string KEY_MATCH_CHOOSABLE_MAP_COUNT           = "room_raceChoosableMapCount";     // 선택 가능한 맵의 갯수
    public const string KEY_MATCH_RACE_MAP_ID                   = "room_raceMapId";                 // 선택된 맵 ID

    // RACE
    public const string KEY_RACE_STATE_TYPE                     = "room_raceStateType";             // 레이싱 상태 타입
    public const string KEY_RACE_COUNTDOWN_START_TIME           = "room_countStartTime";            // 카운트다운 시작(PhotonServerTime)
    public const string KEY_RACE_START_TIME                     = "room_raceStartTime";             // 레이싱 시작(PhotonServerTime)
    public const string KEY_RACE_FINISH_START_TIME              = "room_finishStartTime";           // 피니시 시작(PhotonServerTime)
    public const string KEY_RACE_FINISH_COUNT                   = "room_finishCount";               // 완주 인원 수

    // DEFAULT VALUE
    public const int VALUE_ROOM_DEFAULT_RACE_MAP_ID             = 0;
    public const int VALUE_ROOM_NOT_CHOSEN_RACE_MAP_ID          = -1;

    #endregion

    #region PLAYER
    // SERVER
    public const string KEY_PLAYER_LEVEL                        = "player_level";                   // 플레이어 레벨
    public const string KEY_PLAYER_EXP                          = "player_exp";                     // 플레이어 경험치

    // LOCAL
    public const string KEY_PLAYER_CAR_ID                       = "player_carId";                   // 선택 카트 ID
    public const string KEY_PLAYER_CHARACTER_ID                 = "player_characterId";             // 선택 캐릭터 ID
    public const string KEY_PLAYER_HOPERACEMAP_ID               = "player_HopeRaceMapId";           // 희망 맵 ID

    public const string KEY_PLAYER_MATCH_READY                  = "player_match_ready";             // 매치 준비 여부

    public const string KEY_PLAYER_RACE_LOADED                  = "player_race_loaded";             // 레이싱 로드 여부
    public const string KEY_PLAYER_RACE_IS_FINISHED             = "player_race_isFinished";         // 개인 레이싱 완료 여부
    public const string KEY_PLAYER_RACE_FINISHED_TIME           = "player_race_finishedTime";       // 개인 레이싱 완료 시간

    public const string KEY_PLAYER_CURRENT_SCENE                = "player_currentSceneId";          // 현재 씬 (int SceneID)

    public const int VALUE_PLAYER_DEFAULT_LEVEL                 = 1;
    public const int VALUE_PLAYER_DEFAULT_EXP                   = 0;

    public const int VALUE_PLAYER_DEFAULT_CAR_ID                = 0;
    public const int VALUE_PLAYER_DEFAULT_CHARACTER_ID          = 0;
    public const int VALUE_PLAYER_DEFAULT_HOPERACEMAP_ID        = 0;

    #endregion

    #region Mapping

    public static string ToKeyString(RoomKey key) => key switch
    {
        RoomKey.RoomState => KEY_ROOM_STATE_TYPE,

        // Match
        RoomKey.MatchFullFlag => KEY_MATCH_FULL_FLAG,
        RoomKey.MatchReadyCheckStartTime => KEY_MATCH_READY_CHECK_START_TIME,
        RoomKey.MatchChoosableMapCount => KEY_MATCH_CHOOSABLE_MAP_COUNT,
        RoomKey.MatchRaceMapId => KEY_MATCH_RACE_MAP_ID,

        // Race
        RoomKey.RaceState => KEY_RACE_STATE_TYPE,
        RoomKey.CountdownStartTime => KEY_RACE_COUNTDOWN_START_TIME,
        RoomKey.RaceStartTime => KEY_RACE_START_TIME,
        RoomKey.FinishStartTime => KEY_RACE_FINISH_START_TIME,
        RoomKey.FinishCount => KEY_RACE_FINISH_COUNT,

        _ => key.ToString()
    };

    public static string ToKeyString(PlayerKey key) => key switch
    {
        PlayerKey.Level => KEY_PLAYER_LEVEL,
        PlayerKey.Exp => KEY_PLAYER_EXP,

        PlayerKey.CarId => KEY_PLAYER_CAR_ID,
        PlayerKey.CharacterId => KEY_PLAYER_CHARACTER_ID,
        PlayerKey.HopeRaceMapId => KEY_PLAYER_HOPERACEMAP_ID,

        PlayerKey.MatchReady => KEY_PLAYER_MATCH_READY,

        PlayerKey.RaceLoaded => KEY_PLAYER_RACE_LOADED,
        PlayerKey.RaceIsFinished => KEY_PLAYER_RACE_IS_FINISHED,
        PlayerKey.RaceFinishedTime => KEY_PLAYER_RACE_FINISHED_TIME,

        PlayerKey.CurrentScene => KEY_PLAYER_CURRENT_SCENE,

        _ => key.ToString()
    };

    #endregion

    // ============================
    // ========== HELPER ==========
    // ============================

    #region Guard & Util
    private static void EnsureInRoom()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            throw new InvalidOperationException("현재 룸이 아님(PhotonNetwork.InRoom == false).");
    }

    private static Hashtable MakeTable(string key, object value)
    {
        var ht = new Hashtable { { key, value } };
        return ht;
    }

    private static Hashtable MakeTable(IDictionary<string, object> kv)
    {
        var ht = new Hashtable();
        foreach (var p in kv)
            ht[p.Key] = p.Value;
        return ht;
    }
    #endregion

    // ============================
    // ======== ROOM PROPS ========
    // ============================

    #region Room Get
    /// <summary>룸 커스텀 프로퍼티 조회 (존재 안 하면 defaultValue 반환)</summary>
    public static T GetRoomProp<T>(RoomKey key, T defaultValue = default)
    {
        EnsureInRoom();
        var sKey = ToKeyString(key);
        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(sKey, out var raw) &&
            raw is T t) return t;

        // 숫자 캐스팅 보정 (byte/int/long/float -> 원하는 T)
        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(sKey, out raw))
        {
            try
            {
                if (raw is IConvertible) return (T)Convert.ChangeType(raw, typeof(T));
            }
            catch { /* 무시 */ }
        }

        return defaultValue;
    }

    /// <summary>룸 커스텀 프로퍼티 TryGet</summary>
    public static bool TryGetRoomProp<T>(RoomKey key, out T value)
    {
        EnsureInRoom();
        var sKey = ToKeyString(key);
        value = default;
        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(sKey, out var raw))
        {
            if (raw is T t) { value = t; return true; }
            try
            {
                if (raw is IConvertible)
                {
                    value = (T)Convert.ChangeType(raw, typeof(T));
                    return true;
                }
            }
            catch { /* 무시 */ }
        }
        return false;
    }
    #endregion

    #region Room Set
    /// <summary>룸 커스텀 프로퍼티 설정 (단일 키)</summary>
    public static bool SetRoomProp(RoomKey key, object value, Hashtable expected = null, WebFlags webFlags = null)
    {
        EnsureInRoom();
        var sKey = ToKeyString(key);
        return PhotonNetwork.CurrentRoom.SetCustomProperties(MakeTable(sKey, value), expected, webFlags);
    }

    /// <summary>룸 커스텀 프로퍼티 설정 (여러 키 묶음)</summary>
    public static bool SetRoomProps(IDictionary<RoomKey, object> values, IDictionary<RoomKey, object> expected = null, WebFlags webFlags = null)
    {
        EnsureInRoom();
        var toSet = new Dictionary<string, object>();
        foreach (var kv in values) toSet[ToKeyString(kv.Key)] = kv.Value;

        Hashtable expectedTable = null;
        if (expected != null)
        {
            expectedTable = new Hashtable();
            foreach (var kv in expected) expectedTable[ToKeyString(kv.Key)] = kv.Value;
        }

        return PhotonNetwork.CurrentRoom.SetCustomProperties(MakeTable(toSet), expectedTable, webFlags);
    }

    /// <summary>
    /// CAS(Compare-And-Swap)로 값 바꾸기.
    /// 예) 특정 값일 때만 업데이트하고 싶을 때 사용.
    /// </summary>
    public static bool CompareExchangeRoomProp(RoomKey key, object newValue, object expectedValue, WebFlags webFlags = null)
    {
        EnsureInRoom();
        var sKey = ToKeyString(key);
        var set = MakeTable(sKey, newValue);
        var expected = MakeTable(sKey, expectedValue);
        return PhotonNetwork.CurrentRoom.SetCustomProperties(set, expected, webFlags);
    }
    #endregion

    // ============================
    // ======= PLAYER PROPS =======
    // ============================

    #region Player Get
    /// <summary>플레이어 커스텀 프로퍼티 조회 (대상 지정, 없으면 defaultValue)</summary>
    public static T GetPlayerProp<T>(Player player, PlayerKey key, T defaultValue = default)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        var sKey = ToKeyString(key);

        if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(sKey, out var raw) &&
            raw is T t) return t;

        if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(sKey, out raw))
        {
            try
            {
                if (raw is IConvertible) return (T)Convert.ChangeType(raw, typeof(T));
            }
            catch { /* 무시 */ }
        }

        return defaultValue;
    }

    /// <summary>로컬 플레이어 커스텀 프로퍼티 조회</summary>
    public static T GetLocalPlayerProp<T>(PlayerKey key, T defaultValue = default)
        => GetPlayerProp(PhotonNetwork.LocalPlayer, key, defaultValue);

    public static bool TryGetPlayerProp<T>(Player player, PlayerKey key, out T value)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        var sKey = ToKeyString(key);
        value = default;
        if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(sKey, out var raw))
        {
            if (raw is T t) { value = t; return true; }
            try
            {
                if (raw is IConvertible)
                {
                    value = (T)Convert.ChangeType(raw, typeof(T));
                    return true;
                }
            }
            catch { /* 무시 */ }
        }
        return false;
    }
    #endregion

    #region Player Set
    /// <summary>플레이어 커스텀 프로퍼티 설정 (대상 지정, 단일 키)</summary>
    public static bool SetPlayerProp(Player player, PlayerKey key, object value, Hashtable expected = null, WebFlags webFlags = null)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        var sKey = ToKeyString(key);
        return player.SetCustomProperties(MakeTable(sKey, value), expected, webFlags);
    }

    /// <summary>로컬 플레이어 커스텀 프로퍼티 설정 (단일 키)</summary>
    public static bool SetLocalPlayerProp(PlayerKey key, object value, Hashtable expected = null, WebFlags webFlags = null)
        => SetPlayerProp(PhotonNetwork.LocalPlayer, key, value, expected, webFlags);

    /// <summary>플레이어 커스텀 프로퍼티 설정 (여러 키 묶음)</summary>
    public static bool SetPlayerProps(Player player, IDictionary<PlayerKey, object> values, IDictionary<PlayerKey, object> expected = null, WebFlags webFlags = null)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));

        var toSet = new Dictionary<string, object>();
        foreach (var kv in values) toSet[ToKeyString(kv.Key)] = kv.Value;

        Hashtable expectedTable = null;
        if (expected != null)
        {
            expectedTable = new Hashtable();
            foreach (var kv in expected) expectedTable[ToKeyString(kv.Key)] = kv.Value;
        }

        return player.SetCustomProperties(MakeTable(toSet), expectedTable, webFlags);
    }

    /// <summary>CAS(Compare-And-Swap)로 플레이어 값 바꾸기</summary>
    public static bool CompareExchangePlayerProp(Player player, PlayerKey key, object newValue, object expectedValue, WebFlags webFlags = null)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        var sKey = ToKeyString(key);
        var set = MakeTable(sKey, newValue);
        var expected = MakeTable(sKey, expectedValue);
        return player.SetCustomProperties(set, expected, webFlags);
    }
    #endregion
}
