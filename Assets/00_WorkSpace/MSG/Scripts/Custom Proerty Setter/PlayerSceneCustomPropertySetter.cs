using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// 로비에 진입했을 때 플레이어 씬 커스텀 프로퍼티를 세팅해주는 컴포넌트입니다.
    /// </summary>
    public class PlayerSceneCustomPropertySetter : MonoBehaviour
    {
        private void Start()
        {
            PlayerManager.Instance.SetPlayerCPCurrentScene(SceneID.LobbyScene);
        }
    }
}
