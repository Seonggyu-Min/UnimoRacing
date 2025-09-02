using Cinemachine;
using Photon.Pun;
using UnityEngine;
using YSJ.Util;

namespace PJW
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PhotonView))]
    public class DollyCartSync : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback
    {
        const float EPS = 0.001f; // 미세 흔들림 방지

        [Header("Refs")]
        [SerializeField] private CinemachineDollyCart cart;

        [Header("Sync")]
        [SerializeField, Range(0f, 1f)] private float smoothing = 1.0f;

        private CinemachinePathBase[] tracks;   // 이동할 수 있는 트랙

        private float _netSpeed;
        private int _netTrackIndex;


        private bool _isRacable = false;
        private float _raceSpeed;

        private float _posUnwrapped;           // 현재(보간된) 언랩 위치
        private float _targetUnwrapped;        // 목표 언랩 위치
        private float _lastNetNormPos = float.NaN;
        private int   _lastNetTrackIndex = -1;


        private int   _sendLap = 0;
        private float _lastSentNorm = 0.0f;

        private int _raceLap = 0;
        private int _endLap = int.MinValue;

        private bool _isFinished = false;

        public int RaceLap => _raceLap;

        private void Awake()
        {
            // 씬의 트랙 목록 가지고 오기
            tracks = FindObjectsOfType<CinemachinePathBase>(true);

            _isRacable = false;

            // 필요 이벤트 연결
            UnLinkAction();
            LinkAction();
        }


        // 기본적으로 속도는
        // - 넷 속도(_netSpeed): "네트워크에서 받은" 속도(넷에서 받은 값)
        // - 로컬 속도(=cart.m_Speed): 내가 가진 실제 속도(소유자 입장에선 송신할 값)
        // - 카 속도(=cart.m_Speed): DollyCart에 최종 적용되는 연출용 속도(필수 아님; 위치가 진짜임)
        private void Start()
        {
            if (cart != null && cart.m_Path != null)
            {
                // 경로를 따라 움직일 때, 내부적으로 위치를 어떤 단위로 다룰지 정해줌(0~1 정규화로 다룰거임)
                cart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;

                _netSpeed = cart.m_Speed;                           
                _netTrackIndex = FindTrackIndex(cart.m_Path);       

                _raceSpeed = _netSpeed;                             
                _netSpeed = 0;                                      
                cart.m_Speed = 0;                                   

                _posUnwrapped = cart.m_Position;                    
                _targetUnwrapped = _posUnwrapped;                   
                _lastNetNormPos = cart.m_Position;                  

                // 송신측 랩 상태 초기화
                _lastSentNorm = cart.m_Position;                    
                _sendLap = 0;
                _raceLap = 0;
            }
        }

        private void Update()
        {
            if (cart == null) return;                               
            if (!_isRacable) return;                                

            if (photonView.IsMine)
            {
                float norm = Mathf.Repeat(cart.m_Position, 1f);
                CheckLap(norm);
                PlayerLapCheck((float)PhotonNetwork.Time);
                return;
            }

            if (tracks != null && _netTrackIndex >= 0 && _netTrackIndex < tracks.Length)
            {
                if (cart.m_Path != tracks[_netTrackIndex])
                {
                    cart.m_Path = tracks[_netTrackIndex];
                    
                    if (!float.IsNaN(_lastNetNormPos))
                    {
                        _posUnwrapped = Mathf.Floor(_posUnwrapped) + _lastNetNormPos;
                        _targetUnwrapped = _posUnwrapped;
                    }
                    // 위에 코드로 진행 상황 갱신
                    _lastNetTrackIndex = _netTrackIndex; // 트랙 갱신
                }
            }

            cart.m_PositionUnits = CinemachinePathBase.PositionUnits.Normalized;
            cart.m_Speed = Mathf.Lerp(cart.m_Speed, _netSpeed, Time.deltaTime * smoothing);
            _posUnwrapped = Mathf.Lerp(_posUnwrapped, _targetUnwrapped, Time.deltaTime * smoothing);

            // Repeat
            // (-0.10f, 1f) => 0.90   // 음수도 안전하게 변경됨
            // ( 1.00f, 1f) => 0.00   // 1은 0과 같은 자리
            cart.m_Position = Mathf.Repeat(_posUnwrapped, 1f);
        }

        private void OnDestroy()
        {
            UnLinkAction();
        }


        #region PhotonNetwork
        // 네트워크 동기화용 콜백
        // 보내는 쪽 & 받는 쪽 필요
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (cart == null) return;

            if (stream.IsWriting)
            {
                Send(stream, info);
            }
            else
            {
                Receive(stream, info);
            }
        }

        // 데이터 보내기
        private void Send(PhotonStream stream, PhotonMessageInfo info)
        {
            float norm = Mathf.Repeat(cart.m_Position, 1f);
            CheckLap(norm);
            _sendLap = _raceLap;

            int index = FindTrackIndex(cart.m_Path);

            // 패킷 포맷: [norm, lap, speed, trackIndex]
            stream.SendNext(norm);
            stream.SendNext(_sendLap);
            stream.SendNext(cart.m_Speed);
            stream.SendNext(index);
        }

        // 데이터 받기 > 실 카트 갱신은 Update()
        private void Receive(PhotonStream stream, PhotonMessageInfo info)
        {
            // 패킷 포맷: [norm(0~1), lap(int), speed, trackIndex]
            float recvNorm      = (float)stream.ReceiveNext();
            int   recvLap       = (int)stream.ReceiveNext();
            float recvSpeed     = (float)stream.ReceiveNext();
            _netTrackIndex      = (int)stream.ReceiveNext();

            // 지연 보정
            double now  = PhotonNetwork.Time;
            double sent = info.SentServerTime;
            float  lag  = Mathf.Max(0f, (float)(now - sent));
            float  predictedNorm = Mathf.Repeat(recvNorm + recvSpeed * lag, 1f);

            _netSpeed = recvSpeed;
            _raceLap = recvLap; // UI나 등등에서 사용

            // 언랩 목표 복원: lap + predictedNorm
            float predictedUnwrapped = recvLap + predictedNorm;

            if (float.IsNaN(_lastNetNormPos))
            {
                _posUnwrapped = predictedUnwrapped;     // 바로 맞춰 놓고
                _targetUnwrapped = predictedUnwrapped;  // 목표도 동일하게(오차 0으로 시작)
                _lastNetNormPos = predictedNorm;        // 이후 비교 기준
                return;
            }

            // 이후엔 타겟만 갱신 > Update()에서 Lerp로 부드럽게 수렴
            _targetUnwrapped = predictedUnwrapped;
            _lastNetNormPos = predictedNorm;
        }

        // 스폰 진행 한쪽에서 넣어준 object형 배열 데이터를 받아서 처리
        // 해당 오브젝트 식별을 위한 오브젝트 이름 수정
        // 데이터 받으려면 IPunInstantiateMagicCallback 필요함
        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            var view = GetComponent<PhotonView>();
            if (view == null)
                return;

            // InstantiationData 읽기
            object[] data = view.InstantiationData;

            int actorNumber = view.Owner != null ? view.Owner.ActorNumber : 0;
            string userId = view.Owner != null ? view.Owner.UserId : null;

            if (data != null)
            {
                // 안전하게 꺼내기 (전송한 순서 > actor, startIndex, characterID, kartID, userId, endLapCount)

                // actorNumber
                if (data.Length >= 1 && data[0] is int an)
                    actorNumber = an;

                // track
                if ((data.Length >= 2 && data[1] is int si)
                    && (tracks != null && tracks[si] != null))
                    cart.m_Path = tracks[si];

                // userId
                if (data.Length >= 5 && data[4] is string uid && !string.IsNullOrEmpty(uid))
                    userId = uid;

                // endLapCount
                if (data.Length >= 1 && data[0] is int endlap)
                {
                    if (ReInGameManager.Instance.RaceEndLapCount == endlap)
                    {
                        _endLap = endlap;
                    }
                    else
                    {
                        _endLap = 1;
                    }
                }

                // 일단은 끝나는 렙 수를 받을 거임
                // 그런데 처리를 어디에서 해주냐임.

                // 일단 여기에서 해주면 안됨. 문제가 생김
            }

            string safeUserId = string.IsNullOrEmpty(userId) ? "NoUserId" : userId;
            gameObject.name = $"Kart_Actor{actorNumber}_{safeUserId}";
        }


        #endregion

        #region About InGameManager Action
        // Link
        private void UnLinkAction()
        {
            var mgr = ReInGameManager.Instance;
            mgr.OnRaceState_LoadPlayers -= ActionDontMove;
            mgr.OnRaceState_Racing -= ActionRaceStart;
            mgr.OnRaceState_Finish -= ActionDontMove;
        }
        private void LinkAction()
        {
            var mgr = ReInGameManager.Instance;

            mgr.OnRaceState_LoadPlayers += ActionDontMove;
            mgr.OnRaceState_Racing += ActionRaceStart;
            mgr.OnRaceState_Finish += ActionDontMove;
        }

        // Action
        private void ActionDontMove()
        {
            _isRacable = false;
            cart.m_Speed = 0;
            _netSpeed = 0;

            this.PrintLog($"Server Time: {PhotonNetwork.Time}, [DontMove] > {this.gameObject.name} / {_raceSpeed} > {cart.m_Speed}");
        }
        private void ActionRaceStart()
        {
            float moveS = cart.m_Speed;
            _isRacable = true;
            cart.m_Speed = _raceSpeed;
            _netSpeed = _raceSpeed;
            this.PrintLog($"Server Time: {PhotonNetwork.Time}, [RaceStart] > {this.gameObject.name} / {moveS} > {_raceSpeed}");
        }

        #endregion

        // 트랙 변경해 가지고 오기
        private int FindTrackIndex(CinemachinePathBase path)
        {
            if (path == null || tracks == null) return -1;
            for (int i = 0; i < tracks.Length; i++)
            {
                if (tracks[i] == path) return i;
            }
            return -1;
        }
        private void CheckLap(float norm)
        {
            // 첫 프레임 방어(직전 표본 없으면 기준만 세팅)
            if (float.IsNaN(_lastSentNorm))
            {
                _lastSentNorm = norm;
            }

            // 랩 감지: '접기 전(raw)' 차이로만 판단
            float deltaRaw = norm - _lastSentNorm;
            if (cart.m_Speed > EPS && deltaRaw < -0.5f)
            {
                _raceLap++;
            }
            else if (cart.m_Speed < -EPS && deltaRaw > +0.5f)
            {
                _raceLap--;
            }

            _lastSentNorm = norm;
        }

        private void PlayerLapCheck(float tiem)
        {
            if (_endLap > _raceLap) return;
            if (_isFinished) return;

            PhotonNetworkCustomProperties.LocalPlayerRaceFinishedSetting(tiem);
            _isFinished = true;
        }
    }
}
