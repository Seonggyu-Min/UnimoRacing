using Cinemachine;
using System;
using UnityEngine;

namespace PJW
{
    public class TrackRegistry : MonoBehaviour
    {
        public static TrackRegistry Instance { get; private set; }

        [SerializeField] public CinemachinePathBase[] tracks;
        public bool IsReady { get; private set; }

        public event Action OnTracksReady;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void RegisterTracks(CinemachinePathBase[] found)
        {
            tracks = found;
            IsReady = (tracks != null && tracks.Length > 0);
            if (IsReady) OnTracksReady?.Invoke();
        }

        public void ClearTracks()
        {
            tracks = Array.Empty<CinemachinePathBase>();
            IsReady = false;
        }

        public CinemachinePathBase GetPathForLocalPlayer(int actorNumber)
        {
            int index = actorNumber % tracks.Length;

            return tracks[index];
        }

    }
}
