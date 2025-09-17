using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    // 실제 사용 시 ShopManager가 아이템 스폰을 담당하므로, 테스트용으로만 사용하고 있습니다.
    public class TestItemSpawner : MonoBehaviour
    {
        [SerializeField] private ScrollRect _scrollRect;

        [SerializeField] private List<ShopUIBinder> _unimos = new();
        [SerializeField] private List<ShopUIBinder> _karts = new();

        // 실제로는 스크롤 뷰가 켜질 때 등록하고, 꺼질 때 해제해야 함
        // 근데 Start에서 테스트용으로 호출했고, 초기화가 안됐는데 Register해서 코루틴으로 한 프레임 기다림
        private void Start()
        {
            StartCoroutine(Wait());
        }

        private IEnumerator Wait()
        {
            yield return null;

            Dictionary<int, ShopUIBinder> unimoDict = new();
            foreach (var unimo in _unimos)
                unimoDict.Add(unimo.UnimoId, unimo);

            ScrollItemChecker.Instance.Register(_scrollRect, unimoDict);
        }
    }
}
