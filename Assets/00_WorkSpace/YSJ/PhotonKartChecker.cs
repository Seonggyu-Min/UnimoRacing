using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

public class PhotonKartChecker : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerManager.Instance.SetPlayerCPRaceLoaded(true);
    }
}
