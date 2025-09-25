using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace MSG
{
    /// <summary>
    /// 안드로이드 빌드에서 세 손가락 터치 두 번을 하면 나오는 디버그 창을 활성화할 지 결정하는 컴포넌트입니다.
    /// </summary>
    public class URPDebuggerEnabler : MonoBehaviour
    {
        [SerializeField] private bool _enableDebugger = false;

        private void Start()
        {
            if (DebugManager.instance != null)
            {
                DebugManager.instance.enableRuntimeUI = _enableDebugger;
            }
        }
    }
}
