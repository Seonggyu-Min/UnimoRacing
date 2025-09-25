using UnityEngine;
using YTW;

public class ItemBoxSoundCmp : MonoBehaviour
{
    [Header("Sound Config")]
    [SerializeField] private AudioClip _collisionAudioClip;         // 충돌 시
    [SerializeField] private AudioClip _spawnAudioClip;             // 스폰 시
    [SerializeField] private AudioClip _despawnAudioClip;           // 디스폰 시

    private ItemBox _itemBoxCmp;
    private AudioManager _audioManager;

    private void Awake()
    {
        _itemBoxCmp = GetComponentInChildren<ItemBox>();
        _audioManager = AudioManager.Instance;

        _itemBoxCmp.OnCollisionAction -= OnCollision;
        _itemBoxCmp.OnCollisionAction += OnCollision;

        _itemBoxCmp.OnSpawnAction -= OnSpawn;
        _itemBoxCmp.OnSpawnAction += OnSpawn;

        _itemBoxCmp.OnDespawnAction -= OnDespawn;
        _itemBoxCmp.OnDespawnAction += OnDespawn;
    }

    private void OnCollision(Collider collider)
    {
        if (_collisionAudioClip != null)
            _audioManager?.PlaySFX(_collisionAudioClip.name, this.transform.position);
    }

    private void OnSpawn()
    {
        if (_spawnAudioClip != null)
            _audioManager?.PlaySFX(_spawnAudioClip.name, this.transform.position);
    }

    private void OnDespawn()
    {
        if (_despawnAudioClip != null)
            _audioManager?.PlaySFX(_despawnAudioClip.name, this.transform.position);
    }
}
