using Photon.Pun;
using UnityEngine;

public class ItemInventory : MonoBehaviour
{
    private PhotonView _ownerView;
    private bool _isSetup = false;
    public bool IsSetup => _isSetup;

    public void Setup() { _isSetup = true; }
}