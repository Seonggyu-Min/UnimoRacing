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
        private float spawnT = 0f; // Ʈ���� ��� �������� ��������
        private bool faceAlongTrack = true; // Ʈ�� ���� ������ �ٶ󺸰� �������� ����

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

            Vector3 pos = myTrack.EvaluatePositionAtUnit(spawnT, CinemachinePathBase.PositionUnits.Normalized) + Vector3.up; // ������ 0���� �����ϰ� ���ִ� ���
            Quaternion rot = Quaternion.identity;

            // ������ ���� �˻�
            var prefab = Resources.Load<GameObject>(playerPrefabName);
            if (prefab == null) return;
            var go = PhotonNetwork.Instantiate(playerPrefabName, pos, rot);

            hasSpawned = true;
        }
    }
}
