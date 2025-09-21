using Firebase.Database;
using MSG;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using YSJ.Util;

public class PlayerSpawner : MonoBehaviour
{
    private const int DEFAULT_CHARACTER_ID = 20001;
    private const int DEFAULT_KART_ID = 10001;

    [Header("Injecter")]
    [SerializeField] private bool _isUseInjecter = false;

    [Header("Config")]
    [SerializeField] private bool _isStartDirectSpawn = false;

    // Network
    private Player _me;

    // Injecter
    private PlayerSpawnDataInjecter _injecter;
    private PlayerSpawnData _data = null;
    private bool _useInjecter = false;

    // Load
    private GameObject _baseGO;
    private int _reqCharacterID;
    private int _reqKartID;
    private float _reqKartBaseSpeed;

    private bool _isLoadBaseGO= false;
    private bool _isReqCharacterID = false;
    private bool _isReqKartID = false;
    private bool _isReqKartBaseSpeed = false;

    // Delay Load 
    private float _loadableTime = 5.0f;


    private void Start()
    {
        SetupInjecter();
        if (_isStartDirectSpawn)
        {
            this.PrintLog("Start 함수 실행, 바로 스폰은 진행 합니다.");
            StartCoroutine(CO_PlayerSpanwe());
        }
        else
        {
            this.PrintLog("InGameManager 매니저에 OnRaceState_LoadPlayers 타이밍으로 스폰 타이밍을 이관합니다.");
            InGameManager.Instance.OnRaceState_LoadPlayers -= OnSpawnAction;
            InGameManager.Instance.OnRaceState_LoadPlayers += OnSpawnAction;
        }
    }

    // 외부 스폰 이관 시, 사용
    private void OnSpawnAction()
    {
        StartCoroutine(CO_PlayerSpanwe());
    }

    // 플레이어 스폰 함수
    private IEnumerator CO_PlayerSpanwe()
    {
        _isLoadBaseGO = TryLoadBaseGO();
        if (!_isLoadBaseGO)
        {
            this.PrintLog("플레이어 베이스될 수 있는 게임 오브젝트 로드에 실패 하였습니다.");
            yield break;
        }
        this.PrintLog("플레이어 베이스 정상 로드");



        this.PrintLog("플레이어, 룸에 들어와 있는지 확인중");
        while (!TryCheckIsSpawnable())
        {
            yield return null;
        }
        _me = PhotonNetwork.LocalPlayer;



        this.PrintLog("플레이어, 캐릭터 ID 로드");
        TryLoadCharacterID();
        while (!_isReqCharacterID)
        {
            yield return null;
        }



        this.PrintLog("플레이어, 카트 ID 로드");
        TryLoadKartID();
        while (!_isReqKartID)
        {
            yield return null;
        }



        this.PrintLog("플레이어, 카트 BaseSpeed 로드");
        TryLoadKartBaseSpeed();
        while (!_isReqKartBaseSpeed)
        {
            yield return null;
        }



        this.PrintLog("플레이어, 스폰");
        // TrySpawnLocal();
        TrySpawnLocal_2();
        yield break;
    }

    // Injecter Func
    private bool UseInjecter(PlayerSpawnDataInjecter injecter)
    {
        if (!_isUseInjecter) return false;
        return (injecter != null && injecter.GetData() != null);
    }
    private void SetupInjecter()
    {
        this.PrintLog("SetupInjecter 진행");

        _injecter = GetComponent<PlayerSpawnDataInjecter>();
        _useInjecter = UseInjecter(_injecter);
        if (_useInjecter)
        {
            this.PrintLog("인젝터를 사용할 수 있는 상태이자, 사용하고자한 상태입니다. > 인젝터에서 데이터를 주입 받습니다.");
            _data = _injecter.GetData();
        }

        this.PrintLog("SetupInjecter 진행 완료");
    }

