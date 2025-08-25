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

        DontDestroyOnLoad(gameObject); // ¾ÀÀÌ ¹Ù²î¾îµµ À¯Áö
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

    // ¸ðµç ÆË¾÷ ´Ý±â
    public void CloseAllPopups()
    {
        while (popupStack.Count > 0)
        {
            popupStack.Pop().Close();
        }
    }

    // ¾À ÀüÈ¯
    public void LoadScene(string sceneName)
    {
        CloseAllPopups(); // ¾À ¹Ù²Ù±â Àü¿¡ ¿­·ÁÀÖ´Â ÆË¾÷ ´Ù ´Ý±â
        SceneManager.LoadScene(sceneName);
    }

    public void ReloadCurrentScene()
    {
        CloseAllPopups();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        if (Keyboard.current.escapeKey.wasPressedThisFrame) // µÚ·Î °¡±â
        {
            if (popupStack.Count > 0)
                ClosePopup();
            else
                Debug.Log("´ÝÀ» ÆË¾÷ ¾øÀ½");
        }
    }
}
