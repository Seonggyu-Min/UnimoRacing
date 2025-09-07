using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PresenceService : MonoBehaviour
    {
        [SerializeField] private PresenceLogic _presenceLogic;
        [SerializeField] private bool _isInGame;

        private void Start()
        {
            _presenceLogic.SetOnlineStatus(true);
            _presenceLogic.SetInGameStatus(_isInGame);
        }
    }
}
