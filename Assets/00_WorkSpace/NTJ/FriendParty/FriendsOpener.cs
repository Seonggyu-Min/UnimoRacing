using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FriendsOpener : MonoBehaviour
{
    public void OnTouchToOpen()
    {
        UIManager.Instance.Show("FriendsPopup");
    }
    public void OnTouchToClose()
    {
        UIManager.Instance.Hide("FriendsPopup");
    }
}
