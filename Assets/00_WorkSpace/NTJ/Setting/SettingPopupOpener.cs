using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingPopupOpener : MonoBehaviour
{
    public void OnTouchToOpen()
    {
        UIManager.Instance.Show("SettingPopup");
    }
    public void OnTouchToClose()
    {
        UIManager.Instance.Hide("SettingPopup");
    }
}
