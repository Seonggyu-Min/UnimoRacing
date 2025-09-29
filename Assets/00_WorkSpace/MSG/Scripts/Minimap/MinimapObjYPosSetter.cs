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


        private void Start()
        {
            if (_willUseFlatMode)
            {
                SetYPos();
            }
        }

        private void SetYPos()
        {
            Vector3 newPos;
            if (_isPlayer)
            {
                newPos = new Vector3(transform.position.x, _playerYPos, transform.position.z);
            }
            else
            {
                newPos = new Vector3(transform.position.x, _trackYPos, transform.position.z);
            }

            transform.position = newPos;
        }
    }
}
