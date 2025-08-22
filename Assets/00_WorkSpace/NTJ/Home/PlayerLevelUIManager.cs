using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLevelUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text levelText;       // 레벨 표시용 텍스트
    [SerializeField] private Image expFillBar;         // 경험치 게이지 (주황색)
    [SerializeField] private Button addExpButton;      // 테스트용 경험치 버튼

    [Header("Level Data")]
    [SerializeField] private int currentLevel = 1;     // 현재 레벨
    [SerializeField] private int currentExp = 0;       // 현재 경험치
    [SerializeField] private int expToNextLevel = 100; // 다음 레벨까지 필요한 경험치

    private void Start()
    {
        // 버튼 클릭 이벤트 등록
        addExpButton.onClick.AddListener(() => AddExp(30));

        // 시작 UI 업데이트
        UpdateUI();
    }

    public void AddExp(int amount)
    {
        currentExp += amount;

        // 경험치가 레벨업 조건을 만족했을 때
        while (currentExp >= expToNextLevel)
        {
            currentExp -= expToNextLevel; // 초과분은 다음 레벨에 누적
            currentLevel++;
            expToNextLevel = Mathf.RoundToInt(expToNextLevel * 1.2f); // 경험치통 점점 증가
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        // 레벨 텍스트 업데이트
        levelText.text = $"Lv. {currentLevel}";

        // 경험치 게이지 업데이트 (0 ~ 1 사이 값)
        float percent = (float)currentExp / expToNextLevel;
        expFillBar.fillAmount = Mathf.Clamp01(percent);
    }
}
