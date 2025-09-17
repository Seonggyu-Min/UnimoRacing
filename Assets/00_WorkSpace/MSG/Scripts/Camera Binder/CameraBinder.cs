using Cinemachine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class CameraBinder : MonoBehaviour
    {
        private CinemachineDollyCart cart;
        private PhotonView pv;

        [Header("카메라 바인딩")]
        private CinemachineVirtualCamera vcam;
        private bool bindFollow = true;
        private bool bindLookAt = true;
        private bool onlyIfMine = true;

        [Header("카메라 위치 설정")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 3f, -6f);
        [SerializeField] private Vector3 trackPathOffset = Vector3.zero;
        [SerializeField] private bool applyPositionOnStart = true;

        private void Awake()
        {
            pv = GetComponentInParent<PhotonView>();
        }

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            if (vcam == null)
                vcam = FindObjectOfType<CinemachineVirtualCamera>();

            if (cart == null)
                cart = GetComponentInParent<CinemachineDollyCart>();

            BindCamera();
            ApplyCameraPosition();
        }

        private void BindCamera()
        {
            if (vcam == null || cart == null) return;
            if (onlyIfMine && pv != null && !pv.IsMine) return;

            if (bindFollow && vcam.Follow == null)
                vcam.Follow = cart.transform;

            if (bindLookAt && vcam.LookAt == null)
                vcam.LookAt = cart.transform;
        }

        // 카메라 위치 설정
        private void ApplyCameraPosition()
        {
            if (vcam == null) return;

            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
                transposer.m_FollowOffset = followOffset;

            var framing = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framing != null)
            {
                framing.m_TrackedObjectOffset = new Vector3(followOffset.x, followOffset.y, 0f);
                framing.m_CameraDistance = Mathf.Abs(followOffset.z);
            }

            var tpf = vcam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (tpf != null)
            {
                tpf.ShoulderOffset = new Vector3(followOffset.x, followOffset.y, 0f);
                tpf.CameraDistance = Mathf.Abs(followOffset.z);
            }

            var tracked = vcam.GetCinemachineComponent<CinemachineTrackedDolly>();
            if (tracked != null)
                tracked.m_PathOffset = trackPathOffset;
        }
    }
}
