using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Photon.Pun;

namespace PJW
{
    [DisallowMultipleComponent]
    public class DollyLaneSwitcher : MonoBehaviour
    {
        private CinemachineDollyCart cart;
        private CinemachinePathBase leftTrack;
        private CinemachinePathBase rightTrack;

        private PhotonView pv;

        [SerializeField] private bool isControlsInverted = false;
        private float invertUntilTime = 0f;                      

        private void Awake()
        {
            pv = GetComponent<PhotonView>();
            cart = GetComponent<CinemachineDollyCart>();
            TryAutoBindTracks();
        }

        private void Update()
        {
            if (isControlsInverted && Time.time >= invertUntilTime)
            {
                isControlsInverted = false;
            }

            if (pv != null && pv.ViewID != 0 && !pv.IsMine) return;

            // 모바일 터치
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    Vector2 pos = touch.position.ReadValue();
                    int targetIndex = (pos.x < Screen.width * 0.5f) ? 0 : 1;
                    if (isControlsInverted) targetIndex = 1 - targetIndex;
                    ChangeTrack(targetIndex);
                    return;
                }
            }

            // 에디터/PC 마우스
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 pos = Mouse.current.position.ReadValue();
                int targetIndex = (pos.x < Screen.width * 0.5f) ? 0 : 1;
                if (isControlsInverted) targetIndex = 1 - targetIndex;
                ChangeTrack(targetIndex);
                return;
            }
        }

        [PunRPC]
        public void RPCApplyInvertControls(float duration)
        {
            isControlsInverted = true;
            float wantedOff = Time.time + Mathf.Max(0f, duration);
            if (wantedOff > invertUntilTime) invertUntilTime = wantedOff;
        }

        private void TryAutoBindTracks()
        {
            var registry = TrackRegistrySafe();

            if (registry != null && registry.tracks != null && registry.tracks.Length >= 2)
            {
                PickLeftRightFromArray(registry.tracks, out leftTrack, out rightTrack);
            }

            if (leftTrack == null || rightTrack == null)
            {
                var all = FindObjectsOfType<CinemachinePathBase>(true);
                if (all != null && all.Length >= 2)
                {
                    PickLeftRightFromArray(all, out leftTrack, out rightTrack);
                }
            }
        }

        private TrackRegistry TrackRegistrySafe()
        {
            return TrackRegistry.Instance;
        }

        // 트랙 배열에서 이름 기반으로 좌/우 트랙 결정
        private void PickLeftRightFromArray(CinemachinePathBase[] arr, out CinemachinePathBase left, out CinemachinePathBase right)
        {
            left = null;
            right = null;

            foreach (var line in arr)
            {
                if (line == null) continue;
                string bar = line.name.ToLower();
                if (left == null && (bar.Contains("left") || bar.Contains("l_"))) { left = line; continue; }
                if (right == null && (bar.Contains("right") || bar.Contains("r_"))) { right = line; continue; }
            }

            if (left == null || right == null)
            {
                if (arr.Length >= 2)
                {
                    left ??= arr[0];
                    right ??= arr[1] == left && arr.Length >= 3 ? arr[2] : arr[1];
                }
            }
        }

        private void ChangeTrack(int targetIndex)
        {
            if (cart == null || leftTrack == null || rightTrack == null) return;

            var fromPath = cart.m_Path;
            var toPath = (targetIndex == 0) ? leftTrack : rightTrack;

            if (toPath == null || fromPath == null) return;

            float fromMin = fromPath.MinPos;
            float fromMax = fromPath.MaxPos;
            float fromLen = Mathf.Max(0.0001f, fromMax - fromMin);
            float t = Mathf.Clamp01((cart.m_Position - fromMin) / fromLen);

            float toMin = toPath.MinPos;
            float toMax = toPath.MaxPos;
            float toLen = Mathf.Max(0.0001f, toMax - toMin);
            float newPos = toMin + t * toLen;

            cart.m_Path = toPath;
            cart.m_Position = newPos;
        }
    }
}
