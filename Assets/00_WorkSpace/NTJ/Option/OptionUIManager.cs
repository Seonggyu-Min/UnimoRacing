using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptionUIManager : MonoBehaviour
{
    [SerializeField] private GameObject SettingPanel;

    public void OpenOptionUI()
    {
        Debug.Log("OpenOptionUI called!");
        // UI �г��� Ȱ��ȭ�Ͽ� ȭ�鿡 ǥ���մϴ�.
        SettingPanel.SetActive(true);
    }
}
