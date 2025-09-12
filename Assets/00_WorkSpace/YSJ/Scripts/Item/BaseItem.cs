using Photon.Pun;
using UnityEngine;

public enum OwnerType
{
    None,
    Local,

}

public class BaseItem : MonoBehaviour
{
    private PhotonView _owner;
    private int _itemId;

    public void Start()
    {
        
    }

    public void Setup() 
    { 
    
    }

    private void OnTriggerEnter(Collider other)
    {
        
    }
}