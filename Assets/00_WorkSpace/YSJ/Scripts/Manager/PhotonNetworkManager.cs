using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Text;
using YSJ.Util;

// - 이걸 만든 이유
// 여러군데에서 네트워크 포톤을 사용하면 추적하기 실행점을 찾는데 많은 시간이 소요되므로, 현재와 같이
// 네트워크 관련 이벤트를 관리해주는 매니저가 필요하다고 생각했다.

public class PhotonNetworkManager : SimpleSingletonPun<PhotonNetworkManager>
{
    public Action OnActionConnectedToMaster;

    public Action OnActionJoinedLobby;

    public Action OnActionOnJoinedRoom;
    public Action OnActionLeftRoom;

    public Action<Player> OnActionPlayerEnteredRoom;
    public Action<Player> OnActionPlayerLeftRoom;

    public Action<Player, Hashtable> OnActionPlayerPropertiesUpdate;
    public Action<Hashtable> OnActionRoomPropertiesUpdate;

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        OnActionConnectedToMaster?.Invoke();
    }

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        OnActionJoinedLobby?.Invoke();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        OnActionOnJoinedRoom?.Invoke();
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        OnActionLeftRoom?.Invoke();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
        OnActionPlayerPropertiesUpdate?.Invoke(targetPlayer, changedProps);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);
        OnActionPlayerEnteredRoom?.Invoke(newPlayer);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        OnActionPlayerLeftRoom?.Invoke(otherPlayer);
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        StringBuilder sb = new();
        
        sb.Append($"[RoomProperty Updated]\n");
        foreach (var key in propertiesThatChanged.Keys)
            sb.Append($"Key: {key}, Value: {propertiesThatChanged[key]}\n");
        
        this.PrintLog(sb.ToString());
        
        OnActionRoomPropertiesUpdate?.Invoke(propertiesThatChanged);
    }

    // PlayerList는 이미 ActorNumber 오름차순 정렬되어 있다고 한다.
    public int GetStartIndexFor(Player p)
    {
        var arr = PhotonNetwork.PlayerList; 
        for (int i = 0; i < arr.Length; i++)
            if (arr[i].ActorNumber == p.ActorNumber)
                return i;
        return 0;
    }
}
