using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PresenceService : MonoBehaviour
    {
        [SerializeField] private bool _isInGame;

        private void Start()
        {
            PresenceLogic.Instance.SetOnlineStatus(true);
            PresenceLogic.Instance.SetInGameStatus(_isInGame);
        }
    }
}
