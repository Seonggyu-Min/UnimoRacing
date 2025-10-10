using MSG;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Purchasing;


namespace MSG
{
    public class IAPManager : MonoBehaviour
    {
        #region Fields
        [SerializeField] private IAPTable _iapTable;

        private const string test_iap = "test_iap";

        private StoreController _store;
        private readonly Dictionary<string, Product> _productCache = new();

        #endregion

        #region Unity Methods

        private async void Start()
        {
            await InitIAP();
        }

        private void OnDestroy()
        {
            if (_store != null)
            {
                HookEvents(false);
            }
        }

        #endregion

        #region Init

        public async Task InitIAP()
        {
            _store = new StoreController();
            HookEvents(true);

            await _store.Connect();

            var productDefinitions = new List<ProductDefinition>();
            foreach (var entry in _iapTable.Entries)
            {
                productDefinitions.Add(new ProductDefinition(entry.ProductId, ProductType.Consumable));
            }

            if (productDefinitions.Count > 0)
            {
                _store.FetchProducts(productDefinitions);
            }
            else
            {
                Debug.LogWarning("[IAPManager] IAPTable에서 상품을 찾을 수 없습니다");
            }
        }

        private void HookEvents(bool subscribe)
        {
            if (subscribe)
            {
                _store.OnStoreDisconnected += OnStoreDisconnected;

                _store.OnProductsFetched += OnProductsFetched;
                _store.OnProductsFetchFailed += OnProductsFetchFailed;

                _store.OnPurchasePending += OnPurchasePending;
                _store.OnPurchaseDeferred += OnPurchaseDeferred;
                _store.OnPurchaseFailed += OnPurchaseFailed;
                _store.OnPurchaseConfirmed += OnPurchaseConfirmed;

                _store.OnPurchasesFetched += OnPurchasesFetched;
                _store.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
            }
            else
            {
                _store.OnStoreDisconnected -= OnStoreDisconnected;

                _store.OnProductsFetched -= OnProductsFetched;
                _store.OnProductsFetchFailed -= OnProductsFetchFailed;

                _store.OnPurchasePending -= OnPurchasePending;
                _store.OnPurchaseDeferred -= OnPurchaseDeferred;
                _store.OnPurchaseFailed -= OnPurchaseFailed;
                _store.OnPurchaseConfirmed -= OnPurchaseConfirmed;

                _store.OnPurchasesFetched -= OnPurchasesFetched;
                _store.OnPurchasesFetchFailed -= OnPurchasesFetchFailed;
            }
        }

        #endregion

        #region UI Logics
        public void BuyTestBlue()
        {
            _store.PurchaseProduct(test_iap);
        }

        public void BuyProduct(string productId)
        {
            if (_store == null)
            {
                Debug.LogWarning("[IAPManager] Store가 초기화되지 않았습니다");
                return;
            }

            if (!_productCache.ContainsKey(productId))
            {
                Debug.LogWarning($"[IAPManager] 상품 '{productId}' 을 찾을 수 없습니다");
                return;
            }

            Debug.Log($"[IAPManager] {productId}를 구매 시도합니다");
            _store.PurchaseProduct(productId);
        }

        #endregion

        #region IAP Callbacks
        // Products
        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"[IAPManager] OnProductsFetched: {products.Count}");
            _productCache.Clear();
            foreach (var p in products)
            {
                _productCache[p.definition.id] = p;
            }
        }

        private void OnProductsFetchFailed(ProductFetchFailed err)
        {
            Debug.LogWarning($"[IAPManager] OnProductsFetchFailed: {err.FailureReason}");
        }

        // Purchases
        private void OnPurchasePending(PendingOrder pending)
        {
            Debug.Log($"[IAPManager] OnPurchasePending: {pending.Info}");

            foreach (var item in pending.CartOrdered.Items())
            {
                Debug.Log($"[IAPManager] {item} 처리 시작");

                if (_iapTable.TryGet(item.Product.definition.id, out int blueAmount))
                {
                    DatabaseManager.Instance.IncrementToLongOnMainWithTransaction(DBRoutes.BlueHoneyGem(FirebaseManager.Instance.Auth.CurrentUser.UserId),
                        blueAmount,
                        suc =>
                        {
                            Debug.Log($"블루허니잼 {blueAmount} 결제 완료. 현재 블루허니잼: {suc}");
                            MissionService.Instance.Report(MissionVerb.Obtain, MissionObject.BluyHoneyGem, null, blueAmount);
                            _store.ConfirmPurchase(pending);
                        },
                        err => Debug.LogWarning($"블루허니잼 트랜잭션 오류: {err}"));
                }
                else
                {
                    Debug.LogWarning($"[IAPManager] {item}를 _iapTable에서 찾을 수 없습니다");
                }
            }
        }

        private void OnPurchaseDeferred(DeferredOrder def)
        {
            Debug.Log($"[IAPManager] OnPurchasePending: {def.Info}");
        }

        private void OnPurchaseFailed(FailedOrder failed)
        {
            Debug.LogWarning($"[IAPManager] OnPurchaseFailedOnPurchaseFailed: {failed.Info} / {failed.Details}");
        }

        private void OnPurchaseConfirmed(Order completed)
        {
            Debug.Log($"[IAPManager] OnPurchaseCompleted: {completed.Info.PurchasedProductInfo.Count}");
        }

        // Restore
        private void OnPurchasesFetched(Orders orders)
        {
            Debug.Log($"[IAPManager] OnPurchasesFetched: DeferredOrders: {orders.DeferredOrders.Count} / ConfirmedOrders: {orders.ConfirmedOrders.Count}");
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription err)
        {
            Debug.LogWarning($"[IAPManagerIAPManager] OnPurchasesFetchFailed: {err.FailureReason}");
        }

        // Disconnect
        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            Debug.LogWarning($"[IAPManager] OnStoreDisconnected: {description.message}");
        }

        #endregion

        #region Debug

        private void DebugLog()
        {

        }

        #endregion
    }
}