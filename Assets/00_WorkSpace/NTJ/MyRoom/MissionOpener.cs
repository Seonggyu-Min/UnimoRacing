using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MissionOpener : MonoBehaviour
    {
        public void OnClickShowMissionButton()
        {
            UIManager.Instance.Show("Mission Panel");
        }

        public void OnClickHideMissionButton()
        {
            UIManager.Instance.Hide("Mission Panel");
        }
    }
}
