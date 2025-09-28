using Photon.Pun;
using UnityEngine;
using YSJ;
using YSJ.Util;

[DisallowMultipleComponent]
[RequireComponent(typeof(PhotonView))]
public class InventoryRPCProxy : MonoBehaviourPun
{
    [PunRPC]
    public void RPC_GrantItemById(int itemIdInt, bool isDelaySave = false, bool isForceSave = false, bool isSaveItemAction = true)
    {
        this.PrintLog("ItemInventory에 Set 시도");
        var inv = GetComponentInChildren<ItemInventory>(true);
        if (inv == null)
        {
            this.PrintLog("ItemInventory 미발견", LogType.Warning);
            return;
        }

        var so = ItemManager.Instance.ResolveById((ItemId)itemIdInt);
        if (so == null || so.itemID == ItemId.None)
        {
            this.PrintLog($"Resolve 실패 or None: {(ItemId)itemIdInt}", LogType.Warning);
            return;
        }

        this.PrintLog("ItemInventory에 Set 처리 시도");
        inv.SaveItem(so, isDelaySave, isForceSave, isSaveItemAction);
    }
}
