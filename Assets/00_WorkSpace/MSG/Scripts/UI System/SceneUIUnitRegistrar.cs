using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// SceneUIPanelRegistrar의 하위 오브젝트 중 UIPanel 컴포넌트를 가진 오브젝트들을 찾아 자동으로 등록해주는 컴포넌트입니다.
    /// </summary>
    public class SceneUIUnitRegistrar : MonoBehaviour
    {
        private void Awake()
        {
            foreach (var unit in GetComponentsInChildren<UIUnit>(true))
            {
                string key = unit.name;
                UIUnit go = unit;

                UIManager.Instance.RegisterUnit(key, go);
            }
        }

        private void OnDestroy()
        {
            // 만약 씬이 바뀌어도 유지되어야 하는 유닛이 있다면 이거 수정하고 직접 key 넣어서 지우도록 바꿔야 됨
            UIManager.Instance.ClearUnit();
        }
    }
}
