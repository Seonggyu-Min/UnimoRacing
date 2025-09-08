using Photon.Pun;
using UnityEngine;
using YSJ.Util;

[RequireComponent(typeof(PhotonView))]
public class DollyCartSync : MonoBehaviourPun, IPunObservable
{
    private bool _isSetup = false;

    private PlayerRaceData _data;

    public bool IsSetup => _isSetup;

    public void Setup(PlayerRaceData data)
    {
        if (data == null)
        {
            this.PrintLog("PlayerRaceData를 받아올 수 없습니다.");
            return;
        }

        _data = data;
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
    }

    // 받기
    private void Receive(PhotonStream stream, PhotonMessageInfo info)
    {
        int     recvTrackIndex    = (int)stream.ReceiveNext();
        float   recvKartSpeed     = (float)stream.ReceiveNext();

        int     recvLap           = (int)stream.ReceiveNext();
        float   recvNorm          = (float)stream.ReceiveNext();

        _data.Controller.ChangeTrack(recvTrackIndex, info);

        _data.Movement.ChangeSpeed(recvKartSpeed, info);
        _data.Movement.SyncPosition(recvNorm, info);
        // _controller.SyncReceive(recvNorm, recvLap, recvSpeed);
    }
}
