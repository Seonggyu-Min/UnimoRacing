using PJW;
using UnityEngine;

public class UnimoSynergySystem : MonoBehaviour
{
    private bool _isSetup = false;

    private PlayerRaceData _data;
    private PlayerItemInventory _inventory;

    public bool IsSetup => _isSetup;

    public void Setup(PlayerRaceData data)
    {
        _data = data;
        _inventory = GetComponent<PlayerItemInventory>();
        _isSetup = true;
    }


}