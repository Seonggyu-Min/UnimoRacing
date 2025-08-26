using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLevelUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text levelText;       // ���� ǥ�ÿ� �ؽ�Ʈ
    [SerializeField] private Image expFillBar;         // ����ġ ������ (��Ȳ��)
    [SerializeField] private Button addExpButton;      // �׽�Ʈ�� ����ġ ��ư

    [Header("Level Data")]
    [SerializeField] private int currentLevel = 1;     // ���� ����
    [SerializeField] private int currentExp = 0;       // ���� ����ġ
    [SerializeField] private int expToNextLevel = 100; // ���� �������� �ʿ��� ����ġ

    private void Start()
    {
        // ��ư Ŭ�� �̺�Ʈ ���
        addExpButton.onClick.AddListener(() => AddExp(30));

        // ���� UI ������Ʈ
        UpdateUI();
    }

    public void AddExp(int amount)
    {
        currentExp += amount;

        // ����ġ�� ������ ������ �������� ��
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel; // �ʰ����� ���� ������ ����
            currentLevel++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.2f); // ����ġ�� ���� ����
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        // ���� �ؽ�Ʈ ������Ʈ
        levelText.text = $"Lv. {currentLevel}";

        // ����ġ ������ ������Ʈ (0 ~ 1 ���� ��)
        float percent = (float)currentExp / expToNextLevel;
        expFillBar.fillAmount = Mathf.Clamp01(percent);
    }
}
