using Photon.Pun;
using System;
using System.Collections.Generic;
using UnityEngine;
using YSJ;
using YSJ.Util;

[DisallowMultipleComponent]
public class ItemBox : MonoBehaviour
{
    [Header("ItemSO Config")]
    [SerializeField] private bool _useItemRegistry = false;     // 아이템 레지스터리 사용 여부
    [SerializeField] private List<UnimoItemSO> _items = new();

    [Header("Config")]
    [SerializeField] private bool _selfSetup = true;
    [SerializeField] private LayerMask _collisionLayers;    // 충돌 가능한 레이어들
    [SerializeField] private bool _isDespawnStart = false;  // 없어진 상태로 시작할지 여부
    [SerializeField] private float _respawnCycleTime = 8f;  // 리스폰 주기 시간

    [Header("Visual Config")]
    [SerializeField] private GameObject _boxBody;           // 시각적 오브젝트
    [SerializeField] private AudioClip _collisionAudioClip; // 충돌 시
    [SerializeField] private AudioClip _spawnAudioClip;     // 스폰 시
    [SerializeField] private AudioClip _despawnAudioClip;   // 디스폰 시

    public Action<Collider> OnCollisionAction;              // 충돌 시 콜백
    public Action OnSpawnAction;                            // 스폰 시 콜백
    public Action OnDespawnAction;                          // 디스폰 시 콜백

    // 상태
    private double _firtSpawnTime;      // 첫 생성 시간

    private bool _isDespawn = false;    // 디스폰 상태
    private double _despawnTime = -1;   // 디스폰 시각(서버 기준)
    private double _respawnTime = -1;   // 리스폰 예정 시각(서버 기준)

    private GameObject _lastCollisionPlayerGO;
    private string _lastCollisionPlayerID;


    #region Unity Func
    private void Awake()
    {
        _firtSpawnTime = PhotonNetwork.Time;
        if (_boxBody == null)
            this.PrintLog($"{name}: _boxBody가 비어있어. 비주얼이 보이지 않을 수 있습니다.");

        this.PrintLog($"Item ID: {gameObject.GetInstanceID()} / 첫 스폰 시간: {_firtSpawnTime}");
    }

    private void Start()
    {
        if (_selfSetup)
            Setup();
    }

    private void Update()
    {
        if (_isDespawn && _respawnTime > PhotonNetwork.Time)
        {
            ForceSpawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 디스폰 상태면 무시
        if (_isDespawn) return;

        // 레이어 필터링
        if (!UnityUtilEx.IsInLayerMask(other.gameObject.layer, _collisionLayers))
            return;

        // 마지막 충돌 정보
        bool setInfo = LastCollisionInfo(other);
        if (!setInfo) return;

        // 외부 콜백 먼저
        OnCollisionAction?.Invoke(other);

        // 아이템 지급 로직은 보통 외부 OnCollisionAction에서 처리
        ForceDespawn(0f);
    }
    #endregion

    #region Script Func
    public void Setup()
    {
        LoadItemRegistry();

        if (_isDespawnStart)
            ForceDespawn(0f);
        else
            ForceSpawn();
    }

    public void ForceSpawn()
    {
        _isDespawn = false;
        _respawnTime = PhotonNetwork.Time;

        SetVisualActive(true);
        OnSpawnAction?.Invoke();
    }
    public void ForceDespawn(float extraDelay = 0f)
    {
        _isDespawn = true;
        _despawnTime = PhotonNetwork.Time;
        _respawnTime = _despawnTime + Mathf.Max(0f, _respawnCycleTime + extraDelay);

        SetVisualActive(false);
        OnDespawnAction?.Invoke();
    }

    private void SetVisualActive(bool active)
    {
        if (_boxBody != null)
            _boxBody.SetActive(active);
    }
    private bool LastCollisionInfo(Collider other)
    {
        _lastCollisionPlayerGO = other.gameObject;
        _lastCollisionPlayerID = null;

        // PhotonView에서 유저 ID 가져오기
        var view = _lastCollisionPlayerGO.GetComponentInParent<PhotonView>();
        if (view != null)
        {
            // UserId가 일반적으로 문자열 ID
            _lastCollisionPlayerID = view.Owner != null ? view.Owner.UserId : null;
        }

        return (_lastCollisionPlayerGO != null && view != null);
    }

    private void LoadItemRegistry()
    {
        if (!_useItemRegistry) return;

        this.PrintLog("LoadAllItem 진행");

        UnimoItemSO[] loadItemSOArray = ItemManager.Instance.GetItemSOs();
        if (loadItemSOArray == null) return;


        if (loadItemSOArray.Length > 0)
        {
            foreach (var itemSO in loadItemSOArray)
            {
                if (_items.Contains(itemSO))
                {
                    this.PrintLog($"모든 아이템 자동 로드 시, {itemSO}은 List에 이미 포함 되어 있습니다.", LogType.Warning);
                    continue;
                }

                this.PrintLog($"모든 아이템 자동 로드 시, {itemSO}은 List에 추가합니다.");
                _items.Add(itemSO);
            }
        }

        this.PrintLog("LoadAllItem 진행 완료");
    }
    #endregion
}