using Photon.Pun;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private PhotonView ownerView;
    private bool _isSetup = false;
    public bool IsSetup => _isSetup;

    public void Setup() { _isSetup = true; }
}