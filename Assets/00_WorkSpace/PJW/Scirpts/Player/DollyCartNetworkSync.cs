using Photon.Pun;
using UnityEngine;
using Cinemachine;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhotonView))]
    public class DollyCartSync : MonoBehaviourPun, IPunObservable
    {
        [Header("Refs")]
        [SerializeField] private CinemachineDollyCart cart;

        [Header("Sync")]
        [SerializeField, Range(0f, 1f)] private float lerp = 12;

        private float netPosition;   
        private float netSpeed;      
        private int netTrackIndex;   

        private CinemachinePathBase[] tracks;

        private void Awake()
        {
            tracks = FindObjectsOfType<CinemachinePathBase>(true);
        }

        private void Start()
        {
            if (cart != null && cart.m_Path != null)
            {
                netPosition = cart.m_Position;
                netSpeed = cart.m_Speed;
                netTrackIndex = FindTrackIndex(cart.m_Path);
            }
        }

        private void Update()
        {
            if (cart == null) return;

            if (photonView.IsMine)
            {
                return;
            }

            if (tracks != null && netTrackIndex >= 0 && netTrackIndex < tracks.Length)
            {
                if (cart.m_Path != tracks[netTrackIndex])
                    cart.m_Path = tracks[netTrackIndex];
            }

            // 속도 보간
            cart.m_Speed = Mathf.Lerp(cart.m_Speed, netSpeed, Time.deltaTime * lerp);

            // 포지션 보간
            cart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;
            cart.m_Position = Mathf.Lerp(cart.m_Position, netPosition, Time.deltaTime * lerp);
        }

        // 네트워크 동기화용 콜백
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (cart == null) return;

            if (stream.IsWriting)
            {
                float position = cart.m_Position;
                float speed = cart.m_Speed;
                int index = FindTrackIndex(cart.m_Path);

                stream.SendNext(position);
                stream.SendNext(speed);
                stream.SendNext(index);
            }
            else
            {
                netPosition = (float)stream.ReceiveNext();
                netSpeed = (float)stream.ReceiveNext();
                netTrackIndex = (int)stream.ReceiveNext();
            }
        }

        private int FindTrackIndex(CinemachinePathBase path)
        {
            if (path == null || tracks == null) return -1;
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i] == path) return i;
            }
            return -1;
        }
    }
}
