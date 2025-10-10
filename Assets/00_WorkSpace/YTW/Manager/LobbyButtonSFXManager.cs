using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YTW
{
    public class LobbyButtonSFXManager : MonoBehaviour
    {
        [SerializeField] private string sfxName = "Click_SFX";

        private void Start()
        {
            RegisterLobbyButtons();
        }

        private void RegisterLobbyButtons()
        {
            var buttons = FindObjectsOfType<Button>(true);
            foreach (var button in buttons)
            {
                button.onClick.RemoveListener(OnButtonClick);
                button.onClick.AddListener(OnButtonClick);
            }

            Debug.Log($"[LobbyButtonSFXManager] 로비 버튼 효과음 등록 완료 ({buttons.Length}개)");
        }

        private void OnButtonClick()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(sfxName);
        }
    }
}
