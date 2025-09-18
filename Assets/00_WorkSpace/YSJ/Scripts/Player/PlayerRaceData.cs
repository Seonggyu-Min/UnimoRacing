using Cinemachine;
using Firebase.Database;
using MSG;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using YSJ.Util;

// 시스템
[RequireComponent(typeof(PhotonView))] // 네트워크
[RequireComponent(typeof(CinemachineDollyCart))] // 이동

// 필수 기능 추가
[RequireComponent(typeof(DollyCartController))] // 경로 컨트롤
[RequireComponent(typeof(DollyCartMovement))] // 이동 제어

[RequireComponent(typeof(PlayerInventory))] // 인벤
[RequireComponent(typeof(UnimoSynergySystem))] // 시너지
[RequireComponent(typeof(UnimoRaceAnimationController))] // 유니모 애니메이션 컨트롤러

[RequireComponent(typeof(DollyCartSync))] // 싱크(=동기화)
public class PlayerRaceData : MonoBehaviour, IPunInstantiateMagicCallback
{
    private const float EPS = 0.0001f; // 미세 흔들림 방지

    private bool _isSetups = false;

    [Header("Config")]
    [SerializeField] private string _sitPointName = "pivot_Character";
    [SerializeField] private string _followCam = "VirtualCam";
    [SerializeField] private bool _useLoadFollowCam = true;
    [SerializeField] private bool _useGM = false;
    [SerializeField] private bool _useDataBase = false;

    [Header("Data Being Applied")]
    [SerializeField] private float _kartBaseSpeed = 0.0f;
    [SerializeField] private float _kartCurrentSpeed = 0.0f;

    [SerializeField] private bool _isControlable = false;    // 컨트롤 가능 여부
    [SerializeField] private bool _isMovable = false;        // 이동 가능 여부
    [SerializeField] private bool _isItemUsable = false;     // 아이템 사용가능 여부

    private bool _isEndRace = false;
    private int _currentTrackIndex = -1;
    private int _lap = 0;
    private float _norm = 0.0f;
    private float _oldNorm = 0.0f;



    private PhotonView _view;
    private CinemachineDollyCart _cart;

    private DollyCartController _cartController;
    private DollyCartMovement _cartMovement;
    private PlayerInventory _playerInventory;

    private UnimoSynergySystem _synergySystem;
    private UnimoRaceAnimationController _raceAniCtrl;

    private DollyCartSync _sync;



    private InGameManager _inGM;



    private int _characterID = -1;
    private int _kartID = -1;

    private UnimoCharacterSO _characterSO;  // CharacterSetup
    private UnimoKartSO _kartSO;            // KartSetup

    private GameObject _kartBody;
    private GameObject _characterBody;

    private GameObject _kartSitPoint;

    private bool _isSync = false;
    private bool _isSynergy = false;


    public PhotonView View => _view;
    public CinemachineDollyCart Cart => _cart;
    public DollyCartController Controller => _cartController;
    public DollyCartMovement Movement => _cartMovement;

    public int CharacterID => _characterID;
    public int KartID => _kartID;

    public UnimoCharacterSO CharacterSO => _characterSO;
    public UnimoKartSO KartSO => _kartSO;

    public bool IsControlable => _isControlable;     
    public bool IsMovable => _isMovable;         
    public bool IsItemUsable => _isItemUsable;

    #region Unity
    private void Awake()
    {
        _view = gameObject.GetOrAddComponent<PhotonView>();
        _cart = gameObject.GetOrAddComponent<CinemachineDollyCart>();

        _cartController = gameObject.GetOrAddComponent<DollyCartController>();
        _cartMovement = gameObject.GetOrAddComponent<DollyCartMovement>();

        _playerInventory = gameObject.GetOrAddComponent<PlayerInventory>();
        _synergySystem = gameObject.GetOrAddComponent<UnimoSynergySystem>();
        _raceAniCtrl = gameObject.GetOrAddComponent<UnimoRaceAnimationController>();

        _sync = gameObject.GetOrAddComponent<DollyCartSync>();

        _inGM = InGameManager.Instance;

        _isControlable = false;    // 컨트롤 가능 여부
        _isMovable = false;        // 이동 가능 여부
        _isItemUsable = false;     // 아이템 사용가능 여부

        _isEndRace = false;
    }

    #endregion

