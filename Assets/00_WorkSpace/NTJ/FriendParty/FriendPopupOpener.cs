using UnityEngine;
using UnityEngine.UI;

public class FriendPopupOpener : MonoBehaviour
{
    // ģ�� �˾� ������Ʈ ��ü�� �ν����Ϳ��� �����մϴ�.
    [SerializeField] private GameObject friendsPopupObject;

    // ģ�� ����� ���� ��ư�Դϴ�.
    [SerializeField] private Button friendsButton;

    private void Awake()
    {
        // �˾� ������Ʈ�� �ʱ⿡�� ��Ȱ��ȭ ���¿��� �մϴ�.
        if (friendsPopupObject != null)
        {
            friendsPopupObject.SetActive(false);
        }

        // ��ư Ŭ�� �� �˾��� ���� �Լ��� �����մϴ�.
        if (friendsButton != null)
        {
            friendsButton.onClick.AddListener(OpenFriendsPopup);
        }
    }

    public void OpenFriendsPopup()
    {
        if (friendsPopupObject != null)
        {
            // �˾� ������Ʈ�� Ȱ��ȭ�Ͽ� �˾��� ���ϴ�.
            friendsPopupObject.SetActive(true);
        }
    }

    public void CloseFriendsPopup()
    {
        if (friendsPopupObject != null)
        {
            // �˾� ������Ʈ�� ��Ȱ��ȭ�Ͽ� �˾��� �ݽ��ϴ�.
            friendsPopupObject.SetActive(false);
        }
    }
}
