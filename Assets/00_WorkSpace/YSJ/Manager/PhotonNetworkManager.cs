using System;
using YSJ.Util;

// - 이걸 만든 이유
// 여러군데에서 네트워크 포톤을 사용하면 추적하기 실행점을 찾는데 많은 시간이 소요되므로, 현재와 같이
// 네트워크 관련 이벤트를 관리해주는 매니저가 필요하다고 생각했다.

public class PhotonNetworkManager : SimpleSingletonPun<PhotonNetworkManager>
{
    public Action OnActionConnectedToMaster;
    public Action OnActionJoinedLobby;
    public Action OnActionOnJoinedRoom;

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
}
