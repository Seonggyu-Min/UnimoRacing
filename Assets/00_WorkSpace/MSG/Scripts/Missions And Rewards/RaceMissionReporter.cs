using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class RaceMissionReporter : MonoBehaviour
    {
        private void Start()
        {
            InGameManager.Instance.OnStateChanged += CheckEnd;
        }

        private void OnDisable()
        {
            if (InGameManager.GetInstance != null)
            {
                InGameManager.Instance.OnStateChanged -= CheckEnd;
            }
        }

        private void CheckEnd(RaceState state)
        {
            if (state == RaceState.Finish)
            {
                bool isParty = PartyService.Instance.IsInParty ? true : false;
                MissionService.Instance.Report(MissionVerb.Finish, MissionObject.Race, isParty, 1);
            }
        }
    }
}
