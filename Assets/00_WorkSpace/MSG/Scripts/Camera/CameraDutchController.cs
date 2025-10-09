using Cinemachine;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace MSG
{
    public class CameraDutchController : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera _vCam;

        private Transform target;
        private float currentDutch;

        private void Start()
        {
            StartCoroutine(Wait());
        }

        private IEnumerator Wait()
        {
            yield return new WaitForSeconds(1f);    // 플레이어 생성이 보통 늦게 됨
                                                    // 결국은 1초 기다리는 것이 아니라 플레이어 스폰된 Action 등을 받아서 그 때 초기화해줘야 될 듯

            if (_vCam == null)
            {
                _vCam = GetComponent<CinemachineVirtualCamera>();
            }

            if (_vCam != null)
            {
                target = _vCam.LookAt;
            }

            if (target == null)
            {
                Debug.Log("[CameraDutchController] target이 null이어서 Find로 진입");
                var players = FindObjectsOfType<PlayerRaceData>();

                var me = players.FirstOrDefault(p => p.View.IsMine);
                target = me.transform;
            }
        }

        private void LateUpdate()
        {
            if (_vCam == null)
            {
                //Debug.Log("[CameraDutchController] _vCam == null");
                return;
            }
            if (target == null)
            {
                //Debug.Log("[CameraDutchController] target == null");
                return;
            }

            float rawZ = target.localRotation.eulerAngles.z;
            currentDutch = Mathf.DeltaAngle(0f, rawZ);

            var lens = _vCam.m_Lens;
            lens.Dutch = currentDutch;
            _vCam.m_Lens = lens;
        }
    }
}
