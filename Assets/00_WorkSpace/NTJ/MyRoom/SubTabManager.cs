using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SubTabManager : MonoBehaviour
{
    private MyRoomManager myRoomManager;
    // 토글 그룹
    public Toggle[] subToggles; // 스탯, 스킬, 스킨 등

    // 정보 패널
    public TMP_Text itemNameText;
    public TMP_Text itemLevelText;
    public TMP_Text itemDescriptionText;
    public Image characterImage; // 오른쪽 캐릭터 이미지

    // 스탯, 스킬, 스킨, 인연 등 각 탭에 해당하는 UI 패널
    public GameObject[] tabPanels;

    private void Start()
    {
        // MyRoomManager 인스턴스 찾기
        myRoomManager = FindObjectOfType<MyRoomManager>();
        if (myRoomManager == null)
        {
            Debug.LogError("마이룸 인스턴스 찾기 실패.");
            return;
        }
        // 모든 토글에 리스너 추가
        for (int i = 0; i < subToggles.Length; i++)
        {
            int index = i;
            subToggles[i].onValueChanged.AddListener(isOn =>
            {
                OnToggleChanged(index, isOn);
            });
        }

        // 초기 탭 설정
        OnToggleChanged(0, true); // 첫 번째 탭 활성화
    }

    private void OnToggleChanged(int index, bool isOn)
    {
        if (!isOn) return;

        // 모든 패널 비활성화
        foreach (GameObject panel in tabPanels)
        {
            panel.SetActive(false);
        }

        // 선택된 패널만 활성화
        if (index >= 0 && index < tabPanels.Length)
        {
            tabPanels[index].SetActive(true);
            UpdateInfoPanel(index); // 정보 패널 업데이트
        }
    }

    private void UpdateInfoPanel(int tabIndex)
    {
        // 탭 인덱스에 따라 다른 정보 표시
        // 예시: 0번 탭(스탯), 1번 탭(스킬), 2번 탭(스킨)
        string name = "초보 붕붕엔진";
        int level = 3;
        string description = "초보자를 위한 붕붕엔진입니다.";

        // 실제로는 데이터베이스나 스크립터블 오브젝트에서 정보를 가져와야 함
        // 예: itemData.name, itemData.level, itemData.description

        itemNameText.text = name;
        itemLevelText.text = "LV." + level;
        itemDescriptionText.text = description;
    }

    // 캐릭터 이미지 업데이트 메서드
    public void UpdateCharacterDisplay()
    {
        if (myRoomManager != null)
        {
            // public 프로퍼티를 통해 캐릭터 스프라이트 가져오기
            Sprite characterSprite = myRoomManager.CurrentEquippedCharacterSprite;

            // 가져온 스프라이트를 SubTabManager의 Image 컴포넌트에 할당
            characterImage.sprite = characterSprite;
        }
    }

    // 카트 이미지 업데이트 메서드
    public void UpdateKartDisplay()
    {
        if (myRoomManager != null)
        {
            // public 프로퍼티를 통해 카트 스프라이트 가져오기
            Sprite kartSprite = myRoomManager.CurrentEquippedKartSprite;

            // 가져온 스프라이트를 SubTabManager의 Image 컴포넌트에 할당
            characterImage.sprite = kartSprite;
        }
    }
}
