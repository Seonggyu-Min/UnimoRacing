using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionUIManager : MonoBehaviour
{
    [SerializeField] private GameObject SettingPanel;

    public void OpenOptionUI()
    {
        Debug.Log("OpenOptionUI called!");
        // UI 패널을 활성화하여 화면에 표시합니다.
        SettingPanel.SetActive(true);
    }
}
