using System;
using YSJ.Util;

// - �̰� ���� ����
// ������������ ��Ʈ��ũ ������ ����ϸ� �����ϱ� �������� ã�µ� ���� �ð��� �ҿ�ǹǷ�, ����� ����
// ��Ʈ��ũ ���� �̺�Ʈ�� �������ִ� �Ŵ����� �ʿ��ϴٰ� �����ߴ�.

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
