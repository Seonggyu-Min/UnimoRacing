using Photon.Pun;
using System.Collections.Generic;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace YSJ.Net
{
    // 스냅샷(> 특정 시점을 말함 = 커스텀프터 저장임)
    public static class ItemUtil
    {
        public const string RoomKeyPrefix = "ItemBox:";

        public static string Key(int viewId) => $"{RoomKeyPrefix}{viewId}";

        // 단일 박스 스냅샷 저장
        public static void SetSnapshot(int viewId, double respawnEpoch)
        {
            if (PhotonNetwork.CurrentRoom == null) return;
            var table = new Hashtable { { Key(viewId), respawnEpoch } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(table);
        }

        // 여러 박스 스냅샷 배치 저장
        public static void SetManySnapshots(Dictionary<int, double> viewIdToEpoch)
        {
            if (PhotonNetwork.CurrentRoom == null) return;
            var table = new Hashtable();
            foreach (var kv in viewIdToEpoch)
                table[Key(kv.Key)] = kv.Value;
            PhotonNetwork.CurrentRoom.SetCustomProperties(table);
        }
    }
}
