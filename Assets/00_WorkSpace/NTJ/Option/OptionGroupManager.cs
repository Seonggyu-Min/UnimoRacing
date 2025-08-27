using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// PopupBase를 상속받아 팝업 기능을 통합
public class OptionGroupManager : PopupBase
{
    // 인스펙터에서 UI 버튼들을 할당할 리스트
    [SerializeField]
    private List<Button> optionButtons;

    // 각 버튼의 체크 표시 이미지를 담을 리스트
    [SerializeField]
    private List<Image> checkmarkImages;

    // Start() 대신 Awake()를 사용해 초기화 로직을 팝업이 활성화되기 전에 실행
    private void Awake()
    {
        // 리스트에 있는 버튼들에 클릭 리스너를 추가합니다.
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i; // 클로저 문제 방지를 위해 인덱스 복사
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }
    }

    // 팝업을 열 때 호출되는 메서드 (PopupBase에서 상속)
    public override void Open()
    {
        base.Open(); // PopupBase의 Open()을 호출하여 게임 오브젝트를 활성화

        OnOptionSelected(1);
    }

    // 팝업을 닫을 때 호출되는 메서드 (PopupBase에서 상속)
    public override void Close()
    {
        base.Close(); // PopupBase의 Close()를 호출하여 게임 오브젝트를 비활성화
    }

    private void OnOptionSelected(int selectedIndex)
    {
        // 모든 체크 표시를 비활성화합니다.
        for (int i = 0; i < checkmarkImages.Count; i++)
        {
            // 리스트 인덱스 범위 확인
            if (i < checkmarkImages.Count)
            {
                checkmarkImages[i].enabled = false;
            }
        }

        // 선택된 옵션의 체크 표시만 활성화합니다.
        if (selectedIndex >= 0 && selectedIndex < checkmarkImages.Count)
        {
            checkmarkImages[selectedIndex].enabled = true;
        }
    }

    // TODO: 여기에 실제 그래픽 설정(품질 레벨)을 변경하는 코드를 추가하세요.
    // 예를 들어: QualitySettings.SetQualityLevel(selectedIndex, true);
}
