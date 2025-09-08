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
    private int _currentTrackIndex = -1;

    public Action<int> OnChangeTrack = null;

    public bool IsSetup => _isSetup;
    public int CurrentTrackIndex => _currentTrackIndex;

    public void Setup(PlayerRaceData data)
    {
        if (data == null)
        {
            this.PrintLog("PlayerRaceData를 받아올 수 없습니다.");
            return;
        }

        _data = data;

        // 트랙 배치 필수 데이터
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
        _cart = _data.Cart;
        if (_cart)
        {
            _cart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;
            _cart.m_Position = 0.0f;
            _cart.m_Path = _currentPath;
        }
        else
        {
            this.PrintLog($"CinemachineDollyCart 컴포넌트를 찾을 수 없습니다. 해당 오브젝트{this.gameObject}를 확인해주세요.");
            return;
        }

        _isSetup = true;
    }

    void Update()
    {
        if (!_isSetup) return;

        // 해당 포톤 뷰가, 해당 클라이언트 것이 맞고 조종이 가능한 상태인지
        if (!photonView.IsMine) return;

        MobileController();
        PcCountroller();
    }

    private void MobileController()
    {
        // 모바일용
        if (Touchscreen.current == null) return;

        var touch = Touchscreen.current.primaryTouch;
        if (touch.press.wasPressedThisFrame)
        {
            Vector2 pos = touch.position.ReadValue();
            int targetIndex = _currentTrackIndex + ((pos.x < Screen.width * 0.5f) ? -1 : 1);
            if (isControlsInverted) targetIndex = 1 - targetIndex;
#if UNITY_EDITOR
            var oldIndex = _currentTrackIndex;
#endif
            _currentTrackIndex = ChangeTrack(targetIndex);
#if UNITY_EDITOR
            this.PrintLog($"MOVE: {oldIndex} > {_currentTrackIndex}");
#endif
        }
    }
    private void PcCountroller()
    {
        // 에디터/PC용
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 pos = Mouse.current.position.ReadValue();
            int targetIndex = _currentTrackIndex + ((pos.x < Screen.width * 0.5f) ? -1 : 1);
            if (isControlsInverted) targetIndex = 1 - targetIndex;
#if UNITY_EDITOR
            var oldIndex = _currentTrackIndex;
#endif
            _currentTrackIndex = ChangeTrack(targetIndex);
#if UNITY_EDITOR
            this.PrintLog($"MOVE: {oldIndex} > {_currentTrackIndex}");
#endif
        }
    }

    /// <summary>
    /// 외부 값의 적용을 받을 수 있습니다. 네트워크 설계 고려됨.
    /// </summary>
    /// <param name="targetIndex">이동 하려는 경로의 TrackRegistry 인덱스</param>
    /// <returns></returns>
    public int ChangeTrack(int targetIndex, PhotonMessageInfo info = default)
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

        OnChangeTrack.Invoke(targetIndex);

        return targetIndex;
    }
}
