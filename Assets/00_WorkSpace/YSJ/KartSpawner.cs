using Cinemachine;
using Firebase.Database;
using MSG;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using YSJ.Util;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class KartSpawner : MonoBehaviour
{
    [Header("Prefab / Tracks")]
    [SerializeField] private string _kartPrefabName = "Player"; // Resources 폴더 기준
    [SerializeField] private CinemachinePathBase[] tracks;

    [Header("Placement")]
    [SerializeField] private CinemachinePathBase.PositionUnits units = CinemachinePathBase.PositionUnits.Normalized;
    [SerializeField] private float spawnUnitPos = 0f;                        // 트랙의 어느 지점에서 스폰할지
    // [SerializeField] private bool faceAlongTrack = true;                     // 트랙 진행 방향을 바라보게 스폰할지 여부

    private bool _spawned;

    private int characterID = -1;
    private int kartID = -1;

    private bool _isSetCharacterID = false;
    private bool _isSetkartID = false;

    private bool _isSuccessed => _isSetCharacterID && _isSetkartID;

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
        if (tracks == null || tracks.Length == 0)
        {
            this.PrintLog("Spawn points not configured");
            return;
        }

        // 스폰 포인트 확정
        var t = tracks[startIndex % tracks.Length];
        // 스폰 필요 데이터
        Vector3 pos = t.EvaluatePositionAtUnit(spawnUnitPos, CinemachinePathBase.PositionUnits.Normalized) + Vector3.up; // 스폰을 0에서 시작하게 해주는 기능
        Quaternion rot = Quaternion.identity;

        // 서버에서 현 로컬 플레이어의 IDs 가지고 오기
        DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedKart(me.UserId), GetSuccessCharacterID, GetErrorCharacterID);
        DatabaseManager.Instance.GetOnMain(DBRoutes.EquippedUnimo(me.UserId), GetSuccessKartID, GetErrorKartID);

        var endLapCount = ReInGameManager.Instance.RaceEndLapCount;

        // 인스턴스 데이터(늦게 들어온 플레이어도 동일 데이터로 재생성됨)
        object[] instData = { me.ActorNumber, startIndex, characterID, kartID, me.UserId, endLapCount };

        // 서버 데이터가지고 왔을 때까지, 딜레이하다가 소환
        this.PrintLog($"Spawner User ID: {me.UserId}");
        StartCoroutine(DelayKartSpawn(_kartPrefabName, pos, rot, 0, instData));
    }

    

    private IEnumerator DelayKartSpawn(string kartID, Vector3 spawnPosition, Quaternion spawnRotation, byte group, object[] data)
    {
        while (!_isSuccessed)
        {
            yield return null;
        }

        // 네트워크 스폰(룸 캐시에 기록, 나중 사용)
        PhotonNetwork.Instantiate(kartID, spawnPosition, spawnRotation, group, data);
        _spawned = true;

        yield break;
    }

    // CharacterId / CarId 같은 게 늦게 세팅돼도 반응(테스트용으로도 사용 가능)
    /*public override void OnPlayerPropertiesUpdate(Player target, Hashtable changedProps)
    {
        if (target.IsLocal)
        {
            if (changedProps.ContainsKey(PK(PlayerKey.CharacterId)) ||
                changedProps.ContainsKey(PK(PlayerKey.CarId)))
            {
                TrySpawnLocal();
            }
        }
    }*/

    // onSuccess
    private void GetSuccessCharacterID(DataSnapshot snapShot)
    {
        characterID = PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_CHARACTER_ID;
        _isSetCharacterID = true;
        if (!int.TryParse(snapShot.Value.ToString(), out characterID))
            return;
    }
    private void GetSuccessKartID(DataSnapshot snapShot)
    {
        kartID = PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_KART_ID;
        _isSetkartID = true;
        if (!int.TryParse(snapShot.Value.ToString(), out kartID))
            return;
    }

    // onError(연결이 끊키거나, 데이터를 가지고 오기를 실패헀을 때, 엔진 제차 에러거나 등)
    private void GetErrorCharacterID(string snapShot)
    {
        characterID = PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_CHARACTER_ID;
        _isSetCharacterID = true;
    }
    private void GetErrorKartID(string snapShot)
    {
        kartID = PhotonNetworkCustomProperties.VALUE_PLAYER_DEFAULT_KART_ID;
        _isSetkartID = true;
    }
}
