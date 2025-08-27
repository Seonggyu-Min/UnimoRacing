using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonKartLoadedChecker : MonoBehaviour
{
    private void OnEnable()
    {
        PlayerManager.Instance.SetRaceLoadedSelection();
    }

    private void OnDisable()
    {
        
    }
}
