using Photon.Pun;
using UnityEngine;
using YSJ.Util;

[RequireComponent(typeof(PhotonView))]
public class DollyCartSync : MonoBehaviourPun, IPunObservable
{
    #region Parameter
    [SerializeField, Min(0.1f)] private float _fixedCPUpdateCycleTime = 2.0f;

    private bool _isSetup = false;

    private PlayerRaceData _data;
    private PlayerManager _pm;
    private float _currentCycleTime = 0.0f;

    public bool IsSetup => _isSetup;
    #endregion

    private void OnDisable()
    {
        if (!_isSetup) return;

        _data.OnAfterRaceSetupAction -= AfterPlayerRaceSetupChangeRacableCP;
    }

    public void Setup(PlayerRaceData data)
    {
        if (data == null)
        {
            this.PrintLog("PlayerRaceData를 받아올 수 없습니다.");
            return;
        }

        _data = data;
        _pm = PlayerManager.Instance;

        _data.OnAfterRaceSetupAction -= AfterPlayerRaceSetupChangeRacableCP;
        _data.OnAfterRaceSetupAction += AfterPlayerRaceSetupChangeRacableCP;
        _currentCycleTime = 0.0f;
        _isSetup = true;
    }

    // 동기화를 받아야 되는 카트에 필요
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (!_isSetup) return;

        // 이건 원 주인
        if (stream.IsWriting && _data.View.IsMine)
        {
            Send(stream, info);
        }
        // 동기화를 받아야되는 대상
        else
        {
            Receive(stream, info);
        }
    }

    // 보내기
    private void Send(PhotonStream stream, PhotonMessageInfo info)
    {
        stream.SendNext(_data.CurrentTrackIndex);   // 라인
        stream.SendNext(_data.KartSpeed);           // 속도

        stream.SendNext(_data.Lap);                 // 렙
        stream.SendNext(_data.Norm);                // 진행 퍼센트

        stream.SendNext(_data.IsControlable);
        stream.SendNext(_data.IsMovable);
        stream.SendNext(_data.IsItemUsable);
    }

    // 받기
    private void Receive(PhotonStream stream, PhotonMessageInfo info)
    {
        int     recvTrackIndex    = (int)stream.ReceiveNext();
        float   recvKartSpeed     = (float)stream.ReceiveNext();

        int     recvLap           = (int)stream.ReceiveNext();
        float   recvNorm          = (float)stream.ReceiveNext();

        bool   recvIsControlable  = (bool)stream.ReceiveNext();
        bool   recvIsMovable      = (bool)stream.ReceiveNext();
        bool   recvIsItemUsable   = (bool)stream.ReceiveNext();

        _data.Controller.ChangeTrack(recvTrackIndex, info);

        _data.Movement.ChangeSpeed(recvKartSpeed, info);
        _data.Movement.SyncPosition(recvNorm, info);
        _data.SetState(recvIsControlable, recvIsMovable, recvIsItemUsable);

        // _controller.SyncReceive(recvNorm, recvLap, recvSpeed);
    }

    private void AfterPlayerRaceSetupChangeRacableCP()
    {
        if (_isSetup) return;

        var pm = PlayerManager.Instance;
        if (_data != null)
        {
            pm.SetPlayerCPRaceLoaded(_data.IsSetups);
            pm.SetPlayerCPRaceCurrentNorm(_data.Lap + _data.Norm);
        }
    }

    private void FixedCPUpdate()
    {
        _pm?.SetPlayerCPRaceCurrentNorm(_data.Lap + _data.Norm);
    }

    public void Update()
    {
        if (!_isSetup) return;
        if (!_data.View.IsMine) return;

        _currentCycleTime += Time.deltaTime;
        if (_currentCycleTime > _fixedCPUpdateCycleTime)
        {
            _currentCycleTime -= _fixedCPUpdateCycleTime;
            FixedCPUpdate();
        }
    }
}
