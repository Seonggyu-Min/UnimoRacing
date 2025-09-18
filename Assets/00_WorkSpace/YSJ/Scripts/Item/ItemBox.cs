using System;
using UnityEngine;
#if PHOTON_UNITY_NETWORKING
using Photon.Pun;
using Photon.Realtime;
#endif

[DisallowMultipleComponent]
public class ItemBox : MonoBehaviour
{
    [Header("Config")]
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
    private bool _isDespawn;                    // 디스폰 상태
    private double _despawnTime;                // 디스폰 시각(서버 기준)
    private double _respawnTime;                // 리스폰 예정 시각(서버 기준)

    private GameObject _lastCollisionPlayerGO;
    private string _lastCollisionPlayerID;

    private AudioSource _audioSource;

    public void Setup()
    {
        _audioSource = GetComponent<AudioSource>();

        // 시작 상태 반영
        if (_isDespawnStart)
            ForceDespawn(0f);
        else
            ForceSpawn();
    }

    public void ForceSpawn()
    {
        _isDespawn = false;
        _despawnTime = -1;
        _respawnTime = -1;

        SetVisualActive(true);
        PlayOneShot(_spawnAudioClip);
        OnSpawnAction?.Invoke();
    }

    public void ForceDespawn(float extraDelay = 0f)
    {
        _isDespawn = true;
        _despawnTime = 
        _respawnTime = _despawnTime + Mathf.Max(0f, _respawnCycleTime + extraDelay);

        SetVisualActive(false);
        PlayOneShot(_despawnAudioClip);
        OnDespawnAction?.Invoke();
    }

    private void Awake()
    {
        // 가능하면 에디터에서도 미리 보여주기 위해 최소한의 세팅
        if (_boxBody == null)
            Debug.LogWarning($"{name}: _boxBody가 비어있어. 비주얼 토글이 안 될 수 있어.");
    }

    private void Start()
    {
        if (_audioSource == null)
            _audioSource = GetComponent<AudioSource>();

        // Start 시점에서 상태 초기화 (중복보호)
        if (_isDespawnStart) ForceDespawn(0f);
        else ForceSpawn();
    }

    private void Update()
    {
        // 디스폰 상태에서 리스폰 타이밍 체크
        if (_isDespawn && _respawnTime > 0)
        {
            ForceSpawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 이미 디스폰 상태면 무시
        if (_isDespawn) return;

        // 레이어 필터링
        if (!IsInLayerMask(other.gameObject.layer, _collisionLayers))
            return;

        // (필요하면 여기서 쿨다운/태그 체크 등 추가)
        CaptureLastCollisionInfo(other);

        // 외부 콜백 먼저
        OnCollisionAction?.Invoke(other);

        // 충돌 사운드
        PlayOneShot(_collisionAudioClip);

        // 아이템 지급 로직은 보통 외부 OnCollisionAction에서 처리
        ForceDespawn(0f);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return ((1 << layer) & mask.value) != 0;
    }

    private void SetVisualActive(bool active)
    {
        if (_boxBody != null)
            _boxBody.SetActive(active);
        else
            gameObject.SetActive(active); // 백업: 본체 토글
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null) return;

        if (_audioSource != null && _audioSource.enabled)
        {
            _audioSource.PlayOneShot(clip);
        }
        else
        {
            // 3D 환경에서 간단히 재생
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }

    private void CaptureLastCollisionInfo(Collider other)
    {
        _lastCollisionPlayerGO = other.attachedRigidbody != null
            ? other.attachedRigidbody.gameObject
            : other.gameObject;

        _lastCollisionPlayerID = null;

        // PhotonView에서 유저 ID 가져오기
        var view = _lastCollisionPlayerGO.GetComponentInParent<PhotonView>();
        if (view != null)
        {
            // UserId가 일반적으로 문자열 ID
            _lastCollisionPlayerID = view.Owner != null ? view.Owner.UserId : null;
        }
    }
}
