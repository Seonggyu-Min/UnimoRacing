using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyRoom2Opener : MonoBehaviour
{
    public void OnTouchToOpen()
    {
        UIManager.Instance.Show("MyRoomPopup2");
    }
    public void OnTouchToClose()
    {
        UIManager.Instance.Hide("MyRoomPopup2");
    }
}
