using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace MSG
{
    public enum AttatchmentType
    {
        //                 부착될 위치                         부착될 프리팹
        Head,           // 머리 위                             유니모
        BackBooster,    // 카트 뒤 부스터                      카트
        Forward,        // 앞                                  유니모
        Center,         // 중앙                                유니모
        // 더 있을 수도 있음
    }

    [Serializable]
    public class PlayerAttatchWrapper
    {
        public AttatchmentType AttatchmentType;
        public GameObject AttachObj;
        // 시간은 그 아이템의 SO를 직접 보고 하는 것으로 결정했음
    }

    // 유니모랑 카트 둘 다 있어야 됨. 중복으로 등록해도 같이 합쳐서 받아줌
    public class PlayerAttatchRegistrant : MonoBehaviourPun
    {
        [SerializeField] private PlayerAttatchWrapper[] _wrappers;


        private void Start()
        {
            // 일단 빌드할 때는 이거 주석처리 하고 할 듯
            if (SceneManager.GetActiveScene().buildIndex == 2)  // 인게임 씬일 때만 등록 시도
            {
                Debug.Log($"[PlayerAttatchRegistrant] {gameObject.name}등록 시도");
                PlayerEffectManager.Instance.RegisterPoints(photonView.Owner.UserId, _wrappers);
            }
        }
    }
}
