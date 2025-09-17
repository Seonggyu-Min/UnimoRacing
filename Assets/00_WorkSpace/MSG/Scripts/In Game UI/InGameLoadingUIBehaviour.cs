using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;


namespace MSG
{
    // TODO: 미니맵 UI 추가
    public class InGameLoadingUIBehaviour : MonoBehaviourPunCallbacks
    {
        [SerializeField] private GameObject _inGameUI;
        [SerializeField] private List<LoadingPlayerUIItem> _players = new();
        [SerializeField] private Image _loadingBarImage;

        private int _unimoFallbackIndex = 20001;

        HashSet<string> _preparedPlayer = new();


        private void Start()
        {
            MakePlayerUI();
        }


        private void Update()
        {
            float amount = Mathf.Clamp01((float)_preparedPlayer.Count / PhotonNetwork.CurrentRoom.PlayerCount);
            _loadingBarImage.fillAmount = amount;
            //Debug.Log($"[InGameLoadingUIBehaviour] amount = {amount}");
        }


        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (!changedProps.TryGetValue(PhotonNetworkCustomProperties.KEY_PLAYER_RACE_LOADED, out object ready)) return;
            if (ready is bool isReady && isReady == true)
            {
                _preparedPlayer.Add(targetPlayer.UserId);
                Debug.Log($"[InGameLoadingUIBehaviour] 플레이어 {targetPlayer.UserId}가 준비 완료됨");

                if (_preparedPlayer.Count >= PhotonNetwork.CurrentRoom.PlayerCount)
                {
                    OnReady();

                    //StartCoroutine(TestWaitOnReady());
                }
            }
        }


        private void MakePlayerUI()
        {
            if (PhotonNetwork.CurrentRoom == null)
            {
                Debug.LogError("[InGameLoadingUIBehaviour] 방에 있지 않습니다");
                return;
            }

            Player[] ordered = PhotonNetwork.PlayerList;
            int count = ordered.Length;

            if (_players.Count < count)
            {
                Debug.LogWarning($"[InGameLoadingUIBehaviour] UI 슬롯이 부족합니다. slots={_players.Count}, players={count}");
                count = _players.Count; // 넘치면 스킵
            }

            for (int i = 0; i < count; i++)
            {
                int slot = i;
                Player p = ordered[slot];

                string uid = p.UserId;
                int index = _unimoFallbackIndex;
                string nickname = "Loading...";

                _players[slot].gameObject.SetActive(true);
                _players[slot].Init(nickname, index);

                DatabaseManager.Instance.GetOnMain(
                    DBRoutes.Users(uid),
                    snap =>
                    {
                        string nn = nickname;
                        int uni = index;

                        var nickNode = snap.Child(DatabaseKeys.nickname);
                        if (nickNode != null && nickNode.Value != null)
                        {
                            nn = nickNode.Value.ToString();
                        }

                        var uniNode = snap.Child(DatabaseKeys.equipped).Child(DatabaseKeys.unimos);
                        if (uniNode != null && uniNode.Value != null)
                        {
                            if (!int.TryParse(uniNode.Value.ToString(), out uni))
                            {
                                Debug.LogWarning("[InGameLoadingUIBehaviour] 유니모 인덱스 파싱 실패");
                            }
                        }

                        if (slot < _players.Count)
                        {
                            _players[slot].Init(nn, uni);
                        }
                    },
                    err =>
                    {
                        Debug.LogWarning($"[InGameLoadingUIBehaviour] 플레이어 데이터 읽기 실패: {err}");
                        if (slot < _players.Count)
                        {
                            _players[slot].Init("Error", _unimoFallbackIndex);
                        }
                    });
            }
        }

        private void OnReady()
        {
            _inGameUI.SetActive(true);      // 준비됐으니 인게임 UI 켜주고
            gameObject.SetActive(false);    // 로딩은 종료
        }


        private IEnumerator TestWaitOnReady()
        {
            yield return new WaitForSeconds(3f);

            _inGameUI.SetActive(true);      // 준비됐으니 인게임 UI 켜주고
            gameObject.SetActive(false);    // 로딩은 종료
        }
    }
}
