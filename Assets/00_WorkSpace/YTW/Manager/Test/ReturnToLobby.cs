using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace YTW
{
    public class ReturnToLobby : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            // ESC 키가 눌렸는지 확인
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // 씬 매니저가 로딩 중이 아닐 때만 실행
                if (Manager.Scene != null && !Manager.Scene.IsLoading)
                {
                    Debug.Log("ESC 키 입력 감지. 로비 씬으로 돌아갑니다.");

                    // YTW_TestScene1 (로비 씬으로 가정)으로 씬 전환 요청
                    Manager.Scene.LoadGameScene(SceneType.YTW_TestScene1);
                }
            }
        }
    }
}