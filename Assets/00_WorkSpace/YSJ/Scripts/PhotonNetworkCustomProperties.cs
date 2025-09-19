using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using YSJ.Util;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class PhotonNetworkCustomProperties
{
    #region ROOM
    // ROOM
    public const string KEY_ROOM_STATE_TYPE                     = "room_roomStateType";             // 룸 상태 타입                       포톤

    // MATCH
    public const string KEY_MATCH_FULL_FLAG                     = "room_fullNotified";              // 방이 다 찼음을 알림                 포톤
    public const string KEY_MATCH_READY_CHECK_START_TIME        = "room_matchReadyCheckStartTime";  // 매치 레디 체크 시작 시간            필요없음(테스트용)  

    public const string KEY_MATCH_CHOOSABLE_MAP_COUNT           = "room_raceChoosableMapCount";     // 선택 가능한 맵의 갯수               포톤
    public const string KEY_MATCH_RACE_MAP_ID                   = "room_raceMapId";                 // 선택된 맵 ID                       포톤

    // RACE
    public const string KEY_RACE_STATE_TYPE                     = "room_raceStateType";             // 레이싱 상태 타입                    포톤
    public const string KEY_RACE_COUNTDOWN_START_TIME           = "room_countStartTime";            // 카운트다운 시작(PhotonServerTime)   포톤
    public const string KEY_RACE_START_TIME                     = "room_raceStartTime";             // 레이싱 시작(PhotonServerTime)       포톤
    public const string KEY_RACE_FINISH_START_TIME              = "room_finishStartTime";           // 피니시 시작(PhotonServerTime)       포톤
    public const string KEY_RACE_FINISH_END_TIME                = "room_finishEndTime";           // 피니시 끝(PhotonServerTime)          포톤
    public const string KEY_RACE_FINISH_COUNT                   = "room_finishCount";               // 완주 인원 수                        포톤

    // DEFAULT VALUE
    public const int VALUE_ROOM_DEFAULT_RACE_MAP_ID             = 0;
    public const int VALUE_ROOM_NOT_CHOSEN_RACE_MAP_ID          = -1;

    // VOTE
    public const string KEY_ROOM_VOTE_STATE                     = "vote_state";                     // 투표 중인지 여부
    public const string KEY_ROOM_VOTE_END_AT                    = "vote_endAt";                    // 투표 종료 시간 (PhotonNetwork.Time)
    public const string KEY_VOTE_WINNER_INDEX                   = "vote_winner_index";              // 방에서 투표로 선정된 맵의 인덱스를 저장할 키

    #endregion

    #region PLAYER
    // SERVER
    public const string KEY_PLAYER_LEVEL                        = "player_level";                   // 플레이어 레벨                      서버
    public const string KEY_PLAYER_EXP                          = "player_exp";                     // 플레이어 경험치                     서버

    // LOCAL
    public const string KEY_PLAYER_CAR_ID                       = "player_carId";                   // 선택 카트 ID                      서버
    public const string KEY_PLAYER_CHARACTER_ID                 = "player_characterId";             // 선택 캐릭터 ID                    서버
    public const string KEY_PLAYER_HOPERACEMAP_ID               = "player_HopeRaceMapId";           // 희망 맵 ID                        포톤

    public const string KEY_PLAYER_MATCH_READY                  = "player_match_ready";             // 매치 준비 여부                     필요 없음(테스트용)

    public const string KEY_PLAYER_RACE_LOADED                  = "player_race_loaded";             // 레이싱 로드 여부                    포톤
    public const string KEY_PLAYER_RACE_IS_FINISHED             = "player_race_isFinished";         // 개인 레이싱 완료 여부               포톤
    public const string KEY_PLAYER_RACE_FINISHED_TIME           = "player_race_finishedTime";       // 개인 레이싱 완료 시간               포톤 

    public const string KEY_PLAYER_CURRENT_SCENE                = "player_currentSceneId";          // 현재 씬 (int SceneID)              포톤

    public const int VALUE_PLAYER_DEFAULT_LEVEL                 = 1;
    public const int VALUE_PLAYER_DEFAULT_EXP                   = 0;

    public const int VALUE_PLAYER_DEFAULT_KART_ID               = 0;
    public const int VALUE_PLAYER_DEFAULT_CHARACTER_ID          = 0;
    public const int VALUE_PLAYER_DEFAULT_HOPERACEMAP_ID        = 0;

    // VOTE
    public const string KEY_VOTE_MAP                            = "vote_map";                       // 플레이어 개인이 투표한 맵의 인덱스를 저장할 키

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
        RoomKey.FinishEndTime => KEY_RACE_FINISH_END_TIME,
        RoomKey.FinishCount => KEY_RACE_FINISH_COUNT,

        // Vote
        RoomKey.WinnerMapIndex => KEY_VOTE_WINNER_INDEX,
        RoomKey.VoteState => KEY_ROOM_VOTE_STATE,
        RoomKey.VoteEndTime => KEY_ROOM_VOTE_END_AT,

        _ => key.ToString()
    };

    public static string ToKeyString(PlayerKey key) => key switch
    {
        PlayerKey.Level => KEY_PLAYER_LEVEL,
        PlayerKey.Exp => KEY_PLAYER_EXP,

        PlayerKey.KartId => KEY_PLAYER_CAR_ID,
        PlayerKey.CharacterId => KEY_PLAYER_CHARACTER_ID,
        PlayerKey.HopeRaceMapId => KEY_PLAYER_HOPERACEMAP_ID,

        PlayerKey.MatchReady => KEY_PLAYER_MATCH_READY,

        PlayerKey.RaceLoaded => KEY_PLAYER_RACE_LOADED,
        PlayerKey.RaceIsFinished => KEY_PLAYER_RACE_IS_FINISHED,
        PlayerKey.RaceFinishedTime => KEY_PLAYER_RACE_FINISHED_TIME,

        PlayerKey.CurrentScene => KEY_PLAYER_CURRENT_SCENE,

        // VOTE
        PlayerKey.VotedMap => KEY_VOTE_MAP,

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
    public static T GetRoomProp<T>(RoomKey key, T defaultValue = default, Action onSuccess = null, Action onError = null)
    {
        EnsureInRoom();
        var sKey = ToKeyString(key);
        // 특정 타입으로 캐스팅 보정
        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(sKey, out var raw) &&
            raw is T t)
        {
            onSuccess?.Invoke();
            return t;
        }

        // 숫자 캐스팅 보정 (byte/int/long/float -> 원하는 T)
        if (PhotonNetwork.CurrentRoom.CustomProperties != null &&
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue(sKey, out raw))
        {
            try
            {
                if (raw is IConvertible)
                {
                    onSuccess?.Invoke();
                    return (T)Convert.ChangeType(raw, typeof(T));
                }
            }
            catch { /* 무시 */ }
        }

        onError?.Invoke();
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

        // 그냥 기본형태의 클래스 등의 타입으로만 형변환 됨. Enum은 안됨
        /*if (player.CustomProperties != null &&
            player.CustomProperties.TryGetValue(sKey, out var raw) &&
            raw is T t) return t;*/

        if (player.CustomProperties != null &&
        player.CustomProperties.TryGetValue(sKey, out var raw))
        {
            if (raw is T t) return t;

            try
            {
                var target = typeof(T);
                if (target.IsEnum)
                {
                    if (raw is string es) return (T)Enum.Parse(target, es, ignoreCase: true);   // 문자 Enum 변환
                    if (raw is IConvertible) return (T)Enum.ToObject(target, raw);              // 오젝 Enum 변환
                }

                if (raw is IConvertible) return (T)Convert.ChangeType(raw, typeof(T));          // 숫자 Enum 변환
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
                var target = typeof(T);
                if (target.IsEnum)
                {
                    if (raw is string es) { value = (T)Enum.Parse(target, es, true); return true; }
                    if (raw is IConvertible) { value = (T)Enum.ToObject(target, raw); return true; }
                }
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

    // ============================
    // = ROOM STATE SETTING PROPS =
    // ============================

    #region Room Setting
    public static void RoomNoneStateSetting()
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        // 방이 없어질 때, 초기화 되는 값 상정(null, 서버에서 값을 날리는 것임)
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            { RoomKey.RoomState,                    null                        },

            // Match
            { RoomKey.MatchFullFlag,                null                        },
            { RoomKey.MatchReadyCheckStartTime,     null                        },
            { RoomKey.MatchChoosableMapCount,       null                        },
            { RoomKey.MatchRaceMapId,               null                        },
                                                    
            // Race                                 
            { RoomKey.RaceState,                    null                        },
            { RoomKey.CountdownStartTime,           null                        },
            { RoomKey.RaceStartTime,                null                        },
            { RoomKey.FinishStartTime,              null                        },
            { RoomKey.FinishEndTime,                null                        },
            { RoomKey.FinishCount,                  null                        },
        });
    }

    public static void RoomWaitPlayerStateSetting(int raceChoosableMapCount = 2)
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        // 매칭 되어 플레이어들 기다릴 때(막 룸이 만들어졌을 때), 값 상정
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            { RoomKey.RoomState,                    RoomState.WaitPlayer                },

            // Match
            { RoomKey.MatchFullFlag,                false                               },
            { RoomKey.MatchReadyCheckStartTime,     -1                                  },
            { RoomKey.MatchChoosableMapCount,       raceChoosableMapCount               },
            { RoomKey.MatchRaceMapId,               -1                                  },

            // Race
            { RoomKey.RaceState,                    RaceState.None                      },
            { RoomKey.CountdownStartTime,           -1                                  },
            { RoomKey.RaceStartTime,                -1                                  },
            { RoomKey.FinishStartTime,              -1                                  },
            { RoomKey.FinishEndTime,                -1                                          },
            { RoomKey.FinishCount,                  -1                                  },
        });
    }

    public static void RoomMatchReadyStateSetting(int MatchRaceMapId = -1)
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        // 매칭해서 다음 씬 넘어가기 전, 투표시간 상정
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            { RoomKey.RoomState,                    RoomState.MatchReady                },

            // Match
            { RoomKey.MatchFullFlag,                true                                },
            { RoomKey.MatchReadyCheckStartTime,     PhotonNetwork.Time                  },
            // { RoomKey.MatchChoosableMapCount,       raceChoosableMapCount               },
            { RoomKey.MatchRaceMapId,               MatchRaceMapId                      },

            // Race
            { RoomKey.RaceState,                    RaceState.None                      },
            { RoomKey.CountdownStartTime,           -1                                  },
            { RoomKey.RaceStartTime,                -1                                  },
            { RoomKey.FinishStartTime,              -1                                  },
            { RoomKey.FinishEndTime,                -1                                          },
            { RoomKey.FinishCount,                  -1                                  },
        });
    }

    public static void RoomRaceStateSetting()
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        // 레이싱으로 씬이 넘어가는 상태, 상정 값
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            { RoomKey.RoomState,                    RoomState.Race                              },

            // Match
            // { RoomKey.MatchFullFlag,                true                                        },
            // { RoomKey.MatchReadyCheckStartTime,     PhotonNetwork.Time                          }, // 이전 상태에서 처리
            // { RoomKey.MatchChoosableMapCount,       raceGameConfig.RaceChoosableMapCount        }, // 이전 상태에서 처리
            // { RoomKey.MatchRaceMapId,               MatchRaceMapId                              },  // 무조건 -1이 아니어야됨.(Test 때는 상관 없음)

            // Race
            { RoomKey.RaceState,                    RaceState.None                              },  // InGameManager 가 자신 초기화 되면서 처리할 거임
            { RoomKey.CountdownStartTime,           -1                                          },
            { RoomKey.RaceStartTime,                -1                                          },
            { RoomKey.FinishStartTime,              -1                                          },
            { RoomKey.FinishEndTime,                -1                                          },
            { RoomKey.FinishCount,                  -1                                          },
        });
    }

    public static void RaceWaitPlayerSetting()
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        // 레이싱으로 씬이 넘어가서 레이싱 시작 전 다른 플레이어 기다리는 중
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            { RoomKey.RoomState,                    RoomState.Race                              },

            // Match
            // { RoomKey.MatchFullFlag,                true                                        },
            // { RoomKey.MatchReadyCheckStartTime,     PhotonNetwork.Time                          }, // 이전 상태에서 처리
            // { RoomKey.MatchChoosableMapCount,       raceGameConfig.RaceChoosableMapCount        }, // 이전 상태에서 처리
            // { RoomKey.MatchRaceMapId,               MatchRaceMapId                              },  // 무조건 -1이 아니어야됨.(Test 때는 상관 없음)

            // Race
            { RoomKey.RaceState,                    RaceState.WaitPlayer                        },
            { RoomKey.CountdownStartTime,           -1                                          },
            { RoomKey.RaceStartTime,                -1                                          },
            { RoomKey.FinishStartTime,              -1                                          },
            { RoomKey.FinishEndTime,                -1                                          },
            { RoomKey.FinishCount,                  -1                                          },
        });
    }

    public static void RaceLoadPlayersSetting()
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        // 다른 플레이어들 다 들어오고 소환 중
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            // { RoomKey.RoomState,                    RoomState.Race                              },

            // Match
            // { RoomKey.MatchFullFlag,                true                                        },
            // { RoomKey.MatchReadyCheckStartTime,     PhotonNetwork.Time                          }, // 이전 상태에서 처리
            // { RoomKey.MatchChoosableMapCount,       raceGameConfig.RaceChoosableMapCount        }, // 이전 상태에서 처리
            // { RoomKey.MatchRaceMapId,               MatchRaceMapId                              },  // 무조건 -1이 아니어야됨.(Test 때는 상관 없음)

            // Race
            { RoomKey.RaceState,                    RaceState.LoadPlayers                       },
            { RoomKey.CountdownStartTime,           -1                                          },
            { RoomKey.RaceStartTime,                -1                                          },
            { RoomKey.FinishStartTime,              -1                                          },
            { RoomKey.FinishEndTime,                -1                                          },
            // { RoomKey.FinishCount,                  -1                                          },
        });
    }

    public static void RaceCountdownSetting(double countdownStartTime = 0, int countDownTime = 3)
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        if (countdownStartTime <= 0)
            countdownStartTime = PhotonNetwork.Time;

        // 다른 플레이어들 다 들어오고 소환 중
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            // { RoomKey.RoomState,                    RoomState.Race                              },

            // Match
            // { RoomKey.MatchFullFlag,                true                                        },
            // { RoomKey.MatchReadyCheckStartTime,     PhotonNetwork.Time                          }, // 이전 상태에서 처리
            // { RoomKey.MatchChoosableMapCount,       raceGameConfig.RaceChoosableMapCount        }, // 이전 상태에서 처리
            // { RoomKey.MatchRaceMapId,               MatchRaceMapId                              },  // 무조건 -1이 아니어야됨.(Test 때는 상관 없음)

            // Race
            { RoomKey.RaceState,                    RaceState.Countdown                         },
            { RoomKey.CountdownStartTime,           countdownStartTime                          },
            { RoomKey.RaceStartTime,                countdownStartTime + countDownTime          },
            { RoomKey.FinishStartTime,              -1                                          },
            { RoomKey.FinishEndTime,                -1                                          },
            // { RoomKey.FinishCount,                  -1                                          },
        });
    }

    public static void RaceRacingSetting()
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        // 다른 플레이어들 다 들어오고 소환 중
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            { RoomKey.RoomState,                    RoomState.Race                              },

            // Match
            // { RoomKey.MatchFullFlag,                true                                        },
            // { RoomKey.MatchReadyCheckStartTime,     PhotonNetwork.Time                          }, // 이전 상태에서 처리
            // { RoomKey.MatchChoosableMapCount,       raceGameConfig.RaceChoosableMapCount        }, // 이전 상태에서 처리
            // { RoomKey.MatchRaceMapId,               MatchRaceMapId                              },  // 무조건 -1이 아니어야됨.(Test 때는 상관 없음)

            // Race
            { RoomKey.RaceState,                    RaceState.Racing                            },
            // { RoomKey.CountdownStartTime,           countdownStartTime                          },
            // { RoomKey.RaceStartTime,                countdownStartTime + raceStartDelayTime     },
            { RoomKey.FinishStartTime,              -1                                          },
            { RoomKey.FinishEndTime,                -1                                          },
            // { RoomKey.FinishCount,                  -1                                          },
        });
    }

    public static void RaceFinishSetting(float finishStartTime, float postGameChangeDely)
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        // 다른 플레이어들 다 들어오고 소환 중
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            { RoomKey.RoomState,                    RoomState.Race                              },

            // Match
            // { RoomKey.MatchFullFlag,                true                                        },
            // { RoomKey.MatchReadyCheckStartTime,     PhotonNetwork.Time                          }, // 이전 상태에서 처리
            // { RoomKey.MatchChoosableMapCount,       raceGameConfig.RaceChoosableMapCount        }, // 이전 상태에서 처리
            // { RoomKey.MatchRaceMapId,               MatchRaceMapId                              },  // 무조건 -1이 아니어야됨.(Test 때는 상관 없음)

            // Race
            { RoomKey.RaceState,                    RaceState.Finish                            },
            // { RoomKey.CountdownStartTime,           countdownStartTime                          },
            // { RoomKey.RaceStartTime,                countdownStartTime + raceStartDelayTime     },
            { RoomKey.FinishStartTime,              finishStartTime                             },
            { RoomKey.FinishEndTime,                finishStartTime + postGameChangeDely        },
            // { RoomKey.FinishCount,                  -1                                          },
        });
    }

    public static void RacePostGameSetting()
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        double finishStartTime = PhotonNetwork.Time;

        // 다른 플레이어들 다 들어오고 소환 중
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            { RoomKey.RoomState,                    RoomState.Race                              },

            // Match
            // { RoomKey.MatchFullFlag,                true                                        },
            // { RoomKey.MatchReadyCheckStartTime,     PhotonNetwork.Time                          }, // 이전 상태에서 처리
            // { RoomKey.MatchChoosableMapCount,       raceGameConfig.RaceChoosableMapCount        }, // 이전 상태에서 처리
            // { RoomKey.MatchRaceMapId,               MatchRaceMapId                              },  // 무조건 -1이 아니어야됨.(Test 때는 상관 없음)

            // Race
            { RoomKey.RaceState,                    RaceState.PostGame                          },
            // { RoomKey.CountdownStartTime,           countdownStartTime                          },
            // { RoomKey.RaceStartTime,                countdownStartTime + raceStartDelayTime     },
            // { RoomKey.FinishStartTime,              finishStartTime                             },
            // { RoomKey.FinishEndTime,                finishStartTime + postGameChangeDely        },
            // { RoomKey.FinishCount,                  -1                                          },
        });
    }

    public static void RaceFailedGameSetting()
    {
        if (!PhotonNetwork.InRoom && !PhotonNetwork.IsMasterClient) return;

        double finishStartTime = PhotonNetwork.Time;

        // 다른 플레이어들 다 들어오고 소환 중
        PhotonNetworkCustomProperties.SetRoomProps(new Dictionary<RoomKey, object>()
        {
            { RoomKey.RoomState,                    RoomState.Race                              },

            // Match
            // { RoomKey.MatchFullFlag,                true                                        },
            // { RoomKey.MatchReadyCheckStartTime,     PhotonNetwork.Time                          }, // 이전 상태에서 처리
            // { RoomKey.MatchChoosableMapCount,       raceGameConfig.RaceChoosableMapCount        }, // 이전 상태에서 처리
            // { RoomKey.MatchRaceMapId,               MatchRaceMapId                              },  // 무조건 -1이 아니어야됨.(Test 때는 상관 없음)

            // Race
            { RoomKey.RaceState,                    RaceState.FailedGame                        },
            // { RoomKey.CountdownStartTime,           countdownStartTime                          },
            // { RoomKey.RaceStartTime,                countdownStartTime + raceStartDelayTime     },
            // { RoomKey.FinishStartTime,              finishStartTime                             },
            // { RoomKey.FinishEndTime,                finishStartTime + postGameChangeDely        },
            // { RoomKey.FinishCount,                  -1                                          },
        });
    }

    #endregion

    // ==============================
    // = PLAYER STATE SETTING PROPS =
    // ==============================

    #region Player Setting
    public static void LocalPlayerSetup()
    {
        LocalPlayerRoomWaitPlayerSetting();
    }

    public static void LocalPlayerRoomWaitPlayerSetting()
    {
        if (!PhotonNetwork.InRoom) return;

        PhotonNetworkCustomProperties.SetPlayerProps(PhotonNetwork.LocalPlayer,
            new Dictionary<PlayerKey, object>()
            {
                // SERVER
                // { PlayerKey.Level,                    null        },
                // { PlayerKey.Exp,                      null        },
                // { PlayerKey.CarId,                    null        },
                // { PlayerKey.CharacterId,              null        },

                // PHOTON - Match
                { PlayerKey.HopeRaceMapId,            -1            },
                { PlayerKey.MatchReady,               false         },

                // PHOTON - Race
                { PlayerKey.RaceLoaded,               false         },
                { PlayerKey.RaceIsFinished,           -1            },
                { PlayerKey.RaceFinishedTime,         -1            },
                // { PlayerKey.CurrentScene,             sceneId       },
            }
        );
    }

    public static void LocalPlayerRoomMatchReadySetting(int hopeRaceMapId = -1)
    {
        if (!PhotonNetwork.InRoom) return;

        PhotonNetworkCustomProperties.SetPlayerProps(PhotonNetwork.LocalPlayer,
            new Dictionary<PlayerKey, object>()
            {
                // SERVER
                // { PlayerKey.Level,                    null        },
                // { PlayerKey.Exp,                      null        },
                // { PlayerKey.CarId,                    null        },
                // { PlayerKey.CharacterId,              null        },

                // PHOTON - Match
                { PlayerKey.HopeRaceMapId,            hopeRaceMapId },
                { PlayerKey.MatchReady,               true          },

                // PHOTON - Race
                { PlayerKey.RaceLoaded,               false         },
                { PlayerKey.RaceIsFinished,           -1            },
                { PlayerKey.RaceFinishedTime,         -1            },
                // { PlayerKey.CurrentScene,             -1            },
            }
        );
    }

    public static void LocalPlayerRaceWaitPlayerSetting()
    {
        if (!PhotonNetwork.InRoom) return;

        PhotonNetworkCustomProperties.SetPlayerProps(PhotonNetwork.LocalPlayer,
            new Dictionary<PlayerKey, object>()
            {
                // SERVER
                // { PlayerKey.Level,                    null          },
                // { PlayerKey.Exp,                      null          },
                // { PlayerKey.CarId,                    null          },
                // { PlayerKey.CharacterId,              null          },

                // PHOTON - Match
                // { PlayerKey.HopeRaceMapId,            hopeRaceMapId },
                // { PlayerKey.MatchReady,               true          },

                // PHOTON - Race
                { PlayerKey.RaceLoaded,               false         },
                { PlayerKey.RaceIsFinished,           -1            },
                { PlayerKey.RaceFinishedTime,         -1            },
                // { PlayerKey.CurrentScene,             -1            },
            }
        );
    }

    public static void LocalPlayerRaceLoadPlayersSetting()
    {
        if (!PhotonNetwork.InRoom) return;

        PhotonNetworkCustomProperties.SetPlayerProps(PhotonNetwork.LocalPlayer,
            new Dictionary<PlayerKey, object>()
            {
                // SERVER
                // { PlayerKey.Level,                    null          },
                // { PlayerKey.Exp,                      null          },
                // { PlayerKey.CarId,                    null          },
                // { PlayerKey.CharacterId,              null          },

                // PHOTON - Match
                // { PlayerKey.HopeRaceMapId,            hopeRaceMapId },
                // { PlayerKey.MatchReady,               true          },

                // PHOTON - Race
                { PlayerKey.RaceLoaded,               true          },
                { PlayerKey.RaceIsFinished,           -1            },
                { PlayerKey.RaceFinishedTime,         -1            },
                // { PlayerKey.CurrentScene,             -1            },
            }
        );
    }

    public static void LocalPlayerRaceFinishedSetting(double finishTime)
    {
        if (!PhotonNetwork.InRoom) return;

        PhotonNetworkCustomProperties.SetPlayerProps(PhotonNetwork.LocalPlayer,
            new Dictionary<PlayerKey, object>()
            {
                // SERVER
                // { PlayerKey.Level,                    null          },
                // { PlayerKey.Exp,                      null          },
                // { PlayerKey.CarId,                    null          },
                // { PlayerKey.CharacterId,              null          },

                // PHOTON - Match
                // { PlayerKey.HopeRaceMapId,            hopeRaceMapId },
                // { PlayerKey.MatchReady,               true          },

                // PHOTON - Race
                // { PlayerKey.RaceLoaded,               true          },
                { PlayerKey.RaceIsFinished,           true           },
                { PlayerKey.RaceFinishedTime,         finishTime     },
                // { PlayerKey.CurrentScene,             -1            },
            }
        );
    }

    #endregion

    // =============================
    // == PRINT CUSTOM PROPERTIES ==
    // ============================= 

    /// <summary>
    /// 현재 룸의 모든 CustomProperties 출력
    /// </summary>
    public static string PrintRoomCustomProperties()
    {
        var sb = new StringBuilder();
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            sb.AppendLine("[RoomProps] 현재 룸이 아님");
        }
        else
        {
            sb.AppendLine("==== [Room CustomProperties] ====");
            foreach (var kv in PhotonNetwork.CurrentRoom.CustomProperties)
            {
                sb.AppendLine($"{kv.Key} = {kv.Value}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 특정 플레이어의 CustomProperties 출력
    /// </summary>
    public static string PrintPlayerCustomProperties(Player player)
    {
        var sb = new StringBuilder();
        if (player == null)
        {
            sb.AppendLine("[PlayerProps] 대상 플레이어가 null");
        }
        else
        {
            sb.AppendLine($"==== [Player {player.ActorNumber} / {player.NickName}] ====");
            if (player.CustomProperties == null || player.CustomProperties.Count == 0)
            {
                sb.AppendLine("(No CustomProperties)");
            }
            else
            {
                foreach (var kv in player.CustomProperties)
                {
                    sb.AppendLine($"{kv.Key} = {kv.Value}");
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 현재 룸에 속한 모든 플레이어의 CustomProperties 출력
    /// </summary>
    public static void PrintAllPlayersCustomProperties()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
        {
            Debug.LogWarning("[PlayerProps] 현재 룸이 아님");
            return;
        }

        foreach (var p in PhotonNetwork.PlayerList)
        {
            PrintPlayerCustomProperties(p);
        }
    }


    #region Safe Set Wrappers

    private static bool IsSafeToSetProps(out ClientState state)
    {
        state = PhotonNetwork.NetworkClientState;
        return PhotonNetwork.IsConnectedAndReady
            && PhotonNetwork.InRoom
            && state == ClientState.Joined;
    }

    // ===== PLAYER SAFE =====
    public static bool TrySetLocalPlayerPropSafe(PlayerKey key, object value, Hashtable expected = null, WebFlags webFlags = null, string debugTag = null)
    {
        if (!IsSafeToSetProps(out var st))
        {
            typeof(PhotonNetworkCustomProperties).PrintLog(
                $"[CP][{debugTag}] Skip SetLocalPlayerProp: state={st}, inRoom={PhotonNetwork.InRoom}, ready={PhotonNetwork.IsConnectedAndReady}",
                LogType.Warning
            );
            return false;
        }
        return SetLocalPlayerProp(key, value, expected, webFlags);
    }

    public static bool TrySetLocalPlayerPropsSafe(IDictionary<PlayerKey, object> values, IDictionary<PlayerKey, object> expected = null, WebFlags webFlags = null, string debugTag = null)
    {
        if (!IsSafeToSetProps(out var st))
        {
            typeof(PhotonNetworkCustomProperties).PrintLog(
                $"[CP][{debugTag}] Skip SetLocalPlayerProps: state={st}, inRoom={PhotonNetwork.InRoom}, ready={PhotonNetwork.IsConnectedAndReady}",
                LogType.Warning
            );
            return false;
        }
        return SetPlayerProps(PhotonNetwork.LocalPlayer, values, expected, webFlags);
    }

    public static bool TryCompareExchangeLocalPlayerPropSafe(PlayerKey key, object newValue, object expectedValue, WebFlags webFlags = null, string debugTag = null)
    {
        if (!IsSafeToSetProps(out var st))
        {
            typeof(PhotonNetworkCustomProperties).PrintLog(
                $"[CP][{debugTag}] Skip CAS LocalPlayerProp: state={st}",
                LogType.Warning
            );
            return false;
        }
        return CompareExchangePlayerProp(PhotonNetwork.LocalPlayer, key, newValue, expectedValue, webFlags);
    }

    public static IEnumerator CoWaitAndSetLocalPlayerProps(IDictionary<PlayerKey, object> values, float timeoutSec = 3f, string debugTag = null, IDictionary<PlayerKey, object> expected = null, WebFlags webFlags = null)
    {
        float t = 0f;
        while (!IsSafeToSetProps(out _) && t < timeoutSec)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!IsSafeToSetProps(out var st2))
        {
            typeof(PhotonNetworkCustomProperties).PrintLog(
                $"[CP][{debugTag}] Timeout CoWaitAndSetLocalPlayerProps: state={st2}",
                LogType.Warning
            );
            yield break;
        }

        SetPlayerProps(PhotonNetwork.LocalPlayer, values, expected, webFlags);
    }

    // ===== ROOM SAFE (마스터 전용 권장) =====
    private static bool IsMasterAndSafe(out ClientState state)
    {
        var ok = IsSafeToSetProps(out state) && PhotonNetwork.IsMasterClient;
        return ok;
    }

    public static bool TrySetRoomPropSafe(RoomKey key, object value, Hashtable expected = null, WebFlags webFlags = null, string debugTag = null)
    {
        if (!IsMasterAndSafe(out var st))
        {
            typeof(PhotonNetworkCustomProperties).PrintLog(
                $"[RP][{debugTag}] Skip SetRoomProp: state={st}, isMaster={PhotonNetwork.IsMasterClient}, inRoom={PhotonNetwork.InRoom}",
                LogType.Warning
            );
            return false;
        }
        return SetRoomProp(key, value, expected, webFlags);
    }

    public static bool TrySetRoomPropsSafe(IDictionary<RoomKey, object> values, IDictionary<RoomKey, object> expected = null, WebFlags webFlags = null, string debugTag = null)
    {
        if (!IsMasterAndSafe(out var st))
        {
            typeof(PhotonNetworkCustomProperties).PrintLog(
                $"[RP][{debugTag}] Skip SetRoomProps: state={st}, isMaster={PhotonNetwork.IsMasterClient}, inRoom={PhotonNetwork.InRoom}",
                LogType.Warning
            );
            return false;
        }
        return SetRoomProps(values, expected, webFlags);
    }

    public static bool TryCompareExchangeRoomPropSafe(RoomKey key, object newValue, object expectedValue, WebFlags webFlags = null, string debugTag = null)
    {
        if (!IsMasterAndSafe(out var st))
        {
            typeof(PhotonNetworkCustomProperties).PrintLog(
                $"[RP][{debugTag}] Skip CAS RoomProp: state={st}, isMaster={PhotonNetwork.IsMasterClient}",
                LogType.Warning
            );
            return false;
        }
        return CompareExchangeRoomProp(key, newValue, expectedValue, webFlags);
    }

    public static IEnumerator CoWaitAndSetRoomProps(IDictionary<RoomKey, object> values, float timeoutSec = 3f, string debugTag = null, IDictionary<RoomKey, object> expected = null, WebFlags webFlags = null)
    {
        float t = 0f;
        while (!IsMasterAndSafe(out _) && t < timeoutSec)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!IsMasterAndSafe(out var st2))
        {
            typeof(PhotonNetworkCustomProperties).PrintLog(
                $"[RP][{debugTag}] Timeout CoWaitAndSetRoomProps: state={st2}, isMaster={PhotonNetwork.IsMasterClient}",
                LogType.Warning
            );
            yield break;
        }

        SetRoomProps(values, expected, webFlags);
    }

    #endregion

}
