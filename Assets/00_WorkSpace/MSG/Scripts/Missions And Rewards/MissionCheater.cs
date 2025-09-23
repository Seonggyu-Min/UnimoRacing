using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MSG
{
    public class MissionCheater : MonoBehaviour
    {
        public MissionVerb MissionVerb;
        public MissionObject MissionObject;
        public bool IsParty;
        public int Amount;

        [Button("IncreaseMissionProgress")]
        public void IncreaseMissionProgress()
        {
            MissionService.Instance.Report(MissionVerb, MissionObject, IsParty, Amount);
        }
    }
}