    #region Setup
    private void KartSetup()
    {
        this.PrintLog("KartSetup 진행");

        if (_useDataBase)
        {
            // 서버에서 필요 데이터 불러오기
            string uId = PhotonNetwork.LocalPlayer.UserId;

            // 장착 카트 ID
            DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedKart(uId), GetSuccessKartData, GetErrorKartData);

            // _speed = 서버 속도
            PatchService.Instance.GetSpeedOfKart(_kartID, GetSuccessKartSpeedData, GetErrorKartSpeedData);
        }
        
        // Kart ID로 Kart 속성 및 SO Load
        _kartSO = Resources.Load<UnimoKartSO>($"{LoadPath.PLAYER_UNIMO_KART_SO}_{_kartID}");

        // 프리팹 생성
        _kartBody = GameObject.Instantiate(_kartSO.kartPrefab, transform);
        _kartBody.transform.position = Vector3.zero;


        // 생성한 카트 바디 오브젝트의 sitPoint 찾기
        var findSitPoint = _kartBody.GetChild<Transform>(_sitPointName);
        _kartSitPoint = findSitPoint?.gameObject;

        _kartBody.GetOrAddComponent<UnimoKartAniCtrl>();
        this.PrintLog("KartSetup 진행 완료");
    }
    private void CharacterSetup()
    {
        this.PrintLog("CharacterSetup 진행");

        if (_useDataBase)
        {
            // 서버에서 필요 데이터 불러오기
            string uId = PhotonNetwork.LocalPlayer.UserId;

            // 장착 유니모 ID
            DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedUnimo(uId), GetSuccessCharacterData, GetErrorCharacterData);
        }

        // Character ID로 Character 속성 및 SO Load
        _characterSO = Resources.Load<UnimoCharacterSO>($"{LoadPath.PLAYER_UNIMO_CHARACTER_SO}_{_characterID}");

        // 프리팹 생성
        GameObject sitPoint =(_kartSitPoint != null) ? _kartSitPoint : gameObject;
        _characterBody = GameObject.Instantiate(_characterSO.characterPrefab, sitPoint.transform);

        // 시너지 여부 판단
        _isSynergy = (_characterSO.SynergyKartID == _kartSO.KartID);

        // 서버에서 필요 데이터 불러오기

        _characterBody.GetOrAddComponent<UnimoCharacterAniCtrl>();
        this.PrintLog("CharacterSetup 진행 완료");
    }

    private void ControllerSetup()
    {
        this.PrintLog("ControllerSetup 진행");
        // 컨트롤러 설정
        _cartController.Setup(this);
        _currentTrackIndex = _cartController.CurrentTrackIndex;

        _cartController.OnChangeTrack -= OnNetworkSendTrack;
        _cartController.OnChangeTrack += OnNetworkSendTrack;

        this.PrintLog("ControllerSetup 진행 완료");
    }
    private void MovementSetup()
    {
        this.PrintLog("MovementSetup 진행");

        _cartMovement.Setup(this);
        _cartMovement.OnMovementProgress -= OnNetworkSendMovementProgress;
        _cartMovement.OnMovementProgress += OnNetworkSendMovementProgress;
        _cartMovement.OnMovementProgress -= OnEndCheck;
        _cartMovement.OnMovementProgress += OnEndCheck;
        this.PrintLog("MovementSetup 진행 완료");
    }

    private void CamSetup()
    {
        this.PrintLog("CamSetup 진행");
        if (_useLoadFollowCam && View.IsMine)
        {
            GameObject vcPrefab = Resources.Load<GameObject>(_followCam);
            GameObject vcGo = GameObject.Instantiate(vcPrefab);
            CinemachineVirtualCamera vccmp = vcGo.GetComponent<CinemachineVirtualCamera>();
            if (vccmp != null)
            {
                vccmp.Follow = this.transform;
            }
        }
        this.PrintLog("CamSetup 진행 완료");
    }

    private void PlayerInventroySetup()
    {
        this.PrintLog("PlayerInventroySetup 진행");
        if (_playerInventory != null)
        {
            _playerInventory.Setup();
        }
        this.PrintLog("PlayerInventroySetup 진행 완료");
    }
    private void AniCtrlSetup()
    {
        this.PrintLog("AniCtrlSetup 진행");
        if (_raceAniCtrl != null)
        {
            _raceAniCtrl.Setup();

            // TODO: 필요시, 플레이 애니메이션 액션으로 이관 예정
            _raceAniCtrl.PlayMoveAni();
        }

        this.PrintLog("AniCtrlSetup 진행 완료");
    }
    private void SynergySetup()
    {
        this.PrintLog("AniCtrlSetup 진행");
        _synergySystem?.Setup(this);

        this.PrintLog("AniCtrlSetup 진행 완료");
    }

    private void SyncSetup()
    {
        this.PrintLog("SyncSetup 진행");
        _sync?.Setup(this);

        this.PrintLog("SyncSetup 진행 완료");
    }

    private void GameManagerSetup()
    {
        this.PrintLog("GameManagerSetup 진행");
        if (_inGM != null && _useGM)
        {
            // 로드
            _inGM.OnRaceState_LoadPlayers -= OnPlayReady;
            _inGM.OnRaceState_LoadPlayers += OnPlayReady;

            // 레이싱
            _inGM.OnRaceState_Racing -= OnPlayRaceEnter;
            _inGM.OnRaceState_Racing += OnPlayRaceEnter;

            // 레이싱 끝
            _inGM.OnRaceState_Finish -= OnPlayRaceExit;
            _inGM.OnRaceState_Finish += OnPlayRaceExit;
        }

        this.PrintLog("GameManagerSetup 진행 완료");
    }

    #endregion

    #region On Actions
    // 동기화
    private void OnNetworkSendTrack(int chanagedTrackIndex)
    {
        _currentTrackIndex = chanagedTrackIndex;
    }
    private void OnNetworkSendMovementProgress(int lap, float norm)
    {
        _oldNorm = _lap + _norm;
        _lap = lap;
        _norm = norm;
    }
    private void OnEndCheck(int lap, float norm)
    {
        if (_useGM && !_isEndRace)
        {
            if (_inGM != null && _lap >= _inGM.RaceEndLapCount)
            {
                PhotonNetworkCustomProperties.LocalPlayerRaceFinishedSetting(PhotonNetwork.Time);

                _isControlable = false;
                _isMovable = false;
                _isItemUsable = false;

                _isEndRace = true;
            }
        }
    }
    public void SetState(bool isControlable, bool isMovable, bool isItemUsable)
    {
        _isControlable  = isControlable;    
        _isMovable      = isMovable;        
        _isItemUsable   = isItemUsable;
    }


    // 인게임 흐름
    private void OnPlayReady()
    {
        // TODO: 플레이 준비(로드 되고 셋업 되었을 때, 실행)
        _kartCurrentSpeed = 0;
        _isControlable  = false;    // 컨트롤 가능 여부
        _isMovable      = false;    // 이동 가능 여부
        _isItemUsable   = false;    // 아이템 사용가능 여부
    }
    private void OnPlayRaceEnter()
    {
        // TODO: 플레이 들어갈 때
        // 
        _kartCurrentSpeed = _kartBaseSpeed;
        _isControlable  = true;    // 컨트롤 가능 여부
        _isMovable      = true;    // 이동 가능 여부
        _isItemUsable   = true;    // 아이템 사용가능 여부
    }
    private void OnPlayRaceExit()
    {
        // TODO: 플레이 
        _kartCurrentSpeed = 0;
        _isControlable  = false;    // 컨트롤 가능 여부
        _isMovable      = false;    // 이동 가능 여부
        _isItemUsable   = false;    // 아이템 사용가능 여부
    }

    #endregion

    #region Get / Set > Other Out Input Datas
    /// <summary>
    /// 초기화 여부를 판단합니다. 기본적으로 로드가 되어 초기화가 되었는지 확인하는 용도입니다.
    /// </summary>
    public bool IsSetups => _isSetups;
    public bool IsSync => _isSync;
    public bool IsSynergy => _isSynergy;


    /// <summary>
    /// 현재의 트랙의 인텍스 정보입니다. '_currentTrackIndex' 값은 Controller에서 변경을 담당하고 변경 시, 값이 수정됩니다.
    /// </summary>
    public int CurrentTrackIndex => _currentTrackIndex;
    /// <summary>
    /// 렙은 현재 몇 바퀴를 돌았는지를 체크하는 용도입니다.
    /// </summary>
    public int Lap => _lap;
    /// <summary>
    /// 현재 트랙의 진행 상황입니다.
    /// </summary>
    public float Norm => _norm;
    /// <summary>
    /// 실직적으로 적용되어 이동을 도움을 주는 값입니다.
    /// </summary>
    public float KartSpeed => _kartCurrentSpeed;

    public void SetKartSpeed(float applySpeed)
    {
        this.PrintLog($"속도 변화: {applySpeed} > {_kartCurrentSpeed}");
        _kartCurrentSpeed = applySpeed;
    }
    #endregion


    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // info Data
        Player      player      = info.Sender;
        PhotonView  view        = info.photonView;
        double      time        = info.SentServerTime;
        float       timestamp   = info.SentServerTimestamp;
        object[]    instData    = info.photonView.InstantiationData;

        // 오브젝트 이름
        string safeUserId = string.IsNullOrEmpty(player.UserId) ? "NoUserId" : player.UserId;
        string playerGoName = $"PlayerRacer_{safeUserId}";

        gameObject.name = playerGoName;

        // 동기화 필요 여부
        _isSync = !info.Sender.IsLocal;

        // 트랙 배정(DollyCartController > Setup 함수에서 진행)

        // 데이터 역직렬화
        if (instData != null)
        {
            // 안전하게 꺼내기 (characterID, kartID)

            // actorNumber
            if (instData.Length >= 1 && instData[0] is int cID)
                _characterID = cID;
            if (instData.Length >= 2 && instData[1] is int kID)
                _kartID = kID;

            if (_characterID == -1 || _kartID == -1)
            {
                this.PrintLog("OnPhotonInstantiate > 받은 instData 문제 발생");
                return;
            }
        }

        // 로그
        this.PrintLog(
        $"\n플레이어 오브젝트 이름: {playerGoName}\n" +
        $"생성한 플레이어: {player.NickName}\n" +
        $"서버에 도착한 시간(초): {time}\n" +
        $"서버에 도착한 시간(밀리초): {timestamp}\n" +

        $"PhotonView ID: {view.ViewID}\n" +

        $"CharacterID: {_characterID}\n" +
        $"KartID: {_kartID}\n" +

        $"Sync: {_isSync}\n" +
        $"Synergy: {_isSynergy}\n" +
        $"");

        // Setup(순서: (Kart > Character) > (Controller > Movement) > Sync > (Cam > AniCtrl > Synergy))
        // Visual
        KartSetup();
        CharacterSetup();

        // 카트
        ControllerSetup();
        MovementSetup();

        CamSetup();
        PlayerInventroySetup();
        AniCtrlSetup();
        SynergySetup();

        // 동기화
        SyncSetup();

        _isSetups = (_cartController.IsSetup && _cartMovement.IsSetup && _raceAniCtrl.IsSetup && _synergySystem.IsSetup && _sync.IsSetup);

        // 게임 매니저
        GameManagerSetup();

        // 플레이어의 커스텀 프롬퍼티 생성 시점 > 매칭이 되었을 때
        // 룸데이터는 그 이전에 되어 있어야된다.
        var pm = PlayerManager.Instance;
        pm.SetPlayerCPRaceLoaded(_isSetups);
    }

    // 네트워크
    // onSuccess
    private void GetSuccessCharacterData(DataSnapshot snapShot)
    {
        object value = snapShot.Value;
        this.PrintLog($"장착 중인 유니모: {value ?? "없음"}");
        _characterID = _characterID.CompareTo(value);
    }
    private void GetSuccessKartData(DataSnapshot snapShot)
    {
        object value = snapShot.Value;
        this.PrintLog($"장착 중인 카트: {value ?? "없음"}");
        _kartID = _kartID.CompareTo(value);
    }
    private void GetSuccessKartSpeedData(float speedData)
    {
        this.PrintLog($"장착 중인 카트의 속도: {speedData}");
        _kartBaseSpeed = speedData;
    }


    // onError
    private void GetErrorCharacterData(string snapShot)
    {
        this.PrintLog($"장착 중인 유니모 읽기 오류: {snapShot}");
    }
    private void GetErrorKartData(string snapShot)
    {
        this.PrintLog($"장착 중인 카트 읽기 오류: {snapShot}");
    }
    private void GetErrorKartSpeedData(string snapShot)
    {
        this.PrintLog($"장착 중인 카트 속도 읽기 오류: {snapShot}");
    }
}
