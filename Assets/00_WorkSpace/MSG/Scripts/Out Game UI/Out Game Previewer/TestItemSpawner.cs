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


        private void Start()
        {
            ScrollItemChecker.Instance.Register(_scrollRect, _unimos);
        }
    }
}