    // Try Func
    private bool TryLoadBaseGO()
    {
        _baseGO = Resources.Load<GameObject>(LoadPath.PLAYER_BASE_PREFAB_PATH);
        if (_baseGO == null)
        {
            this.PrintLog("플레이어 베이스 프리팹이 없습니다.");
            return false;
        }

        return true;
    }
    private bool TryCheckIsSpawnable()
    {
        return PhotonNetwork.InRoom && TrackPathRegistry.Instance.IsInit;
    }
    private void TryLoadCharacterID()
    {
        if (_isUseInjecter)
        {
            _reqCharacterID = _data.CharacterID;
            _isReqCharacterID = true;
        }
        else
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedUnimo(_me.UserId),
                (DataSnapshot snapShot) =>
                {
                    object value = snapShot.Value;
                    this.PrintLog($"장착 중인 유니모: {value ?? "없음"}");

                    if (value != null)
                    {
                        if (value is long l) _reqCharacterID = (int)l;
                        else if (!int.TryParse(value.ToString(), out _reqCharacterID))
                        {
                            this.PrintLog("CharacterID 파싱 실패");
                            return;
                        }
                    }
                    _isReqCharacterID = true;
                },
                GetErrorCharacterData
            );
        }
    }
    private void TryLoadKartID()
    {
        if (_isUseInjecter)
        {
            _reqKartID = _data.KartID;
            _isReqKartID = true;
        }
        else
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedKart(_me.UserId),
                (DataSnapshot snapShot) =>
                {
                    object value = snapShot.Value;
                    this.PrintLog($"장착 중인 카트: {value ?? "없음"}");

                    if (value != null)
                    {
                        if (value is long l) _reqKartID = (int)l;
                        else if (!int.TryParse(value.ToString(), out _reqKartID))
                        {
                            this.PrintLog("KartID 파싱 실패");
                            return;
                        }
                    }
                    _isReqKartID = true;
                },
                GetErrorKartData
            );
        }
    }
    private void TryLoadKartBaseSpeed()
    {
        PatchService.Instance.GetSpeedOfKart(_reqKartID,
            speed =>
            {
                _reqKartBaseSpeed = speed;
                this.PrintLog($"{_reqKartID}번 카트 속도: {speed}");
                _isReqKartBaseSpeed = true;
            },
            err =>
            {
                _reqKartBaseSpeed = -1;
                this.PrintLog($"{_reqKartID}번 카트 속도 조회 실패: {err}");
                _isReqKartBaseSpeed = true;
            }
        );
    }

    // Pun Spawn
    private void TrySpawnLocal()
    {

        // ID ===========================================================================
        // 아이디 관련 처리
        int userCharacterID = -1;
        int userKartID = -1;

        if (_isUseInjecter)
        {
            userCharacterID = _data.CharacterID;
            userKartID = _data.KartID;
        }
        else
        {
            // 서버에서 현 로컬 플레이어의 IDs 가지고 오기

            DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedKart(_me.UserId),
                (DataSnapshot snapShot) =>
                {
                    object value = snapShot.Value;
                    this.PrintLog($"장착 중인 유니모: {value ?? "없음"}");

                    if (value != null)
                    {
                        if (value is long l) userCharacterID = (int)l;
                        else if (!int.TryParse(value.ToString(), out userCharacterID))
                            this.PrintLog("CharacterID 파싱 실패");
                    }
                }, GetErrorCharacterData);

            DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedUnimo(_me.UserId),
                (DataSnapshot snapShot) =>
                {
                    object value = snapShot.Value;
                    this.PrintLog($"장착 중인 카트: {value ?? "없음"}");

                    if (value != null)
                    {
                        if (value is long l) userKartID = (int)l;
                        else if (!int.TryParse(value.ToString(), out userKartID))
                            this.PrintLog("KartID 파싱 실패");
                    }
                }, GetErrorKartData);
        }

        // 아이디 값 이상하게 들어왔을 때에 대한 예외 처리
        if (userCharacterID == -1 || userKartID == -1)
        {
            userCharacterID = (userCharacterID == -1) ? DEFAULT_CHARACTER_ID : userCharacterID;
            userKartID = (userKartID == -1) ? DEFAULT_KART_ID : userKartID;
            this.PrintLog($"ID 이상 발견 > 베이스 아이디로 처리\n" +
                $"(userCharacterID = {userCharacterID} / userKartID = {userKartID})", LogType.Warning);
        }

        // SO ===========================================================================
        // TODO: 나중에 로드 부분만 어드레서블 오브젝트 로드 변경
        UnimoCharacterSO    characterSO = null;
        UnimoKartSO         kartSO = null;

        characterSO = Resources.Load<UnimoCharacterSO>($"{LoadPath.PLAYER_UNIMO_CHARACTER_SO}_{userCharacterID}");
        kartSO = Resources.Load<UnimoKartSO>($"{LoadPath.PLAYER_UNIMO_KART_SO}_{userKartID}");

        if (characterSO == null || kartSO == null)
        {
            this.PrintLog($"SO 이상 발견 \n" +
                $"(characterSO = {characterSO == null}, {characterSO} / kartSO = {kartSO == null}, {kartSO})", LogType.Error);
            return;
        }

        this.PrintLog($"플레이어 필요 데이터 로드 완료\n" +
            $"로드 시도자 > [{_me} / {_me.ActorNumber} / {UnityUtilEx.GetPlayerRoomIndex(_me)}]\n" +
            $"characterSO = {characterSO == null}, {characterSO.name}\n" +
            $"kartSO = {kartSO == null}, {kartSO.name}");

        // PatchService.Instance.GetSpeedOfKart(_kartID, GetSuccessKartSpeedData, GetErrorKartSpeedData);
        object[] instData = {userCharacterID, userKartID};
        PhotonNetwork.Instantiate(_baseGO.name, Vector3.zero, Quaternion.identity, 0, instData);
    }
    private void TrySpawnLocal_2()
    {
        // 아이디 값 이상하게 들어왔을 때에 대한 예외 처리
        if (_reqCharacterID == -1 || _reqKartID == -1)
        {
            _reqCharacterID = (_reqCharacterID == -1) ? DEFAULT_CHARACTER_ID : _reqCharacterID;
            _reqKartID = (_reqKartID == -1) ? DEFAULT_KART_ID : _reqKartID;

            this.PrintLog($"ID 이상 발견 > 베이스 아이디로 처리\n" +
                $"(userCharacterID = {_reqCharacterID} / userKartID = {_reqKartID})", LogType.Warning);
        }

        object[] instData = { _reqCharacterID, _reqKartID, _reqKartBaseSpeed};
        PhotonNetwork.Instantiate(_baseGO.name, Vector3.zero, Quaternion.identity, 0, instData);
    }

    // onError
    private void GetErrorCharacterData(string snapShot)
    {
        this.PrintLog($"장착 중인 유니모 읽기 오류: {snapShot}");
        _isReqCharacterID = true;
    }
    private void GetErrorKartData(string snapShot)
    {
        this.PrintLog($"장착 중인 카트 읽기 오류: {snapShot}");
        _isReqKartID = true;
    }

    private void PrintLog(string printLogString)
    {
        this.PrintLog(printLogString, LogType.Log, Color.red);
    }
}
