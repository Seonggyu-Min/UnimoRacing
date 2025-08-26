using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
/// </summary>
namespace PJW
{
    public class PlayerEffectRpc : MonoBehaviourPun
    {
        [PunRPC]
        private void RpcObscureOpponents(int ownerActorNr, float duration, float fadeIn, float maxAlpha, float fadeOut)
        {
            if (PhotonNetwork.LocalPlayer.ActorNumber == ownerActorNr) return;
            VisionObscureController.EnsureInScene().Obscure(duration, fadeIn, maxAlpha, fadeOut);
        }
    }
}
