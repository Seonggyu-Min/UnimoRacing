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

        private int currentIndex = 0;
        private bool hasInitialized;
        private PhotonView pv; 

        private void Awake()
        {
            pv = GetComponent<PhotonView>() ?? GetComponentInParent<PhotonView>();

            cart = GetComponent<CinemachineDollyCart>();
            if (cart == null) cart = GetComponentInChildren<CinemachineDollyCart>(true);
            if (cart == null) cart = GetComponentInParent<CinemachineDollyCart>();

            TryAutoBindTracks();
            ApplyTrackImmediate(currentIndex);
            hasInitialized = (cart != null && leftTrack != null && rightTrack != null);
        }

        private void Update()
        {
            if (!hasInitialized) return;

            if (pv != null && pv.ViewID != 0 && !pv.IsMine) return;

            // 모바일용
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.wasPressedThisFrame)
                {
                    Vector2 pos = touch.position.ReadValue();
                    if (pos.x < Screen.width * 0.5f) ChangeTrack(0);
                    else ChangeTrack(1);
                    return;
                }
            }

            // --- 에디터/PC용
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 pos = Mouse.current.position.ReadValue();
                if (pos.x < Screen.width * 0.5f) ChangeTrack(0);
                else ChangeTrack(1);
                return;
            }
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

            if (cart != null && cart.m_Path != null)
            {
                if (cart.m_Path == rightTrack) currentIndex = 1;
                else currentIndex = 0;
            }
        }

        private TrackRegistry TrackRegistrySafe()
        {
            return TrackRegistry.Instance;
        }

        private void PickLeftRightFromArray(CinemachinePathBase[] arr, out CinemachinePathBase left, out CinemachinePathBase right)
        {
            left = null;
            right = null;

            foreach (var p in arr)
            {
                if (p == null) continue;
                string n = p.name.ToLower();
                if (left == null && (n.Contains("left") || n.Contains("l_"))) { left = p; continue; }
                if (right == null && (n.Contains("right") || n.Contains("r_"))) { right = p; continue; }
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
            if (targetIndex == currentIndex) return;
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

            currentIndex = targetIndex;
        }

        private void ApplyTrackImmediate(int index)
        {
            if (cart == null) return;

            var toPath = (index == 0) ? leftTrack : rightTrack;
            if (toPath == null) return;

            float t = 0.0f;
            if (cart.m_Path != null)
            {
                float fromMin = cart.m_Path.MinPos;
                float fromMax = cart.m_Path.MaxPos;
                float fromLen = Mathf.Max(0.0001f, fromMax - fromMin);
                t = Mathf.Clamp01((cart.m_Position - fromMin) / fromLen);
            }

            float toMin = toPath.MinPos;
            float toMax = toPath.MaxPos;
            float toLen = Mathf.Max(0.0001f, toMax - toMin);
            float newPos = toMin + t * toLen;

            cart.m_Path = toPath;
            cart.m_Position = newPos;
        }
    }
}
