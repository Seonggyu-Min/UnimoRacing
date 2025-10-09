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
        private CinemachineVirtualCamera mainCam;
        private CinemachineVirtualCamera fallbackCam;
        private bool bindFollow = true;
        private bool bindLookAt = true;
        private bool onlyIfMine = true;

        [Header("카메라 위치 설정")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 3f, -6f);
        [SerializeField] private Vector3 fallbackOffset = new Vector3(0f, 5f, -2f);     // 안보일 때는 위에서 내려다보게
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
            //if (mainCam == null)
            //    mainCam = FindObjectOfType<CinemachineVirtualCamera>();
            var cams = FindObjectsOfType<CinemachineVirtualCamera>();

            if (cams.Length != 2)
            {
                Debug.LogWarning("[CameraBinder] 씬에 CinemachineVirtualCamera가 2개가 아닙니다");
            }
            else
            {
                // 이거 좋은 패턴은 아닌데 일단 씀
                if (cams[0].name.Contains("main"))
                {
                    mainCam = cams[0];
                    fallbackCam = cams[1];
                }
                else if (cams[0].name.Contains("fallback"))
                {
                    mainCam = cams[1];
                    fallbackCam = cams[0];
                }
                else
                {
                    Debug.LogWarning("main 또는 fallback 이름을 찾을 수 없습니다");
                }
            }

            if (cart == null)
                cart = GetComponentInParent<CinemachineDollyCart>();

            BindCamera();
            ApplyCameraPosition();
        }

        private void BindCamera()
        {
            // Early Return
            if (mainCam == null || fallbackCam == null || cart == null) return;
            if (onlyIfMine && pv != null && !pv.IsMine) return;

            // Follow 등록
            if (bindFollow && mainCam.Follow == null)
                mainCam.Follow = cart.transform;
            if (bindFollow && fallbackCam.Follow == null)
                fallbackCam.Follow = cart.transform;

            // LookAt 등록
            if (bindLookAt && mainCam.LookAt == null)
                mainCam.LookAt = cart.transform;
            if (bindLookAt && fallbackCam.LookAt == null)
                fallbackCam.LookAt = cart.transform;
        }

        // 카메라 위치 설정
        private void ApplyCameraPosition()
        {
            if (mainCam == null || fallbackCam == null) return;

            ApplyOffsetToCam(mainCam, followOffset, trackPathOffset); // 메인 카메라 오프셋 적용
            ApplyOffsetToCam(fallbackCam, fallbackOffset, trackPathOffset); // 폴백 카메라 오프셋 적용
        }

        private void ApplyOffsetToCam(CinemachineVirtualCamera cam, Vector3 offset, Vector3 pathOffset)
        {
            if (cam == null) return;

            var transposer = cam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer != null)
                transposer.m_FollowOffset = offset;

            var framing = cam.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framing != null)
            {
                framing.m_TrackedObjectOffset = new Vector3(offset.x, offset.y, 0f);
                framing.m_CameraDistance = Mathf.Abs(offset.z);
            }

            var tpf = cam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
            if (tpf != null)
            {
                tpf.ShoulderOffset = new Vector3(offset.x, offset.y, 0f);
                tpf.CameraDistance = Mathf.Abs(offset.z);
            }

            var tracked = cam.GetCinemachineComponent<CinemachineTrackedDolly>();
            if (tracked != null)
                tracked.m_PathOffset = pathOffset;
        }
    }
}
