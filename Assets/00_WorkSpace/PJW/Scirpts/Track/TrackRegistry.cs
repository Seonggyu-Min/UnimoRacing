using Cinemachine;
using UnityEngine;

namespace PJW
{
    public class TrackRegistry : MonoBehaviour
    {
        public static TrackRegistry Instance { get; private set; }

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

        public CinemachinePathBase GetPathForLocalPlayer(int actorNumber)
        {
            int index = actorNumber % tracks.Length;

            return tracks[index];
        }
    }
}
