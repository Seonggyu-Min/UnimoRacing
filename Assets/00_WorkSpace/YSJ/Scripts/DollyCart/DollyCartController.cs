using Cinemachine;
using Photon.Pun;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using YSJ.Util;

[RequireComponent(typeof(PhotonView))]
[RequireComponent(typeof(CinemachineDollyCart))]
/// <summary>
/// 돌리 카트 컨트롤러 입니다.
/// 기존적으로 해당 씬의 'TrackPathRegistry'가 필수적입니다.
/// </summary>
public class DollyCartController : MonoBehaviourPun
{
    [SerializeField] private bool isControlsInverted = false;

    private bool _isSetup = false;
    PlayerRaceData _data;

    private int _playerlistIndex = -1;

    private CinemachineDollyCart _cart;
    private CinemachinePathBase _currentPath;

    private TrackPathRegistry _trackRegistry;
    private int _movableTrackCount = -1;
    // null을 받아드릴 수 있게 하여서 값이 없으때에 대한 판단 추가
    private int? _pendingTrackIndex = null;
    private int _currentTrackIndex = -1;
    private RuntimePlatform _platform;    

    public Action<int> OnChangeTrack = null;

    public bool IsSetup => _isSetup;
    public int CurrentTrackIndex => _currentTrackIndex;


    private System.Collections.Generic.HashSet<int> _partitionA; // 지름길로 묶을 인덱스 집합
    private bool _partitionActive = false;

    void Awake()
    {
        if (_cart == null) _cart = GetComponent<CinemachineDollyCart>();
        if (_trackRegistry == null) _trackRegistry = TrackPathRegistry.Instance;
    }

    void Update()
    {
        if (_pendingTrackIndex.HasValue && _isSetup && IsReady())
        {
            int target = Mathf.Clamp(_pendingTrackIndex.Value, 0, _trackRegistry.GetPathLength() - 1);
            if (!IsTargetLocked(target) && !IsCrossingPartition(_currentTrackIndex, target)) ///
            {
                _currentTrackIndex = ApplyTrack(target);
            }
            else
            {
                Debug.Log($"[PENDING-BLOCK] from={_currentTrackIndex} to={target} (lock/partition)");
            }
            _pendingTrackIndex = null;
        }


        if (!_isSetup) return;
        if (!photonView.IsMine) return;
        if (!_data.IsControlable) return;

        if (_platform == RuntimePlatform.WindowsPlayer || _platform == RuntimePlatform.WindowsEditor)
        {
            // Windows 관련 코드 실행
            PcCountroller();
        }
        else if (_platform == RuntimePlatform.Android)
        {
            // Android 관련 코드 실행
            MobileController();
        }
        else
        {
            MobileController();
        }
    }



    public void Setup(PlayerRaceData data)
    {
        if (data == null)
        {
            this.PrintLog("PlayerRaceData를 받아올 수 없습니다.");
            return;
        }

        _data = data;

        // 트랙 배치 필수 데이터
        if (_trackRegistry == null)
            _trackRegistry = TrackPathRegistry.Instance;

        PhotonView view = _data.View;
        _playerlistIndex = view.Owner.GetPlayerRoomIndex();

        // 트랙상 배치
        _movableTrackCount = _trackRegistry.GetPathLength();
        _currentTrackIndex = _trackRegistry.GetPlayerSetupPath(_playerlistIndex);
        _currentPath = _trackRegistry.GetPath(_currentTrackIndex);

        for (int i = 0; i < _trackRegistry.GetPathLength(); i++)
        {
            var p = _trackRegistry.GetPath(i);
            Debug.Log($"레지스트리: i={i}, 경로={(p ? p.name : "null")}");
        }
        Debug.Log($"카트: 시작 인덱스={_currentTrackIndex}, 총레인={_movableTrackCount}");

        if (_movableTrackCount <= 0)
        {
            this.PrintLog("이동가능한 레인이 없습니다.");
            return;
        }

        if (_currentTrackIndex == -1)
        {
            this.PrintLog("레인 인텍스 할당 받지 못 했습니다.");
            return;
        }

        if (_currentPath == null)
        {
            this.PrintLog("경로를 할당 받지 못했습니다.");
            return;
        }

        // 해당 컴포넌트의 설정
        _cart = _data.Cart ?? GetComponent<CinemachineDollyCart>();
        if (!_cart)
        {
            this.PrintLog($"CinemachineDollyCart 컴포넌트를 찾을 수 없습니다. 해당 오브젝트{this.gameObject}를 확인해주세요.");
            return;
        }

        _cart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;
        _cart.m_Position = 0.0f;
        _cart.m_Path = _currentPath;

        _platform = Application.platform;

        targetTrackLock = new int[_movableTrackCount];
        Debug.Log($"카트: 잠금 테이블 생성 len={targetTrackLock.Length}");
        _isSetup = true;

        Debug.Log("카트: 셋업 완료");
    }

