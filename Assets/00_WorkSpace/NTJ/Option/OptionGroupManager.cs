using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionGroupManager : MonoBehaviour
{
    // 인스펙터에서 UI 버튼들을 할당할 리스트
    [SerializeField]
    private List<Button> optionButtons;

    // 각 버튼의 체크 표시 이미지를 담을 리스트
    [SerializeField]
    private List<Image> checkmarkImages;

    [SerializeField] private Button closeButton;

    private void Start()
    {
        // 리스트에 있는 버튼들에 클릭 리스너를 추가합니다.
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i; // 클로저 문제 방지를 위해 인덱스 복사
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseFriendsUI);
        }

        // 게임 시작 시 기본 옵션을 설정합니다. (예: Medium)
        OnOptionSelected(1);
    }

    private void OnOptionSelected(int selectedIndex)
    {
        // 모든 체크 표시를 비활성화합니다.
        for (int i = 0; i < checkmarkImages.Count; i++)
        {
            checkmarkImages[i].enabled = false;
        }

        // 선택된 옵션의 체크 표시만 활성화합니다.
        checkmarkImages[selectedIndex].enabled = true; }

         public void CloseFriendsUI()
    {
        // 이 스크립트가 붙어있는 게임 오브젝트(친구 창 패널)를 비활성화합니다.
        gameObject.SetActive(false);
    }

    // TODO: 여기에 실제 그래픽 설정(품질 레벨)을 변경하는 코드를 추가하세요.
    // 예를 들어: QualitySettings.SetQualityLevel(selectedIndex, true);
}
