using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private Stack<PopupBase> popupStack = new Stack<PopupBase>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject); // ���� �ٲ� ����
    }

    public void OpenPopup(PopupBase popup)
    {
        if (popup == null) return;

        popup.Open();
        popupStack.Push(popup);
    }

    public void ClosePopup()
    {
        if (popupStack.Count == 0) return;

        PopupBase popup = popupStack.Pop();
        popup.Close();
    }

    // ��� �˾� �ݱ�
    public void CloseAllPopups()
    {
        while (popupStack.Count > 0)
        {
            popupStack.Pop().Close();
        }
    }

    // �� ��ȯ
    public void LoadScene(string sceneName)
    {
        CloseAllPopups(); // �� �ٲٱ� ���� �����ִ� �˾� �� �ݱ�
        SceneManager.LoadScene(sceneName);
    }

    public void ReloadCurrentScene()
    {
        CloseAllPopups();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame) // �ڷ� ����
        {
            if (popupStack.Count > 0)
                ClosePopup();
            else
                Debug.Log("���� �˾� ����");
        }
    }
}
