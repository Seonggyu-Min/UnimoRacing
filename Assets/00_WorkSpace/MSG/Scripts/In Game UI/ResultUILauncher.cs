using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;


namespace MSG
{
    public class ResultUILauncher : MonoBehaviourPunCallbacks
    {
        [SerializeField] private RaceResultUIBehaviour _resultUI;


        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            if (changedProps.ContainsKey(PhotonNetworkCustomProperties.KEY_PLAYER_RACE_IS_FINISHED))
            {
                if (changedProps.TryGetValue(PhotonNetworkCustomProperties.KEY_PLAYER_RACE_IS_FINISHED, out object finished))
                {
                    if (finished is bool isFinished && isFinished == true)
                    {
                        _resultUI.gameObject.SetActive(true);
                    }
                }
            }
        }
    }
}
