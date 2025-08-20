using System;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Cinemachine;

namespace PJW
{
    public class MultiTrackPlayerSpawner : MonoBehaviourPunCallbacks
    {
        [Header("Tracks / Prefab")]
        [SerializeField] private CinemachinePathBase[] tracks;
        [SerializeField] private string playerPrefabName = "Player";

        [Header("Placement")]
        [SerializeField] private float heightOffset = 0.5f; 
        [SerializeField] private float spawnT = 0f;         
        [SerializeField] private bool faceAlongTrack = true;

        private bool hasSpawned;

        public override void OnJoinedRoom()
        {
            TrySpawn();
        }

        private void TrySpawn()
        {
            if (hasSpawned) return;

            // 플레이어 순서: ActorNumber 기준으로 정렬
            Player[] sorted = PhotonNetwork.PlayerList.OrderBy(p => p.ActorNumber).ToArray();
            int myIndex = Array.FindIndex(sorted, p => p == PhotonNetwork.LocalPlayer);

            if (myIndex < 0 || myIndex >= tracks.Length)
            {
                return;
            }

            // 내 차례에 해당하는 트랙 선택
            CinemachinePathBase myTrack = tracks[myIndex];
            if (myTrack == null)
            {
                return;
            }

            Vector3 pos = myTrack.EvaluatePositionAtUnit(spawnT, CinemachinePathBase.PositionUnits.Normalized);
            pos += Vector3.up * heightOffset;

            Quaternion rot = Quaternion.identity;
            if (faceAlongTrack)
            {
                rot = myTrack.EvaluateOrientationAtUnit(spawnT, CinemachinePathBase.PositionUnits.Normalized);
            }

            PhotonNetwork.Instantiate(playerPrefabName, pos, rot);

            hasSpawned = true;
        }
    }
}
