using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MSG
{
    public class VoteUIBehaviour : MonoBehaviour
    {
        [SerializeField] private Button map1Button;
        [SerializeField] private Button map2Button;
        [SerializeField] private Button map3Button;

        private void OnEnable()
        {
            map1Button.onClick.AddListener(OnVoteMap1);
            map2Button.onClick.AddListener(OnVoteMap2);
            map3Button.onClick.AddListener(OnVoteMap3);
        }

        private void OnDisable()
        {
            map1Button.onClick.RemoveListener(OnVoteMap1);
            map2Button.onClick.RemoveListener(OnVoteMap2);
            map3Button.onClick.RemoveListener(OnVoteMap3);
        }

        private void SubmitVote(int option)
        {
            if (!PhotonNetwork.InRoom) return;

            ExitGames.Client.Photon.Hashtable props = new(){ { "Vote", option } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            Debug.Log($"맵{option}으로 투표함");
        }

        private void OnVoteMap1() => SubmitVote(0);
        private void OnVoteMap2() => SubmitVote(1);
        private void OnVoteMap3() => SubmitVote(2);
    }
}
