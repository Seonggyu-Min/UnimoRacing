using ExitGames.Client.Photon;
using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;
using YSJ;
using YSJ.Net;
using YSJ.Util;

[DisallowMultipleComponent]
[RequireComponent(typeof(PhotonView))]
public class ItemBox : MonoBehaviourPunCallbacks
{
    [Header("ItemSO Config")]
    [SerializeField] private bool _useItemManager = false;
    [SerializeField] private List<ItemSpawnProbabilityData> _items = new();

    [Header("Config")]
    [SerializeField] private string _collisionTag = "Player";
    [SerializeField] private bool _isDespawnStart = false;
    [SerializeField] private float _respawnCycleTime = 8f;

    [Header("Inventory Save Config")]
    [SerializeField] private bool isDelaySave = false;
    [SerializeField] private bool isForceSave = false;
    [SerializeField] private bool isSaveItemAction = true;

    [Header("Visual Config")]
    [SerializeField] private bool _isSpawnVisualBoxBody = true;
    [SerializeField] private GameObject _spawnableVisualBoxBodyGO;
    [SerializeField] private GameObject _boxBodyPoint;

    public Action OnSetupAction;
    public Action OnSpawnAction;
    public Action OnDespawnAction;

    private bool _isSetup = false;
    
    private PhotonView _pv;
    private GameObject _boxBody;

    private bool _isDespawn;
    private double _despawnTime = -1;
    private double _respawnTime = -1;

    public bool IsSetup => _isSetup;
    public int ViewID => _pv?.ViewID ?? 0;

    private void Awake()
    {
        _pv = GetComponent<PhotonView>();

        if (_isSpawnVisualBoxBody)
        {
            if (_boxBodyPoint == null)
            {
                this.PrintLog($"{name}: _boxBodyPoint 없음 >>> 자동생성", LogType.Warning);
                _boxBodyPoint = new GameObject("BodyPoint");
                _boxBodyPoint.transform.SetParent(transform, false);
            }
            if (_spawnableVisualBoxBodyGO != null)
            {
                _boxBody = Instantiate(_spawnableVisualBoxBodyGO, _boxBodyPoint.transform);
                _boxBody.transform.localPosition = Vector3.zero;
                _boxBody.transform.localRotation = Quaternion.identity;
            }
        }

        if (_useItemManager)
        {
            var IM = ItemManager.GetInstance;
            if(IM == null)
            {
                this.PrintLog($"ItemManager를 사용하려 하였지만, ItemManager가 존재하지 않습니다. _useItemManager는 FALSE를 값으로 변경되었습니다.");
                _useItemManager = false;
            }
        }
    }

    private void Start()
    {
        if (!_useItemManager)
        {
            this.PrintLog($"_useItemManager이 FALSE여서 자체 셋업 됩니다.");
            Setup();
        }
    }

    private void Update()
    {
        if (_isDespawn && PhotonNetwork.Time >= _respawnTime)
        {
            if (PhotonNetwork.IsMasterClient)
                BroadcastSpawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isDespawn) return;
        if (other == null) return;
        if (!other.CompareTag(_collisionTag)) return;

        var playerView = other.GetComponentInParent<PhotonView>();
        if (playerView == null || !playerView.IsMine) return;

        _pv.RPC(nameof(RPC_RequestPickup), RpcTarget.MasterClient, playerView.OwnerActorNr, playerView.ViewID);
    }

    public void Setup()
    {
        OnSetupAction?.Invoke();

        if (_isDespawnStart)
        {
            var epoch = PhotonNetwork.Time + _respawnCycleTime;
            ForceDespawnLocal(epoch);
        }
        else
        {
            ForceSpawnLocal();
        }

        _isSetup = true;
    }

    private void SetVisualActive(bool active)
    {
        if (_boxBody != null) _boxBody.SetActive(active);
    }

    private void ForceSpawnLocal()
    {
        _isDespawn = false;
        _respawnTime = PhotonNetwork.Time;
        SetVisualActive(true);
        OnSpawnAction?.Invoke();
    }

    private void ForceDespawnLocal(double respawnEpoch)
    {
        _isDespawn = true;
        _despawnTime = PhotonNetwork.Time;
        _respawnTime = respawnEpoch;
        SetVisualActive(false);
        OnDespawnAction?.Invoke();
    }

    public void OnDespawnVisual(double epoch) => ForceDespawnLocal(epoch);
    public void OnSpawnVisual() => ForceSpawnLocal();

    // Master 처리 플로우
    [PunRPC]
    private void RPC_RequestPickup(int actorNr, int playerRootViewId, PhotonMessageInfo _)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (_isDespawn) return;

        // 마스터가 아이템 확정
        UnimoItemSO itemSO =
            _useItemManager ? ItemManager.Instance.GetRandomOneItemSO()
                            : ItemManager.Instance.GetRandomOneItemSO(_items);

        ItemId grantId = itemSO != null ? itemSO.itemID : ItemId.None;

        // 디스폰 사이클 확정
        double respawnEpoch = PhotonNetwork.Time + _respawnCycleTime;

        // 룸 스냅샷 저장
        // 모두에게 디스폰 알림
        ItemUtil.SetSnapshot(ViewID, respawnEpoch);
        _pv.RPC(nameof(RPC_OnDespawnAll), RpcTarget.All, respawnEpoch);

        // 지급 대상 플레이어의 PhotonView로 RPC 호출
        var target = PhotonNetwork.CurrentRoom?.GetPlayer(actorNr);
        var targetPV = PhotonView.Find(playerRootViewId);

        if (target != null && targetPV != null)
        {
            // 플레이어 오브젝트에 붙은 InventoryRPCProxy가 수신
            targetPV.RPC(nameof(InventoryRPCProxy.RPC_GrantItemById), target, (int)grantId, (bool)isDelaySave, (bool)isForceSave, (bool)isSaveItemAction);
        }
    }

    [PunRPC]
    private void RPC_OnDespawnAll(double respawnEpoch) => ForceDespawnLocal(respawnEpoch);

    [PunRPC]
    private void RPC_OnSpawnAll() => ForceSpawnLocal();

    private void BroadcastSpawn()
    {
        ItemUtil.SetSnapshot(ViewID, 0d);
        _pv.RPC(nameof(RPC_OnSpawnAll), RpcTarget.All);
    }

    // 복원 > 커스텀프롬
    public override void OnRoomPropertiesUpdate(Hashtable changed)
    {
        var key = ItemUtil.Key(ViewID);
        if (!changed.ContainsKey(key)) return;

        double epoch = (double)changed[key];
        ApplySnapshot(epoch);
    }

    public void ApplySnapshot(double epoch)
    {
        if (epoch <= 0d)
        {
            _isDespawn = false;
            _respawnTime = PhotonNetwork.Time;
            SetVisualActive(true);
        }
        else
        {
            _isDespawn = true;
            _respawnTime = epoch;
            SetVisualActive(false);
        }
    }
}
