using UnityEngine;
using Cinemachine;
using Photon.Pun;

namespace PJW
{
    [DisallowMultipleComponent]
    public class DollyLaneBinder : MonoBehaviour
    {
        [Header("참조")]
        [SerializeField] private CinemachineDollyCart cart;
        [SerializeField] private CinemachinePathBase path;

        [Header("설정")]
        [SerializeField] private bool spawnAtPathStart = true;

        [Header("카메라 바인딩")]
        [SerializeField] private CinemachineVirtualCamera vcam;
        [SerializeField] private bool bindFollow = true;
        [SerializeField] private bool bindLookAt = true;
        [SerializeField] private bool onlyIfMine = true;

        [Header("카메라 위치 설정")]
        [SerializeField] private Vector3 followOffset = new Vector3(0f, 3f, -6f);
        [SerializeField] private Vector3 trackPathOffset = Vector3.zero;
        [SerializeField] private bool applyPositionOnStart = true;

        private PhotonView pv;

        private void Awake()
        {
            pv = GetComponentInParent<PhotonView>();
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            AutoWireReferences();
            TryAssignPathFromRegistry();
            TryApplyPathToCart();
            BindCamera();
            if (applyPositionOnStart) ApplyCameraPosition();
        }

        private void AutoWireReferences()
        {
            if (cart == null)
                cart = GetComponentInChildren<CinemachineDollyCart>(true) ?? GetComponentInParent<CinemachineDollyCart>();

            if (vcam == null)
                vcam = GetComponentInChildren<CinemachineVirtualCamera>(true)
                    ?? GetComponentInParent<CinemachineVirtualCamera>()
                    ?? FindObjectOfType<CinemachineVirtualCamera>();
        }

        private void TryAssignPathFromRegistry()
        {
            if (path != null) return;

            path = TrackRegistry.Instance?.GetPathForLocalPlayer(pv.OwnerActorNr);
        }

        private void TryApplyPathToCart()
        {
            if (cart != null && cart.m_Path == null && path != null)
            {
                cart.m_Path = path;
                Debug.Log($"[DollyLaneBinder] DollyCart에 path 적용됨: {path.name}");
            }

            if (spawnAtPathStart && cart?.m_Path != null)
            {
                cart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;
                cart.m_Position = 0f;
            }
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

        // 외부 제어용
        public void SetFollowOffset(Vector3 offset)
        {
            followOffset = offset;
            ApplyCameraPosition();
        }

        public void SetTrackPathOffset(Vector3 offset)
        {
            trackPathOffset = offset;
            ApplyCameraPosition();
        }

        public void RebindCamera()
        {
            BindCamera();
            ApplyCameraPosition();
        }
    }
}
