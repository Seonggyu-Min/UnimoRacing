using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MissionProcessLoader : MonoBehaviour
    {
        [SerializeField] private MissionUIBehaviour _uiPrefab;


        private void OnEnable()
        {
            
        }

        private void OnDisable()
        {
            // 정리?
        }



        private void LoadAll()
        {
            // 순회하면서 생성?
            // 이번에는 풀링도 하면 좋을 듯


        }
    }
}
