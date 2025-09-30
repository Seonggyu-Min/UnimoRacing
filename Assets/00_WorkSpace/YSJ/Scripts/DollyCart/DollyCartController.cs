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

    void Awake()
    {
        if (_cart == null) _cart = GetComponent<CinemachineDollyCart>();
        if (_trackRegistry == null) _trackRegistry = TrackPathRegistry.Instance;
    }

    void Update()
    {
        if (_pendingTrackIndex.HasValue && IsReady())
        {
            ApplyTrack(_pendingTrackIndex.Value);
            _pendingTrackIndex = null;
        }


        if (!_isSetup) return;
        if (!photonView.IsMine) return;
        if (!_data.IsControlable) return;

        if (_platform == RuntimePlatform.WindowsPlayer)
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

        if (_movableTrackCount == -1)
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
        _isSetup = true;

        EnsureTrackLockArrayInitialized();
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
        if (IsTargetLocked(targetIndex)) return _currentTrackIndex;

        // 아직 준비 안 됐으면 나중에 적용
        if (!IsReady() || !_isSetup)
        {
            _pendingTrackIndex = targetIndex;
            return _currentTrackIndex;
        }

        int max = _trackRegistry.GetPathLength();
        if (max <= 0) return _currentTrackIndex;
        targetIndex = Mathf.Clamp(targetIndex, 0, max - 1);

        return ApplyTrack(targetIndex);
    }
    private int ApplyTrack(int targetIndex)
    {
        var fromPath = _cart.m_Path;
        var toPath = _trackRegistry.GetPath(targetIndex);

        if (fromPath == null || toPath == null) return _currentTrackIndex;

        float fromMin = fromPath.MinPos;
        float fromMax = fromPath.MaxPos;
        float fromLen = Mathf.Max(0.0001f, fromMax - fromMin);
        float t = Mathf.Clamp01((_cart.m_Position - fromMin) / fromLen);

        float toMin = toPath.MinPos;
        float toMax = toPath.MaxPos;
        float toLen = Mathf.Max(0.0001f, toMax - toMin);
        float newPos = toMin + t * toLen;

        _cart.m_Path = toPath;
        _cart.m_Position = newPos;
        _currentPath = _cart.m_Path;

        OnChangeTrack?.Invoke(targetIndex);

        return targetIndex;
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
        if (targetTrackLock == null) return false;
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
    }

    private int[] _lastWhitelist;

    private void EnsureTargetLockArray()
    {
        int desired = -1;
        if (_trackRegistry != null && _trackRegistry.IsInit)
            desired = _trackRegistry.GetPathLength();
        else if (_movableTrackCount > 0)
            desired = _movableTrackCount;

        if (desired <= 0)
        {
            // 최소 1칸은 확보 (안전장치)
            desired = 1;
        }

        if (targetTrackLock == null)
        {
            targetTrackLock = new int[desired];
            return;
        }

        if (targetTrackLock.Length != desired)
        {
            var old = targetTrackLock;
            targetTrackLock = new int[desired];
            Array.Copy(old, targetTrackLock, Math.Min(old.Length, desired));
        }
    }

    public void ApplyTrackWhitelist(params int[] allowedTracks)
    {
        if (allowedTracks == null || allowedTracks.Length == 0)
            return;

        EnsureTargetLockArray();

        // Lock: 허용 목록을 제외한 모든 트랙에 +1
        LockAllExcept(allowedTracks);

        // 현재 트랙이 허용 외라면, 허용 목록 중 하나로 이동 시도
        if (IsTargetLocked(_currentTrackIndex))
        {
            for (int i = 0; i < allowedTracks.Length; i++)
            {
                int idx = allowedTracks[i];
                // 안전한 범위 체크
                if (idx < 0 || idx >= targetTrackLock.Length) continue;

                if (!IsTargetLocked(idx))
                {
                    ChangeTrack(idx);
                    break;
                }
            }
        }

        // Clear에서 되돌릴 수 있도록 저장
        _lastWhitelist = (int[])allowedTracks.Clone();
    }

    public void ClearTrackWhitelist()
    {
        if (_lastWhitelist == null || _lastWhitelist.Length == 0)
            return;

        EnsureTargetLockArray();

        // Unlock: 허용 목록을 제외한 모든 트랙에 -1 (0 미만으로 내려가지 않도록 내부에서 처리됨)
        UnlockAllExcept(_lastWhitelist);

        // 한 번 클리어했으면 기록 제거
        _lastWhitelist = null;
    }

    public void EnsureTrackLockArrayInitialized()
    {
        int len = -1;
        if (_trackRegistry != null && _trackRegistry.IsInit)
            len = _trackRegistry.GetPathLength();
        else if (_movableTrackCount > 0)
            len = _movableTrackCount;

        if (len <= 0) len = 1; // 안전망

        if (targetTrackLock == null || targetTrackLock.Length != len)
            targetTrackLock = new int[len];
    }
    #endregion
}
