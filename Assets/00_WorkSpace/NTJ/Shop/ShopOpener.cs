using MSG;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopOpener : MonoBehaviour
{
    public void OnTouchToOpen()
    {
        UIManager.Instance.Show("ShopPopup");
    }
    public void OnTouchToClose()
    {
        UIManager.Instance.Hide("ShopPopup");
    }
}
