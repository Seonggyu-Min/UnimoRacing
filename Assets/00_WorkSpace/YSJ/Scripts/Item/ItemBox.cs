using DA_Assets.FCU;
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
    [SerializeField] private bool _useItemManager = false;
    [SerializeField] private List<ItemSpawnProbabilityData> _items = new();  // 등장 가능한 아이템 리스트

    [Header("Config")]
    // [SerializeField] private bool _selfSetup = true;
    [SerializeField] private bool _selfSetup = true;
    // [SerializeField] private LayerMask _collisionLayers;     // 충돌 가능한 레이어들
    [SerializeField] private string _collisionTag = "Player";   // 충돌 태그
    [SerializeField] private bool _isDespawnStart = false;      // 없어진 상태로 시작할지 여부
    [SerializeField] private float _respawnCycleTime = 8f;      // 리스폰 주기 시간

    [Header("Visual Config")]
    [SerializeField] private bool _isSpawnVisualBoxBody;            // 시작적 오브젝트 생성 여부
    [SerializeField] private GameObject _spawnableVisualBoxBodyGO;    // 시각적 오브젝트
    [SerializeField] private GameObject _boxBodySpawnPoint;         // 실적용 바디를 스폰할 포인트

    [Header("Sound Config")]
    [SerializeField] private AudioClip _collisionAudioClip;         // 충돌 시
    [SerializeField] private AudioClip _spawnAudioClip;             // 스폰 시
    [SerializeField] private AudioClip _despawnAudioClip;           // 디스폰 시

    public Action<Collider> OnCollisionAction;              // 충돌 시 콜백
    public Action OnSpawnAction;                            // 스폰 시 콜백
    public Action OnDespawnAction;                          // 디스폰 시 콜백

    // 상태
    private GameObject _boxBody;

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
        if (_isSpawnVisualBoxBody)
        {
            if (_boxBodySpawnPoint == null)
            {
                this.PrintLog($"{name}: _boxBodySpawnPoint가 비어있어. _boxBodySpawnPoint를 자동 생성합니다.");
                _boxBodySpawnPoint = GameObject.Instantiate(new GameObject("BodyPoint"), this.gameObject.transform);
            }

            if(_spawnableVisualBoxBodyGO)
            {
                _boxBody = GameObject.Instantiate(_spawnableVisualBoxBodyGO, _boxBodySpawnPoint.transform);
                _boxBody.transform.localPosition = Vector3.zero;
                _boxBody.transform.localRotation = Quaternion.identity;

                this.PrintLog($"{name}: _spawnableVisualBoxBodyGO > PhotonNetwork.Instantiate 생성합니다.");
            }

            if (_boxBody == null)
                this.PrintLog($"{name}: _boxBody가 비어있어. 비주얼이 보이지 않을 수 있습니다.");
        }

        this.PrintLog($"Local Item ID: {gameObject.GetInstanceID()} / 첫 스폰 시간: {_firtSpawnTime}");
    }

    private void Start()
    {
        if (_selfSetup)
            Setup();
    }

    private void Update()
    {
        if (_isDespawn && _respawnTime <= PhotonNetwork.Time)
        {
            ForceSpawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 디스폰 상태면 무시
        if (_isDespawn)
        {
            this.PrintLog("디스폰 되어 있습니다.");
            return;
        }

        // 충돌체가 없는 상황 시
        if (other == null)
        {
            this.PrintLog($"충돌된 오브젝트가 없습니다. 해당 오브젝트 충돌 파트에 대한 확인 필요");
            return;
        }

        // 충돌 체크 태그 확인
        if (!other.CompareTag(_collisionTag))
            return;

        // 마지막 플레리어의 충돌 정보
        bool setInfo = LastCollisionInfo(other);
        if (!setInfo) return;

        // 플레이어 아이템을 저장 시도
        ApplyPlayerSaveItem(other);

        // 외부 콜백 먼저
        OnCollisionAction?.Invoke(other);

        // 아이템 지급 로직은 보통 외부 OnCollisionAction에서 처리
        ForceDespawn(0f);
        this.PrintLog(
            $"\n[충돌된 오브젝트: {other.name}]\n" +
            $"디스폰 시간: {_despawnTime}\n" +
            $"리스폰 시간: {_respawnTime}\n");
    }
    #endregion

    #region Script Func
    public void Setup()
    {
        if (_useItemManager)
        {
            this.PrintLog($"해당 아이템 박스에 등장할 수 있는 아이템 리스트를 아이템 매니저쪽으로 이관 합니다.");
            ItemManager.Instance.RegisterItemDatas(_items.ToArray());
        }

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
    private bool ApplyPlayerSaveItem(Collider other)
    {
        // 플레이어 GO 체크
        GameObject collGO = other.gameObject;
        if (collGO == null)
            return false;

        // 네트워크 송신 가능 옵젝 체크
        PhotonView view = collGO.GetComponentInChildren<PhotonView>();
        if (view == null)
            return false;

        // 플레리어 인벤 체크
        ItemInventory inventory = collGO.GetComponentInParent<ItemInventory>();
        if (inventory == null)
            return false;

        this.PrintLog($"인벤토리에 해당 오브젝트의 컴포넌트가 아이템 저장을 시도합니다. > 대상: {collGO.name}");

        List<UnimoItemSO> showableItems = new();
        UnimoItemSO itemSO = (_useItemManager) ?
            ItemManager.Instance.GetRandomItemSO() :
            ItemManager.Instance.GetRandomItemSO(_items);

        inventory.SaveItem(itemSO);
        return true;
    }
    #endregion
}