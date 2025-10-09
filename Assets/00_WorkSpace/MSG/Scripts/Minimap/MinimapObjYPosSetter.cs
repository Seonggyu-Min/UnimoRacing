using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// 꼬인 맵에서 미니맵이 너무 입체적이라 미니맵 오브젝트들을 낮추는 컴포넌트입니다.
    /// </summary>
    public class MinimapObjYPosSetter : MonoBehaviour
    {
        [SerializeField] private bool _willUseFlatMode = true;

        [SerializeField] private bool _isPlayer;    // 플레이어 UI인지, 트랙 UI인지

        [ShowField(nameof(_isPlayer))]
        [SerializeField] private float _playerYPos = 0.1f;
        [HideField(nameof(_isPlayer))]
        [SerializeField] private float _trackYPos = 0f;

        [SerializeField] private Transform _parentT; // 부모 (레이서)

        private void Start()
        {
            if (_willUseFlatMode && !_isPlayer)
            {
                SetYForTrack();
            }
            else if (_willUseFlatMode && _isPlayer)
            {
                MoveMinimapObjForRoot();
            }
        }

        private void LateUpdate()
        {
            if (_isPlayer && _willUseFlatMode)
            {
                SetYForPlayer();
            }
        }

        private void SetYForTrack()
        {
            transform.position = new Vector3(transform.position.x, _trackYPos, transform.position.z);

            transform.rotation = Quaternion.identity;
        }

        private void MoveMinimapObjForRoot()
        {
            transform.SetParent(null);
        }

        private void SetYForPlayer()
        {
            if (_parentT == null) return;

            Vector3 p = _parentT.position;
            p.y = _playerYPos;
            transform.position = p;

            Vector3 fwd = _parentT.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 1e-6f)
            {
                fwd = Vector3.forward;
            }
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }
    }
}
