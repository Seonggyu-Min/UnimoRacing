using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;


namespace MSG
{
    public class VoteButtonBehaviour : MonoBehaviourPunCallbacks
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


        public override void OnEnable()
        {
            base.OnEnable();

            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            {
                UpdateVoteUI();
                UpdateButtonColor();
            }
        }


        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey(PhotonNetworkCustomProperties.KEY_VOTE_MAP))
            {
                UpdateVoteUI();
                UpdateButtonColor();
            }
        }

        public override void OnRoomPropertiesUpdate(Hashtable changedProps)
        {
            if (changedProps.ContainsKey(PhotonNetworkCustomProperties.KEY_VOTE_WINNER_INDEX) ||
                changedProps.ContainsKey(PhotonNetworkCustomProperties.KEY_ROOM_VOTE_STATE))
            {
                UpdateVoteUI();
                UpdateButtonColor();
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            UpdateVoteUI();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            UpdateVoteUI();
        }



        // 투표 버튼 클릭 연결용 메서드
        public void OnClickSubmitVote()
        {
            if (PhotonNetworkCustomProperties.TryGetRoomProp(RoomKey.VoteEndTime, out double endAt))
            {
                if (endAt < PhotonNetwork.Time)
                {
                    Debug.Log($"[VoteButtonBehaviour] 투표 종료 시간이 지나 return");
                }
            }

            int voted = PlayerManager.Instance.GetPlayerCPVoteIndex();
            if (voted == _votingIndex) return; // 이미 내가 투표한거랑 같으면 return

            PlayerManager.Instance.SetPlayerCPVote(_votingIndex); // 다르면 Set
        }

        #region Private Methods

        private void UpdateVoteUI()
        {
            // 투표 몇 개 받았는지 확인
            int count = 0;
            foreach (var p in PhotonNetwork.CurrentRoom.Players.Values)
            {
                // 내 _votingIndex가 맞으면 count++
                if (PhotonNetworkCustomProperties.GetPlayerProp(p, PlayerKey.VotedMap, 0) == _votingIndex)
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
            int myVote = PlayerManager.Instance.GetPlayerCPVoteIndex();
            int winnerIdx = PhotonNetworkCustomProperties.GetRoomProp(
                RoomKey.WinnerMapIndex,
                -1, // -1이면 아직 미정
                onError: () => Debug.LogWarning("[VoteUIBehaviour] GetRoomProp 실패"
                ));

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

        #endregion
    }
}
