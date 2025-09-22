using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class GetSpeedTester : MonoBehaviour
    {
        private void Start()
        {
            PatchService.Instance.GetSpeedOfKart(10001,
                speed => Debug.Log($"[DBUpdateTester] 10001번 카트 속도: {speed}"),
                err => Debug.LogWarning($"[DBUpdateTester] 10001번 카트 속도 조회 실패: {err}"));
        }
    }
}
