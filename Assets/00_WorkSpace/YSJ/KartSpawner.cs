using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using YSJ.Util;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class KartSpawner : MonoBehaviourPunCallbacks
{
    [SerializeField] private string _kartPrefabName = "Player"; // Resources 폴더 기준
    [SerializeField] private Transform[] _spawnPoints;

    private bool _spawned;

    private static string PK(PlayerKey k) => PhotonNetworkCustomProperties.ToKeyString(k);

    private void Start()
    {
        TrySpawnLocal();
    }

    private void TrySpawnLocal()
    {
        if (_spawned) return;
        if (!PhotonNetwork.InRoom) return;

        var me = PhotonNetwork.LocalPlayer;

        // 스폰 인덱스 계산(결정론)
        int startIndex = PhotonNetworkManager.Instance.GetStartIndexFor(me);
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            this.PrintLog("Spawn points not configured");
            return;
        }

        // 스폰 포인트 확정
        var t = _spawnPoints[startIndex % _spawnPoints.Length];

        // 캐릭터/카 선택값 가져오기(없으면 기본값)
        int charId = me.CustomProperties.TryGetValue(PK(PlayerKey.CharacterId), out var c) ? (int)c : 0;
        int carId  = me.CustomProperties.TryGetValue(PK(PlayerKey.CarId), out var r) ? (int)r : 0;

        // 인스턴스 데이터(늦게 들어온 플레이어도 동일 데이터로 재생성됨)
        object[] instData = { me.ActorNumber, startIndex, charId, carId };

        // 네트워크 스폰(룸 캐시에 기록, 나중에 쓸거임)
        PhotonNetwork.Instantiate(_kartPrefabName, t.position, t.rotation, 0, instData);

        _spawned = true;
    }

    // CharacterId / CarId 같은 게 늦게 세팅돼도 반응(테스트용으로도 사용 가능)
    public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
    {
        if (target.IsLocal)
        {
            if (changedProps.ContainsKey(PK(PlayerKey.CharacterId)) ||
                changedProps.ContainsKey(PK(PlayerKey.CarId)))
            {
                TrySpawnLocal();
            }
        }
    }
}
