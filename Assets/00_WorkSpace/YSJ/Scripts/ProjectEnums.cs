public enum RoomState
{
    None = 0,
    WaitPlayer,
    MatchReady,
    Race,
}

public enum RaceState
{
    None = 0,
    Setup,
    WaitPlayer,
    LoadPlayers,
    Countdown,
    Racing,
    Finish,
    PostGame,

    FailedGame, // WaitPlayer, LoadPlayers 쪽에서 플레이어들이 다 준비 되지 않으면 룸 터트리기
}

public enum SceneID
{
    None = 0,
    TitleScene,
    LobbyScene,
    InGameScene,
}

public enum RoomKey
{
    RoomState,                  // KEY_ROOM_STATE_TYPE

    // Match
    MatchFullFlag,              // KEY_MATCH_FULL_FLAG
    MatchReadyCheckStartTime,   // KEY_MATCH_READY_CHECK_START_TIME
    MatchChoosableMapCount,     // KEY_MATCH_CHOOSABLE_MAP_COUNT
    MatchRaceMapId,             // KEY_MATCH_RACE_MAP_ID

    // Race
    RaceState,                  // KEY_RACE_STATE_TYPE
    CountdownStartTime,         // KEY_RACE_COUNTDOWN_START_TIME
    RaceStartTime,              // KEY_RACE_START_TIME
    FinishStartTime,            // KEY_RACE_FINISH_START_TIME
    FinishEndTime,              // KEY_RACE_FINISH_END_TIME
    FinishCount,                // KEY_RACE_FINISH_COUNT

    // Vote
    WinnerMapIndex,             // KEY_VOTE_WINNER_INDEX
    VoteState,                  // KEY_ROOM_VOTE_STATE
    VoteEndTime,                // KEY_ROOM_VOTE_END_AT
}

public enum PlayerKey
{
    Level,              // KEY_PLAYER_LEVEL
    Exp,                // KEY_PLAYER_EXP

    KartId,             // KEY_PLAYER_CAR_ID
    CharacterId,        // KEY_PLAYER_CHARACTER_ID
    HopeRaceMapId,      // KEY_PLAYER_HOPERACEMAP_ID

    MatchReady,         // KEY_PLAYER_MATCH_READY

    RaceCurrentNorm,    // KEY_PLAYER_RACE_CURRENT_NORM
    RaceLoaded,         // KEY_PLAYER_RACE_LOADED
    RaceIsFinished,     // KEY_PLAYER_RACE_IS_FINISHED
    RaceFinishedTime,   // KEY_PLAYER_RACE_FINISHED_TIME

    CurrentScene,       // KEY_PLAYER_CURRENT_SCENE

    VotedMap,           // KEY_VOTE_MAP
}

// ==========================
// ========== Item ==========
// ==========================

public enum ItemId
{
    None = 0,
    Booster = 30001,    // 부스터
    ThrowBomb,          // 투척용 폭탄 (Thrown/Throwable Bomb 계열)
    Padlock,            // 자물쇠 (락/구속 트랩)
    Shield,             // 실드
    SmokeScreen,        // 시야차단 (연막/잉크 등)
    Missile,            // 미사일 (유도/직선형 모두 커버)
    FriedEgg            // 계란후라이 (바나나 대체 트랩 느낌)
}



// ==========================
// ====== StatusEffect ======
// ==========================

public enum StatusEffect
{
    None,
    Slow,           // 이동속도 감소
    Haste,          // 이동속도 가속
    Slip,           // 미끄러짐
    Silence,        // 침묵
    Immunity,       // 면역
    Airborne,       // 에어본
    Root,           // 속박
    Blind           // 시야 차단
}

public enum ReapplyMode
{
    RefreshDuration,   // 재적용 시 지속시간 갱신
    AddDuration,       // 재적용 시 지속시간 누적
    IgnoreIfActive,    // 활성 중이면 무시
    ReplaceIfStronger  // 더 강하면 교체(값/퍼센트 기준)
}
