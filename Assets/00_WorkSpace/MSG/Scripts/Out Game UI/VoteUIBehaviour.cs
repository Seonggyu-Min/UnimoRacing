using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;


namespace MSG
{
    public class VoteUIBehaviour : MonoBehaviourPunCallbacks
    {
        [Header("Refs")]
        [SerializeField] private Image _voteButtonImage;        // 버튼 자기 자신 (색을 바꾸기 위해 참조함)
        [SerializeField] private Image[] _voteSquares;          // 투표 몇 개 됐는지 확인용

        [Header("Vote Map Index")]
        [SerializeField][Range(1, 3)] private int _votingIndex; // 자신이 몇 번째 맵 투표를 맡고 있는지 (현재는 1 ~ 3 쓸 듯)

        [Header("Colors")]
        [SerializeField] private Color _winnerColor = Color.blue;    // 당선된 맵의 색
        [SerializeField] private Color _defaultColor = Color.white;   // 그렇지 않을 때 보여줄 색
        [SerializeField] private Color _myVoteColor = Color.green;   // 내가 선택한 맵의 색
        [SerializeField] private Color _myLoseVoteColor = Color.green;   // 내가 선택한 맵이 당선 실패 후 보여줄 색 (채도가 낮은 초록색이었음)


        private const string VOTE_MAP = "vote_map"; // 플레이어 프로퍼티 
        private const string VOTE_WINNER_INDEX = "vote_winner_index"; // 룸 프로퍼티
        private const int _defaultedVote = 1;


        public override void OnEnable()
        {
            base.OnEnable();

            UpdateVoteUI();
            UpdateButtonColor();
        }


        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey(VOTE_MAP))
            {
                UpdateVoteUI();
                UpdateButtonColor();
            }
        }

        public override void OnRoomPropertiesUpdate(Hashtable changedProps)
        {
            if (changedProps.ContainsKey(VOTE_WINNER_INDEX))
            {
                UpdateButtonColor();
            }
        }


        // 투표 버튼 클릭 연결용 메서드
        public void OnClickSubmitVote()
        {
            var prop = PhotonNetwork.LocalPlayer.CustomProperties;
            if ((prop.TryGetValue(VOTE_MAP, out var v) ? (int)v : _defaultedVote) == _votingIndex) return; // 이미 투표했으면 return

            var p = new Hashtable { [VOTE_MAP] = _votingIndex };
            PhotonNetwork.LocalPlayer.SetCustomProperties(p);
        }

        #region Private Methods

        private void UpdateVoteUI()
        {
            // 투표 몇 개 받았는지 확인
            int count = 0;
            foreach (var p in PhotonNetwork.CurrentRoom.Players.Values)
            {
                // 내 _votingIndex가 맞으면 count++
                if (p.CustomProperties != null &&
                    p.CustomProperties.TryGetValue(VOTE_MAP, out var v) &&
                    v is int idx &&
                    idx == _votingIndex)
                {
                    count++;
                }
            }

            // count 만큼 네모 활성화
            for (int i = 0; i < _voteSquares.Length; i++)
            {
                if (i < count)
                {
                    _voteSquares[i].gameObject.SetActive(true);
                }
                else
                {
                    _voteSquares[i].gameObject.SetActive(false);
                }
            }
        }

        private void UpdateButtonColor()
        {
            int myVote = GetMyVoteIndex();
            int winnerIdx = GetWinnerIndex(); // -1이면 아직 미정

            if (winnerIdx > 0)
            {
                // 투표 마감 이후, 당선 맵은 파랑, 내가 찍었지만 탈락이면 초록, 나머지는 회색.
                // 내가 찍었으면서 당선 맵이라면 파랑 그대로 보여줌
                if (_votingIndex == winnerIdx)
                    _voteButtonImage.color = _winnerColor;
                else if (_votingIndex == myVote)
                    _voteButtonImage.color = _myLoseVoteColor;
                else
                    _voteButtonImage.color = _defaultColor;
            }
            else // -1일 때 미정
            {
                // 투표 진행 중에는 내가 찍은 버튼만 초록, 그 외는 회색 처리
                _voteButtonImage.color = (_votingIndex == myVote) ? _myVoteColor : _defaultColor;
            }
        }

        private int GetMyVoteIndex()
        {
            var prop = PhotonNetwork.LocalPlayer?.CustomProperties;
            if (prop != null && prop.TryGetValue(VOTE_MAP, out var v) && v is int idx && idx > 0)
            {
                return idx;
            }
            Debug.Log("[VoteUIBehaviour] 내 투표가 없습니다.");
            return _defaultedVote;
        }

        private int GetWinnerIndex()
        {
            var room = PhotonNetwork.CurrentRoom;
            if (room != null &&
                room.CustomProperties != null &&
                room.CustomProperties.TryGetValue(VOTE_WINNER_INDEX, out var w) &&
                w is int idx)
            {
                return idx;
            }
            return -1; // 아직 투표된 것이 없음
        }

        #endregion
    }
}
