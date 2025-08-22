using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 이 스크립트를 각 게임 씬의 비어있는 게임 오브젝트에 붙임
namespace YTW
{
    public class GameSceneController : MonoBehaviour
    {
        void Start()
        {
            // 씬 로딩이 완료되어 Start 함수가 호출되면,
            // SceneManager에게 로딩 화면을 꺼달라고 요청
            if (SceneManager.Instance != null)
            {
                SceneManager.Instance.HideLoadingScreen();
            }
        }
    }

}
