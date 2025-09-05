using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class DBUpdateTester : MonoBehaviour
    {
        #region Editor Attributes
        [HorizontalGroup(true, nameof(btnSp0), nameof(btnSp1), nameof(btnSp2))]
        [SerializeField] private Void rowSpeedGroup;

        [ButtonField(nameof(OnClickGetSpeedOf0), "0번 스피드", 28f)]
        [SerializeField, HideInInspector] private Void btnSp0;

        [ButtonField(nameof(OnClickGetSpeedOf1), "1번 스피드", 28f)]
        [SerializeField, HideInInspector] private Void btnSp1;

        [ButtonField(nameof(OnClickGetSpeedOf2), "2번 스피드", 28f)]
        [SerializeField, HideInInspector] private Void btnSp2;


        [HorizontalGroup(true, nameof(btnCost0), nameof(btnCost1), nameof(btnCost2))]
        [SerializeField] private Void rowCostGroup;

        [ButtonField(nameof(OnClickGetCostOf0), "0번 비용", 28f)]
        [SerializeField, HideInInspector] private Void btnCost0;

        [ButtonField(nameof(OnClickGetCostOf1), "1번 비용", 28f)]
        [SerializeField, HideInInspector] private Void btnCost1;

        [ButtonField(nameof(OnClickGetCostOf2), "2번 비용", 28f)]
        [SerializeField, HideInInspector] private Void btnCost2;


        [HorizontalGroup(true, nameof(btnUnimoCost3), nameof(btnUnimoCost4), nameof(btnUnimoCost5))]
        [SerializeField] private Void rowUnimoCostGroup;

        [ButtonField(nameof(OnClickGetCostOfUnimo3), "3번 유니모 비용", 28f)]
        [SerializeField, HideInInspector] private Void btnUnimoCost3;

        [ButtonField(nameof(OnClickGetCostOfUnimo4), "4번 유니모 비용", 28f)]
        [SerializeField, HideInInspector] private Void btnUnimoCost4;

        [ButtonField(nameof(OnClickGetCostOfUnimo5), "5번 유니모 비용", 28f)]
        [SerializeField, HideInInspector] private Void btnUnimoCost5;

        #endregion


        #region Example Methods
        private void OnClickGetSpeedOf0() =>
            PatchService.Instance.GetSpeedOfKart(0,
                speed => Debug.Log($"[DBUpdateTester] 0번 카트 속도: {speed}"),
                err => Debug.LogWarning($"[DBUpdateTester] 0번 카트 속도 조회 실패: {err}")
                );
        private void OnClickGetSpeedOf1() =>
            PatchService.Instance.GetSpeedOfKart(1,
                speed => Debug.Log($"[DBUpdateTester] 1번 카트 속도: {speed}"),
                err => Debug.LogWarning($"[DBUpdateTester] 1번 카트 속도 조회 실패: {err}")
                );
        private void OnClickGetSpeedOf2() =>
            PatchService.Instance.GetSpeedOfKart(2,
                speed => Debug.Log($"[DBUpdateTester] 2번 카트 속도: {speed}"),
                err => Debug.LogWarning($"[DBUpdateTester] 2번 카트 속도 조회 실패: {err}")
                );
        private void OnClickGetCostOf0() =>
            PatchService.Instance.GetCostOfKart(
                0,
                (cost, moneyType) => Debug.Log($"[DBUpdateTester] 0번 카트 업그레이드 비용: {cost}, 화폐 타입 = {moneyType}"),
                err => Debug.LogError($"[Test] 0번 카트 비용 조회 실패: {err}")
                );
        private void OnClickGetCostOf1() =>
            PatchService.Instance.GetCostOfKart(
                1,
                (cost, moneyType) => Debug.Log($"[DBUpdateTester] 1번 카트 업그레이드 비용: {cost}, 화폐 타입 = {moneyType}"),
                err => Debug.LogError($"[Test] 1번 카트 비용 조회 실패: {err}")
                );
        private void OnClickGetCostOf2() => 
            PatchService.Instance.GetCostOfKart(
                2,
                (cost, moneyType) => Debug.Log($"[DBUpdateTester] 2번 카트 업그레이드 비용: {cost}, 화폐 타입 = {moneyType}"),
                err => Debug.LogError($"[Test] 2번 카트 비용 조회 실패: {err}")
                );
        private void OnClickGetCostOfUnimo3() =>
            PatchService.Instance.GetCostOfUnimo(
                3,
                (cost, moneyType) => Debug.Log($"[DBUpdateTester] 3번 유니모 구매 비용: {cost}, 화폐 타입 = {moneyType}"),
                err => Debug.LogError($"[Test] 3번 유니모 비용 조회 실패: {err}")
                );
        private void OnClickGetCostOfUnimo4() =>
            PatchService.Instance.GetCostOfUnimo(
                4,
                (cost, moneyType) => Debug.Log($"[DBUpdateTester] 4번 유니모 구매 비용: {cost}, 화폐 타입 = {moneyType}"),
                err => Debug.LogError($"[Test] 4번 유니모 비용 조회 실패: {err}")
                );
        private void OnClickGetCostOfUnimo5() =>
            PatchService.Instance.GetCostOfUnimo(
                5,
                (cost, moneyType) => Debug.Log($"[DBUpdateTester] 5번 유니모 구매 비용: {cost}, 화폐 타입 = {moneyType}"),
                err => Debug.LogError($"[Test] 5번 유니모 비용 조회 실패: {err}")
                );
        #endregion
    }
}
