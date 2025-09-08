using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    // 친구 추가가 있을 때, 빨간 원을 활성화해주는 컴포넌트
    public class FriendRequestTracker : MonoBehaviour
    {
        [SerializeField] private FriendBoxSubscriber _friendBoxSubscriber;

        [SerializeField] private GameObject _redCircle1; // 로비에서   보일, 친구 추가가 있다면 생기는 빨간 원
        [SerializeField] private GameObject _redCircle2; // 친구창에서 보일, 친구 추가가 있다면 생기는 빨간 원


        private void OnEnable()
        {
            if (_friendBoxSubscriber == null) _friendBoxSubscriber = FindObjectOfType<FriendBoxSubscriber>();

            if (_friendBoxSubscriber != null)
            {
                _friendBoxSubscriber.OnPendingCountChanged += HandlePendingChanged;
                HandlePendingChanged(_friendBoxSubscriber.PendingCount);
            }
            else
            {
                Debug.LogWarning("[FriendRequestTracker] _friendBoxSubscriber가 null입니다.");
            }
        }

        private void OnDisable()
        {
            if (_friendBoxSubscriber != null)
            {
                _friendBoxSubscriber.OnPendingCountChanged -= HandlePendingChanged;
            }
        }

        private void HandlePendingChanged(int pendingCount)
        {
            bool active = pendingCount > 0;
            if (_redCircle1) _redCircle1.SetActive(active);
            if (_redCircle2) _redCircle2.SetActive(active);
        }
    }
}
