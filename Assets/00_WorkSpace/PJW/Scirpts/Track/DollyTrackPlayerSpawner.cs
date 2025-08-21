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
        [SerializeField] private float heightOffset = 0.5f;
        [SerializeField, Range(0f, 1f)] private float spawnT = 0f;
        [SerializeField] private bool faceAlongTrack = true;

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

            if (!PhotonNetwork.InRoom)
            {
                return;
            }

            if (tracks == null || tracks.Length == 0)
            {
                return;
            }

            int wantedIndex = PhotonNetwork.LocalPlayer.ActorNumber - 1;
            int trackIndex = Mathf.Clamp(wantedIndex, 0, tracks.Length - 1);
            var myTrack = tracks[trackIndex];

            if (myTrack == null)
            {
                return;
            }

            Vector3 pos = myTrack.EvaluatePositionAtUnit(spawnT, CinemachinePathBase.PositionUnits.Normalized) + Vector3.up * heightOffset;
            Quaternion rot = faceAlongTrack
                ? myTrack.EvaluateOrientationAtUnit(spawnT, CinemachinePathBase.PositionUnits.Normalized)
                : Quaternion.identity;

            // 프리팹 존재 검사
            var prefab = Resources.Load<GameObject>(playerPrefabName);
            if (prefab == null) return;
            var go = PhotonNetwork.Instantiate(playerPrefabName, pos, rot);

            hasSpawned = true;
        }
    }
}
