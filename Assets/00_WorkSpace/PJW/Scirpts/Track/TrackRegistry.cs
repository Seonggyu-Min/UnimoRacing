using Cinemachine;
using UnityEngine;

namespace PJW
{
    public class TrackRegistry : MonoBehaviour
    {
        public static TrackRegistry Instance { get; private set; }

        [Header("전체 트랙 리스트")]
        public CinemachinePathBase[] tracks;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        /// <summary>
        /// 로컬 플레이어의 ActorNumber에 해당하는 트랙 반환
        /// </summary>
        public CinemachinePathBase GetPathForLocalPlayer(int actorNumber)
        {
            if (tracks == null || tracks.Length == 0)
                return null;

            int index = actorNumber - 1;
            if (index < 0 || index >= tracks.Length)
                return null;

            return tracks[index];
        }
    }
}
