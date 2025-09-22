using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class UIShowTester : MonoBehaviour
    {
        [Button("Show Reward")]
        public void ShowReward()
        {
            UIManager.Instance.Show("Reward Panel");
        }
    }
}