    private void MobileController()
    {
        if (IsSwitchLocked) return; // 종원 추가 

        // 모바일용
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;
        if (touch.press.wasPressedThisFrame)
        {
            Vector2 pos = touch.position.ReadValue();
            Controller(pos);
        }
    }
    private void PcCountroller()
    {
        if (IsSwitchLocked) return; // 종원 추가 

        // 에디터/PC용
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            Controller(pos);
        }
    }

    // 라인 팅김 방지 및 스압 정상화
    private void Controller(Vector2 pos)
    {
        int delta = (pos.x < Screen.width * 0.5f) ? -1 : 1;
        if (isControlsInverted) delta = -delta;
        int targetIndex = _currentTrackIndex + delta;

#if UNITY_EDITOR
        var oldIndex = _currentTrackIndex;
#endif
        _currentTrackIndex = ChangeTrack(targetIndex);
#if UNITY_EDITOR
        this.PrintLog($"MOVE: {oldIndex} > {_currentTrackIndex}");
#endif
    }

    private bool IsReady()
    {
        if (_cart == null) return false;
        if (_trackRegistry == null) return false;
        if (!_trackRegistry.IsInit) return false;
        return true;
    }

    /// <summary>
    /// 외부 값의 적용을 받을 수 있습니다. 네트워크 설계 고려됨.
    /// </summary>
    /// <param name="targetIndex">이동 하려는 경로의 TrackRegistry 인덱스</param>
    /// <returns></returns>
    public int ChangeTrack(int targetIndex, PhotonMessageInfo info = default)
    {
        // 아직 준비 안 됐으면 나중에 적용
        if (!IsReady() || !_isSetup)
        {
            _pendingTrackIndex = targetIndex;
            return _currentTrackIndex;
        }

        int max = _trackRegistry.GetPathLength();
        if (max <= 0) return _currentTrackIndex;
        targetIndex = Mathf.Clamp(targetIndex, 0, max - 1);

        if (IsCrossingPartition(_currentTrackIndex, targetIndex)) ///
        {
            return _currentTrackIndex; ///
        }

        if (IsTargetLocked(targetIndex)) // 잠금이면 이동 금지
        {
            this.PrintLog($"[LOCKED] targetIndex={targetIndex}");
            return _currentTrackIndex;
        }

        _currentTrackIndex = ApplyTrack(targetIndex);
        return _currentTrackIndex;
    }

    // 월드 좌표를 기준으로 목표 Path에서 가장 가까운 정규화 t를 샘플링으로 찾고 적용
    private int ApplyTrack(int targetIndex)
    {
        if (_cart.m_Path == null) return _currentTrackIndex;
        var fromPath = _cart.m_Path;
        var toPath = _trackRegistry.GetPath(targetIndex);
        if (fromPath == null || toPath == null) return _currentTrackIndex;

        // 현재 월드 좌표
        Vector3 worldPos = fromPath.EvaluatePositionAtUnit(
            _cart.m_Position, CinemachinePathBase.PositionUnits.Normalized
        );

        // 목표 Path의 정규화 좌표(0..1) 중, worldPos와 가장 가까운 t를 샘플링으로 찾음
        float newT = FindClosestTNormalizedBySampling(toPath, worldPos, 200); // 200개

        // 적용
        _cart.m_Path = toPath;
        _cart.m_Position = newT; // 이미 정규화 t(0~1)
        _currentPath = _cart.m_Path;

        OnChangeTrack?.Invoke(targetIndex);
        return targetIndex;
    }

    // Path 위를 균일하게 샘플링해서 worldPos와 가장 가까운 정규화 t를 찾고
    // 1차 검색 후 주변 구간을 정밀화하여 점프/후진을 최소화
    private float FindClosestTNormalizedBySampling(CinemachinePathBase path, Vector3 worldPos, int samples = 200)
    {
        // 1차: 전체 0..1 범위를 균일 샘플링
        float bestT = 0f;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < samples; i++)
        {
            float t = (samples == 1) ? 0f : (float)i / (samples - 1);
            Vector3 p = path.EvaluatePositionAtUnit(t, CinemachinePathBase.PositionUnits.Normalized);
            float d2 = (p - worldPos).sqrMagnitude;
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                bestT = t;
            }
        }

        // 2차: bestT 주변을 더 촘촘하게 미세 탐색
        const int refineSteps = 32;   // 필요시 조절
        const float refineSpan = 0.05f; // bestT 플마 5% 범위
        float tMin = Mathf.Clamp01(bestT - refineSpan);
        float tMax = Mathf.Clamp01(bestT + refineSpan);
        for (int i = 0; i < refineSteps; i++)
        {
            float t = (refineSteps == 1) ? bestT : Mathf.Lerp(tMin, tMax, (float)i / (refineSteps - 1));
            Vector3 p = path.EvaluatePositionAtUnit(t, CinemachinePathBase.PositionUnits.Normalized);
            float d2 = (p - worldPos).sqrMagnitude;
            if (d2 < bestSqr)
            {
                bestSqr = d2;
                bestT = t;
            }
        }

        return bestT;
    }

    #region 박종원 추가

    private int[] targetTrackLock; // 각 트랙 인덱스별 잠금 카운터
    private int switchLockCount = 0;
    public bool IsSwitchLocked => switchLockCount > 0;
    public void AddSwitchLock() { switchLockCount++; }
    public void RemoveSwitchLock()
    {
        switchLockCount = Mathf.Max(0, switchLockCount - 1);
    }
    private bool IsTargetLocked(int trackIndex)
    {
        if (targetTrackLock == null) return true;
        if (trackIndex < 0 || trackIndex >= targetTrackLock.Length) return true; // 범위 밖은 잠금으로 간주
        return targetTrackLock[trackIndex] > 0;
    }

    /// <summary>허용한 트랙만 이동 가능하도록 나머지를 모두 잠금</summary>
    public void LockAllExcept(params int[] allowed)
    {
        if (targetTrackLock == null) return;
        var set = new System.Collections.Generic.HashSet<int>(allowed ?? Array.Empty<int>());
        for (int i = 0; i < targetTrackLock.Length; i++)
        {
            if (!set.Contains(i)) targetTrackLock[i]++;
        }
        Debug.Log($"잠금: 허용=[{string.Join(",", allowed ?? Array.Empty<int>())}]");
    }

    /// <summary>LockAllExcept로 증가시킨 카운터를 되돌림</summary>
    public void UnlockAllExcept(params int[] allowed)
    {
        if (targetTrackLock == null) return;
        var set = new System.Collections.Generic.HashSet<int>(allowed ?? Array.Empty<int>());
        for (int i = 0; i < targetTrackLock.Length; i++)
        {
            if (!set.Contains(i)) targetTrackLock[i] = Mathf.Max(0, targetTrackLock[i] - 1);
        }
        Debug.Log($"해제: 허용=[{string.Join(",", allowed ?? Array.Empty<int>())}]");
    }
    #endregion

    
    // 지름길 세트(A) 활성화: allowedTracks를 A집합으로 등록
    public void SetPartition(params int[] groupA)
    {
        if (groupA == null) groupA = Array.Empty<int>(); 
        _partitionA = new System.Collections.Generic.HashSet<int>(groupA); 
        _partitionActive = true;                                               
    }

    // 파티션 해제
    public void ClearPartition() 
    {
        _partitionA?.Clear();         
        _partitionActive = false;     
        Debug.Log("Partition OFF");   
    }

    // 현재 인덱스(from)에서 target(to)으로 이동이 A <-> B 인지 확인
    private bool IsCrossingPartition(int from, int to) 
    {
        if (!_partitionActive || _partitionA == null) return false; 
        bool fromA = _partitionA.Contains(from);                    
        bool toA = _partitionA.Contains(to);                      
        return fromA != toA;                                        
    }
}
