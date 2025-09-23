using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyRoom1Opener : MonoBehaviour
{
    public void OnTouchToOpen()
    {
        UIManager.Instance.Show("MyRoomPopup1");
    }
    public void OnTouchToClose()
    {
        UIManager.Instance.Hide("MyRoomPopup1");
    }
}
