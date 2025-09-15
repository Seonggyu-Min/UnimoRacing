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
}

public enum PlayerKey
{
    Level,              // KEY_PLAYER_LEVEL
    Exp,                // KEY_PLAYER_EXP

    KartId,             // KEY_PLAYER_CAR_ID
    CharacterId,        // KEY_PLAYER_CHARACTER_ID
    HopeRaceMapId,      // KEY_PLAYER_HOPERACEMAP_ID

    MatchReady,         // KEY_PLAYER_MATCH_READY

    RaceLoaded,         // KEY_PLAYER_RACE_LOADED
    RaceIsFinished,     // KEY_PLAYER_RACE_IS_FINISHED
    RaceFinishedTime,   // KEY_PLAYER_RACE_FINISHED_TIME

    CurrentScene,       // KEY_PLAYER_CURRENT_SCENE

    VotedMap,           // KEY_VOTE_MAP
}