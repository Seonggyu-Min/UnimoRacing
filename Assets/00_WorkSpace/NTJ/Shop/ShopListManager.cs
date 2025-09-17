using Firebase.Database;
using MSG;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopListManager : PopupBase
{
    #region Fields and Properties
    [Header("Register SO")]
    [SerializeField] private UnimoCharacterSO[] _unimoSOs;
    [SerializeField] private UnimoKartSO[] _kartSOs;

    [Header("Button Parent")]
    [SerializeField] private Transform _unimoParent;
    [SerializeField] private Transform _kartParent;

    [Header("Button Prefabs")]
    [SerializeField] private BuyButtonBehaviour _unimoButtonPrefab; // ���ϸ�� ��ư ������
    [SerializeField] private BuyButtonBehaviour _kartButtonPrefab; // īƮ�� ��ư ������

    [Header("Info Text")]
    [SerializeField] private TMP_Text _infoText;

    private readonly Dictionary<int, BuyButtonBehaviour> _unimoDict = new();
    private readonly Dictionary<int, BuyButtonBehaviour> _kartDict = new();
    private bool _isGenerated = false;

    private Action _unsubUnimoInv;
    private Action _unsubKartInv;

    private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;
    #endregion

    #region Unity Methods
    private void OnEnable()
    {
        // OnEnable()�� ������ �����մϴ�.
        // ��ư ������ �����Ͱ� �����ϸ� ������ �ݹ鿡�� ó���մϴ�.
        SubscribeInventory();
    }

    private void OnDisable()
    {
        UnsubscribeInventory();
    }

    #endregion

    #region Private Methods

    private void GenerateButtons()
    {
        // ���ϸ� ��ư ����
        for (int i = 0; i < _unimoSOs.Length; i++)
        {
            UnimoCharacterSO so = _unimoSOs[i];
            if (so == null) continue;

            // Unimo ������ ���
            BuyButtonBehaviour button = Instantiate(_unimoButtonPrefab, _unimoParent);
            button.name = $"UnimoButton_{so.characterName}";          
            button.SetupButton(so.characterName, so.characterSprite, string.Empty, so.currencyType);
            button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Unimo, so.characterId);
            _unimoDict.Add(so.characterId, button);
            button.RefreshItemState(0);
        }

        // īƮ ��ư ����
        for (int i = 0; i < _kartSOs.Length; i++)
        {
            UnimoKartSO so = _kartSOs[i];
            if (so == null) continue;

            BuyButtonBehaviour button = Instantiate(_kartButtonPrefab, _kartParent);
            button.name = $"KartButton_{so.carName}";
            button.SetupButton(so.carName, so.kartSprite, string.Empty, so.currencyType);
            button.SetupTypeAndId(BuyButtonBehaviour.ItemType.Kart, so.KartID);
            _kartDict.Add(so.KartID, button);
            button.RefreshItemState(0);
        }
    }

    private void SubscribeInventory()
    {
        _unsubUnimoInv = DatabaseManager.Instance.SubscribeValueChanged(
            DBRoutes.UnimosInventory(CurrentUid),
            onChanged: OnUnimoInventorySnapshot,
            onError: (err) => Debug.LogWarning($"[ShopManager] ���ϸ� �κ��丮 ���� ����: {err}")
        );

        _unsubKartInv = DatabaseManager.Instance.SubscribeValueChanged(
            DBRoutes.KartsInventory(CurrentUid),
            onChanged: OnKartInventorySnapshot,
            onError: (err) => Debug.LogWarning($"[ShopManager] īƮ �κ��丮 ���� ����: {err}")
        );
    }

    private void UnsubscribeInventory()
    {
        _unsubUnimoInv?.Invoke();
        _unsubKartInv?.Invoke();
        _unsubUnimoInv = null;
        _unsubKartInv = null;
    }

    private void OnUnimoInventorySnapshot(DataSnapshot snap)
    {
        // ��ư�� ���� �������� �ʾҴٸ�, ���� �����մϴ�.
        // �̷��� �ϸ� �κ��丮 �����͸� ������ �ִ� ���¿��� UI�� ���� �� �ֽ��ϴ�.
        if (!_isGenerated)
        {
            GenerateButtons();
            _isGenerated = true;
        }

        var inventoryData = snap.Value as Dictionary<string, object> ?? new Dictionary<string, object>();

        foreach (var kv in _unimoDict)
        {
            int unimoId = kv.Key;
            BuyButtonBehaviour button = kv.Value;
            if (inventoryData.ContainsKey(unimoId.ToString()))
            {
                button.RefreshItemState(1); // 1: ���� ��
            }
            else
            {
                button.RefreshItemState(0); // 0: �������� ����
            }
        }
    }

    private void OnKartInventorySnapshot(DataSnapshot snap)
    {
        // ��ư�� ���� �������� �ʾҴٸ�, ���� �����մϴ�.
        if (!_isGenerated)
        {
            GenerateButtons();
            _isGenerated = true;
        }

        var inventoryData = snap.Value as Dictionary<string, object> ?? new Dictionary<string, object>();
        foreach (var kv in _kartDict)
        {
            int kartId = kv.Key;
            BuyButtonBehaviour button = kv.Value;

            if (inventoryData.ContainsKey(kartId.ToString()))
            {
                // īƮ�� ��� Firebase���� ���� ������ �����ɴϴ�.
                int currentLevel = 0;
                if (int.TryParse(inventoryData[kartId.ToString()].ToString(), out int level))
                {
                    currentLevel = level;
                }
                button.RefreshItemState(currentLevel);
            }
            else
            {
                button.RefreshItemState(0);
            }
        }
    }
    #endregion
}