using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class UIManager : Singleton<UIManager>
    {
        private Dictionary<string, UIUnit> _units = new();


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) DebugUnits();
        }


        #region Public Methods

        public void RegisterUnit(string key, UIUnit unit)
        {
            if (!_units.ContainsKey(key))
            {
                _units.Add(key, unit);
            }
            else
            {
                Debug.LogWarning($"유닛 키{key}가 이미 등록되어있습니다. 등록에 실패했습니다.");
            }
        }

        public void Show(string key)
        {
            if (_units.TryGetValue(key, out UIUnit unit))
            {
                unit.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"유닛 키{key}가 등록되어 있지 않습니다.");
            }
        }

        public void Hide(string key, Action onComplete = null)
        {
            if (_units.TryGetValue(key, out UIUnit unit))
            {
                unit.HideAnimation(onComplete);
            }
            else
            {
                Debug.LogWarning($"유닛 키{key}가 등록되어 있지 않습니다.");
            }
        }

        //public UIUnit GetUnit(string key)
        //{
        //    if (_units.TryGetValue(key, out UIUnit unit))
        //    {
        //        return unit;
        //    }
        //    else
        //    {
        //        Debug.LogError($"유닛 키{key}가 등록되어 있지 않습니다.");
        //        return null;
        //    }
        //}

        #endregion

        private void DebugUnits()
        {
            foreach (var unit in _units) Debug.Log($"등록된 유닛: {unit.Key}");
        }
    }
}
