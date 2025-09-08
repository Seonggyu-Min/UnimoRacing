using Cinemachine;
using Photon.Pun;
using System;
using UnityEngine;
using YSJ.Util;

[RequireComponent(typeof(CinemachineDollyCart))] // 이동
public class DollyCartMovement : MonoBehaviour
{
    private const float EPS = 0.0001f;
    
    private bool _isSetup = false;

    private PlayerRaceData _data;
    private CinemachineDollyCart _cart;

    private float _trackLength = 0;
    private float _applicationSpeed;
    private float _cartSpeed;


    private float _predictedNorm;   // 예측 위치
    private float _posUnwrapped;    // 현재 위치
    private float _targetUnwrapped; // 목표 위치

    [SerializeField] private float smoothingHz = 8f;
    [SerializeField] private float snapIfDiffOver = 0.25f;

    public Action<float> OnMovementProgress;

    public bool IsSetup => _isSetup;

    public void Setup(PlayerRaceData data)
    {
        if (data == null)
        {
            this.PrintLog("PlayerRaceData를 받아올 수 없습니다.");
            return;
        }

        _data = data;

        // 속도 지정
        CinemachinePathBase trackPath = TrackPathRegistry.Instance.GetPath(0);
        _trackLength = trackPath.PathLength;

        _applicationSpeed = _data.KartSpeed;
        _cartSpeed = _applicationSpeed / _trackLength;

        _cart = _data.Cart;
        _cart.m_Speed = 0.0f;

        _posUnwrapped = _cart.m_Position;
        _targetUnwrapped = _posUnwrapped;

        _isSetup = true;
    }

    private void Update()
    {
        if (!(_isSetup && _data.IsSetups)) return;

        // 오너 ===============================================================
        if (_data.View.IsMine)
        {
            ChangeSpeed(_data.KartSpeed);
            _cart.m_Position = Wrap(_cart.m_Position + (_cartSpeed * Time.deltaTime));
            OnMovementProgress?.Invoke(_cart.m_Position);

            _posUnwrapped = RewrapToNear(_posUnwrapped, _cart.m_Position);
            _targetUnwrapped = _posUnwrapped;
            return;
        }

        // 원격 ===============================================================

        // 예측이 0 이상
        if (_predictedNorm >= 0f)
        {
            // 렙 보정
            float predicted = Wrap(_predictedNorm);

            // 기존 목표 주변으로 가장 가까운 연속좌표로 언랩
            _targetUnwrapped = RewrapToNear(_targetUnwrapped, predicted);

            // 한 번 소비했으면 리셋(새 패킷 올 때마다 갱신)
            _predictedNorm = -1.0f;
        }

        // 차이가 너무 크면(텔레포트/랩 전환) 스냅으로 정렬
        float diff = Mathf.Abs(_targetUnwrapped - _posUnwrapped);
        if (diff > snapIfDiffOver)
        {
            _posUnwrapped = _targetUnwrapped;
        }
        else
        {
            float alpha = 1f - Mathf.Exp(-Mathf.Max(0f, smoothingHz) * Time.deltaTime);
            _posUnwrapped = Mathf.Lerp(_posUnwrapped, _targetUnwrapped, alpha);
        }

        _cart.m_Position = Wrap(_posUnwrapped);
        OnMovementProgress?.Invoke(_cart.m_Position);
    }

    public void ChangeSpeed(float speed, PhotonMessageInfo info = default)
    {
        if (_applicationSpeed != speed)
        {
            _applicationSpeed = speed;
            _cartSpeed = _applicationSpeed / _trackLength;
        }
    }
    public void SyncPosition(float norm, PhotonMessageInfo info = default)
    {
        double now  = PhotonNetwork.Time;
        double sent = info.SentServerTime;

        float nowNorm = _cart.m_Position;
        float sentNorm = norm;

        float lagTime  = (float)(now - sent);
        _predictedNorm = sentNorm + (_cartSpeed * lagTime);
    }

    // 1 벗어나지 않게 조정
    private float Wrap(float x)
    {
        x = x - Mathf.Floor(x);
        if (x >= 1f) x -= 1f;
        return x;
    }

    private float RewrapToNear(float refVal, float newNorm)
    {
        float refFrac = refVal - Mathf.Floor(refVal);
        float d = newNorm - refFrac;
        if (d > 0.5f) d -= 1f;
        if (d < -0.5f) d += 1f;
        return refVal + d; 
    }
}