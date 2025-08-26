public static class PhotonNetworkCustomProperties
{
    #region ROOM
    public const string KEY_RACE_CHOOSABLE_MAP_COUNT            = "room_raceChoosableMapCount";     // 선택 가능한 맵의 갯수
    public const string KEY_RACE_MAP_ID                         = "room_raceMapId";                 // 선택된 맵 ID
    public const string KEY_ROOM_FULL_FLAG                      = "room_fullNotified";              // 방이 다 찾음을 알림이
    public const string KEY_ROOM_MATCH_READY_CHECK_START_TIME   = "room_matchReadyCheckStartTime";  // 매치 레디 시작 채크 시간
    public const string KEY_ROOM_STATE_TYPE                     = "room_roomStateType";             // 룸 상태 타입

    public const string KEY_ROOM_COUNTDOWN_START_TIME           = "room_countStartTime";            // PhotonServerTime으로 카운트 다운 시작 시간
    public const string KEY_ROOM_RACE_START_TIME                = "room_raceStartTime";             // PhotonServerTime으로 레이싱 시작 시간
    public const string KEY_ROOM_FINISH_START_TIME              = "room_finishStartTime";           // PhotonServerTime으로 피니쉬 시작 시간
    public const string KEY_ROOM_FINISH_COUNT                   = "room_finishCount";               // 완주한 인원 수


    public const int VALUE_ROOM_DEFAULT_RACE_MAP_ID             = 0;
    public const int VALUE_ROOM_NOT_CHOSEN_RACE_MAP_ID          = -1;

    #endregion

    #region PLAYER
    public const string KEY_PLAYER_LEVEL                        = "player_level";                   // 플레이어 레벨
    public const string KEY_PLAYER_EXP                          = "player_exp";                     // 플레이어 경험치

    public const string KEY_PLAYER_CAR_ID                       = "player_carId";                   // 플레이어 선택 카트 ID
    public const string KEY_PLAYER_CHARACTER_ID                 = "player_characterId";             // 플레이어 선택 캐릭터 ID
    public const string KEY_PLAYER_HOPERACEMAP_ID               = "player_HopeRaceMapId";           // 플레이어 선택한 레이싱 맵 ID

    public const string KEY_PLAYER_MATCH_READY                  = "player_match_ready";             // 플레이어 매치 준비 여부

    public const string KEY_PLAYER_RACE_LOADED                  = "player_race_loaded";             // 플레이어 레이싱 로드 되었는지 여부
    public const string KEY_PLAYER_RACE_IS_FINISHED             = "player_race_isFinished";         // 플레이어 레이싱 개인 완료 여부
    public const string KEY_PLAYER_RACE_FINISHED_TIME           = "player_race_finishedTime";       // 플레이어 레이싱 개인 완료 시간

    public const string KEY_PLAYER_CURRENT_SCENE               = "player_currentSceneId";           // int SceneID임

    public const int VALUE_PLAYER_DEFAULT_LEVEL                 = 1;
    public const int VALUE_PLAYER_DEFAULT_EXP                   = 0;

    public const int VALUE_PLAYER_DEFAULT_CAR_ID                = 0;
    public const int VALUE_PLAYER_DEFAULT_CHARACTER_ID          = 0;
    public const int VALUE_PLAYER_DEFAULT_HOPERACEMAP_ID        = 0;

    #endregion
}