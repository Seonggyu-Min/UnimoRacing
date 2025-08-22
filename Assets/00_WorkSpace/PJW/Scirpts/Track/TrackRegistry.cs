using Cinemachine;
using UnityEngine;

namespace PJW
{
    public class TrackRegistry : MonoBehaviour
    {
        public static TrackRegistry Instance { get; private set; }

        [Header("��ü Ʈ�� ����Ʈ")]
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
        /// ���� �÷��̾��� ActorNumber�� �ش��ϴ� Ʈ�� ��ȯ
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
