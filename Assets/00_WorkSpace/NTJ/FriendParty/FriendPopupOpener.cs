using UnityEngine;
using UnityEngine.UI;

public class FriendPopupOpener : MonoBehaviour
{
    // 친구 팝업 오브젝트 자체를 인스펙터에서 연결합니다.
    [SerializeField] private GameObject friendsPopupObject;

    // 친구 목록을 여는 버튼입니다.
    [SerializeField] private Button friendsButton;

    private void Awake()
    {
        // 팝업 오브젝트가 초기에는 비활성화 상태여야 합니다.
        if (friendsPopupObject != null)
        {
            friendsPopupObject.SetActive(false);
        }

        // 버튼 클릭 시 팝업을 여는 함수를 연결합니다.
        if (friendsButton != null)
        {
            friendsButton.onClick.AddListener(OpenFriendsPopup);
        }
    }

    public void OpenFriendsPopup()
    {
        if (friendsPopupObject != null)
        {
            // 팝업 오브젝트를 활성화하여 팝업을 엽니다.
            friendsPopupObject.SetActive(true);
        }
    }

    public void CloseFriendsPopup()
    {
        if (friendsPopupObject != null)
        {
            // 팝업 오브젝트를 비활성화하여 팝업을 닫습니다.
            friendsPopupObject.SetActive(false);
        }
    }
}
