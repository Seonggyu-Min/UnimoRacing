using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class IAPButtonBehaviour : MonoBehaviour
    {
        [Header("UI Appearance")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _productNameText;
        [SerializeField] private TMP_Text _productPriceText;
        [SerializeField] private TMP_Text _productRewardAmountText;
        //[SerializeField] private TMP_Text _productDescriptionText;      // 이건 필요 없을 듯?

        private int _iapIndex;
        private IAPTable _iapTable;
        private Sprite _sprite;


        public void Init(int iapIndex, IAPTable iapTable, Sprite sprite)
        {
            _iapIndex = iapIndex;
            _iapTable = iapTable;
            _sprite = sprite;

            RenewUI();
        }

        private void RenewUI()
        {
            var products = IAPManager.Instance.ProductCache;
            var myProductId = _iapTable.Entries[_iapIndex].ProductId;

            if (products.ContainsKey(myProductId))
            {
                _productNameText.text = products[myProductId].metadata.localizedTitle;
                _productPriceText.text = products[myProductId].metadata.localizedPriceString;
                var textGroup = products[myProductId].definition.id.Split('_');
                _productRewardAmountText.text = textGroup[textGroup.Length - 1];
                _iconImage.sprite = _sprite;
            }
            else
            {
                Debug.LogWarning($"[IAPButtonBehaviour] products에 key {myProductId}가 존재하지 않습니다");
            }
        }


        public void OnClickBuy()
        {
            IAPManager.Instance.BuyProduct(_iapTable.Entries[_iapIndex].ProductId);

            // 연타 방지 해야되나?
        }
    }
}
