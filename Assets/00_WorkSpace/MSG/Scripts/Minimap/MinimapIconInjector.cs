using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MinimapIconInjector : MonoBehaviourPun
    {
        [SerializeField] private GameObject _arrowObj;  // 자신 카트에 쓰일 화살표 오브젝트
        [SerializeField] private GameObject _circleObj; // 타인 카트에 쓰일 동그라미 오브젝트

        private void Start()
        {
            /*
            if (플레이어 스폰이 안되어있으면)
                플레이어 스폰이 었다는 액션 += Inject;
            else 스폰이 되었으면
                Inject();
            */

            Inject();
        }

        private void Inject()
        {
            if (photonView.IsMine)
            {
                _arrowObj?.SetActive(true);
                _circleObj?.SetActive(false);
            }
            else
            {
                _arrowObj?.SetActive(false);
                _circleObj?.SetActive(true);
            }
        }
    }
}
