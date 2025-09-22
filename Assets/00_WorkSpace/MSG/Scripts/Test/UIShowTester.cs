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

        [Button("ShowImage")]
        public void ShowImage()
        {
            UIManager.Instance.Show("Image");
        }

        [Button("HideImage")]
        public void HideImage()
        {
            UIManager.Instance.Hide("Image");
        }
    }
}
