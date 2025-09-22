using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class PlayerIndicator : MonoBehaviour
    {
        [SerializeField] private RawImage _playerImage;
        private int _equippedCharacterId = 20001;
        private int _equippedKartId = 10001;

        private const int FALLBACK_CHARACTER_ID = 20001;
        private const int FALLBACK_KART_ID = 10001;

        private Action _unsubUnimo;
        private Action _unsubKart;

        private string CurrentUid => FirebaseManager.Instance?.Auth?.CurrentUser?.UserId;


        private void Start()
        {
            RenewUI(); // 혹시 아래가 모두 실패하면 아예 UI가 안뜰 수 있으니까 한 번 호출했음
            SubscribeEquippedChange();
        }

        private void OnDestroy()
        {
            ItemPreviewManager.Instance.UnbindCombinePreview(_playerImage);

            _unsubUnimo?.Invoke();
            _unsubUnimo = null;
            _unsubKart?.Invoke();
            _unsubKart = null;
        }

        private void SubscribeEquippedChange()
        {
            // 유니모 구독
            _unsubUnimo = DatabaseManager.Instance.SubscribeValueChanged(DBRoutes.EquippedUnimo(CurrentUid),
                snap =>
                {
                    int.TryParse(snap?.Value.ToString(), out int newId);
                    if (newId != _equippedCharacterId)
                    {
                        _equippedCharacterId = newId;
                        RenewUI();
                    }
                },
                err => Debug.LogWarning($"[PlayerIndicator] EquippedUnimo 구독 오류: {err}")
                );

            // 카트 구독
            _unsubKart = DatabaseManager.Instance.SubscribeValueChanged(DBRoutes.EquippedKart(CurrentUid),
                snap =>
                {
                    int.TryParse(snap?.Value.ToString(), out int newId);
                    if (newId != _equippedKartId)
                    {
                        _equippedKartId = newId;
                        RenewUI();
                    }
                },
                err => Debug.LogWarning($"[PlayerIndicator] EquippedKart 구독 오류: {err}")
            );
        }

        [Button("RenewUI")]
        private void RenewUI()
        {
            ItemPreviewManager.Instance.BindCombinePreview(_equippedCharacterId, _equippedKartId, _playerImage);
        }
    }
}
