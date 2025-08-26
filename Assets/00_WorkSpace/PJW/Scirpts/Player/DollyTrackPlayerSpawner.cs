using System;
using Photon.Pun;
using UnityEngine;
using Cinemachine;

namespace PJW
{
    public class DollyTrackPlayerSpawner : MonoBehaviourPunCallbacks
    {
        [Header("Tracks / Prefab")]
        [SerializeField] private CinemachinePathBase[] tracks;
        [SerializeField] private string playerPrefabName = "Player";

        [Header("Placement")]
        private float spawnT = 0f; // 트랙의 어느 지점에서 스폰할지
        private bool faceAlongTrack = true; // 트랙 진행 방향을 바라보게 스폰할지 여부

        private bool hasSpawned;

        private void Start()
        {
            if (PhotonNetwork.InRoom && !hasSpawned)
            {
                TrySpawn();
            }
        }

        private void TrySpawn()
        {
            if (hasSpawned) return;

            int wantedIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
            int trackIndex = Mathf.Clamp(wantedIndex, 0, tracks.Length - 1);
            var myTrack = tracks[trackIndex];

            if (myTrack == null)
            {
                return;
            }

            Vector3 pos = myTrack.EvaluatePositionAtUnit(spawnT, CinemachinePathBase.PositionUnits.Normalized) + Vector3.up; // 스폰을 0에서 시작하게 해주는 기능
            Quaternion rot = Quaternion.identity;

            // 프리팹 존재 검사
            var prefab = Resources.Load<GameObject>(playerPrefabName);
            if (prefab == null) return;
            var go = PhotonNetwork.Instantiate(playerPrefabName, pos, rot);

            hasSpawned = true;
        }
    }
}
