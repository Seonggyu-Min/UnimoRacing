using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;


namespace MSG
{
    public class RaceResultUIBehaviour : MonoBehaviourPunCallbacks
    {
        #region Fields and Properties

        [SerializeField] private List<PlayerUIItem> _players = new();

        private Dictionary<string, string> _nickCache = new();
        private Dictionary<string, int> _unimoCache = new();

        private const string NICK_FALLBACK = "Error";
        private const int UNIMO_FALLBACK = 20001;

        private int PlayerCount => PhotonNetwork.PlayerList.Length;

        #endregion


        #region Unity Methods

        private void Start()
        {
            CachePlayer();
        }

        public override void OnEnable()
        {
            base.OnEnable();
            RenewPlayerResultUI();
        }

        #endregion


        #region Caching Methods

        private void CachePlayer()
        {
            foreach (var player in PhotonNetwork.PlayerList)
            {
                string uid = player.UserId;

                // 닉네임 캐싱
                DatabaseManager.Instance.GetOnMain(
                    DBRoutes.Nickname(uid),
                    snap =>
                    {
                        _nickCache[uid] = snap?.Value?.ToString() ?? NICK_FALLBACK;
                        CheckCachedAll();
                    },
                    err =>
                    {
                        _nickCache[uid] = NICK_FALLBACK;
                        CheckCachedAll();
                        Debug.LogError($"[RaceResultUIBehaviour] 닉네임 캐싱 오류: {err}");
                    });

                // 유니모 인덱스 캐싱
                DatabaseManager.Instance.GetOnMain(
                    DBRoutes.EquippedUnimo(uid),
                    snap =>
                    {
                        _unimoCache[uid] = ParseIntSafe(snap?.Value);
                        CheckCachedAll();
                    },
                    err =>
                    {
                        _unimoCache[uid] = UNIMO_FALLBACK;
                        CheckCachedAll();
                        Debug.LogError($"[RaceResultUIBehaviour] 장착 유니모 캐싱 오류: {err}");
                    });
            }
        }

        // 모든 플레이어의 닉네임과 유니모 인덱스를 캐싱했으면 꺼두기
        private void CheckCachedAll()
        {
            if (_nickCache.Count == PlayerCount && _unimoCache.Count == PlayerCount)
            {
                gameObject.SetActive(false);    // 캐싱 후 끌 때는 애니메이션 없이 꺼주기
                //UIManager.Instance.Hide("Result UI Panel");
            }
        }

        private int ParseIntSafe(object v, int fallback = UNIMO_FALLBACK)
        {
            if (v == null) return fallback;
            if (v is int i) return i;
            if (v is long l) return (int)l;
            if (v is double d) return (int)d;
            if (int.TryParse(v.ToString(), out var r)) return r;
            return fallback;
        }

        #endregion


        #region UI Set Methods

        // 이제 이거 받을 필요 없을 듯
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // 플레이어 완주 할 때마다 UI 갱신
            if (changedProps.ContainsKey(PhotonNetworkCustomProperties.KEY_PLAYER_RACE_IS_FINISHED))
            {
                RenewPlayerResultUI();
            }
        }

        private void RenewPlayerResultUI()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

            var list = PhotonNetwork.CurrentRoom.Players.Values.ToList();

            // 완주자, 기록, ActorNumber 순으로 정렬
            list.Sort((a, b) =>
            {
                bool af = GetFin(a), bf = GetFin(b);
                if (af != bf) return bf.CompareTo(af);

                double ta = GetElapsedSec(a), tb = GetElapsedSec(b);
                int c = ta.CompareTo(tb);
                if (c != 0) return c;

                return a.ActorNumber.CompareTo(b.ActorNumber);
            });

            for (int i = 0; i < _players.Count; i++)
            {
                if (i < list.Count)
                {
                    var p = list[i];
                    var uid = p.UserId;

                    string name = _nickCache.TryGetValue(uid, out var nn) ? nn : NICK_FALLBACK;
                    int uni = _unimoCache.TryGetValue(uid, out var u) ? u : UNIMO_FALLBACK;

                    bool finished = GetFin(p);
                    double elapsed = GetElapsedSec(p);

                    _players[i].gameObject.SetActive(true);
                    _players[i].InitForResult(
                        name, uni, p.IsLocal,
                        rank: i + 1,
                        finished: finished,
                        time: double.IsInfinity(elapsed) ? 0f : (float)elapsed
                    );
                }
                else _players[i].gameObject.SetActive(false);
            }
        }

        private bool GetFin(Player p) => PhotonNetworkCustomProperties.GetPlayerProp<bool>(p, PlayerKey.RaceIsFinished);

        private double GetFinishAt(Player p)
            => PhotonNetworkCustomProperties.GetPlayerProp(p, PlayerKey.RaceFinishedTime, double.PositiveInfinity);

        private double GetRaceStartAt()
            => PhotonNetworkCustomProperties.GetRoomProp(RoomKey.RaceStartTime, -1d);

        private double GetElapsedSec(Player p)
        {
            if (!GetFin(p)) return double.PositiveInfinity;
            double start = GetRaceStartAt();
            double fin = GetFinishAt(p);
            if (start < 0d || double.IsInfinity(fin)) return double.PositiveInfinity;

            double elapsed = fin - start;
            return (elapsed < 0d) ? 0d : elapsed;
        }

        #endregion
    }
}
