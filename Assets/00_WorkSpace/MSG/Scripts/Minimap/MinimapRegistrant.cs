using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    [RequireComponent(typeof(PhotonView))]
    public class MinimapRegistrant : MonoBehaviourPun
    {
        private void Start()
        {
            if (photonView.IsMine)
            {
                var minimapFollower = FindObjectOfType<MinimapFollower>();
                if (minimapFollower != null)
                {
                    minimapFollower.RegisterPlayer(transform);
                }
            }
        }
    }
}
