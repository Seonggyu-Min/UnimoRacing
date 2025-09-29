using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    [RequireComponent(typeof(PhotonView))]
    public class MinimapRegistrant : MonoBehaviourPun
    {
        [SerializeField] private bool _willUseFlatMode = true;
        [SerializeField] private GameObject _minimapObj;    // 미니맵 높이에 맞게 아래로 깔릴 거니까 transform이 아닌 미니맵 오브젝트 등록

        private void Start()
        {
            if (photonView.IsMine)
            {
                var minimapFollower = FindObjectOfType<MinimapFollower>();
                if (minimapFollower != null)
                {
                    if (_willUseFlatMode)
                    {
                        minimapFollower.RegisterPlayer(_minimapObj.transform);
                    }
                    else
                    {
                        minimapFollower.RegisterPlayer(transform);
                    }
                }
            }
        }
    }
}
