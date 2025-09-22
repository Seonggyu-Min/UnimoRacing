using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryManager : MonoBehaviour
{
    public Toggle unimoToggle;
    public Toggle kartToggle;

    public GameObject unimoUIGroup;
    public GameObject kartUIGroup;

    public SubTabManager subTabManager;

    // 추가: 유니모 토글의 이미지와 텍스트
    public Image unimoImage;
    public TMP_Text unimoText;

    // 추가: 카트 토글의 이미지와 텍스트
    public Image kartImage;
    public TMP_Text kartText;

    // 추가: 색상 변수
    public Color onColor = Color.white;
    public Color offColor = Color.grey;
    public Color onTextColor = Color.black;
    public Color offTextColor = Color.white;

    private void Start()
    {
        unimoToggle.onValueChanged.AddListener(OnUnimoToggle);
        kartToggle.onValueChanged.AddListener(OnKartToggle);

        // 초기 상태에 맞춰 UI 색상을 설정합니다.
        UpdateToggleColors(unimoToggle.isOn, kartToggle.isOn);
    }



    private void OnUnimoToggle(bool isOn)
    {
        if (isOn)
        {
            unimoUIGroup.SetActive(true);
            kartUIGroup.SetActive(false);
            subTabManager.UpdateCharacterDisplay();
            // 유니모 토글이 켜지면 색상을 업데이트합니다.
            UpdateToggleColors(true, false);
        }
    }

    private void OnKartToggle(bool isOn)
    {
        if (isOn)
        {
            unimoUIGroup.SetActive(false);
            kartUIGroup.SetActive(true);
            subTabManager.UpdateKartDisplay();
            // 카트 토글이 켜지면 색상을 업데이트합니다.
            UpdateToggleColors(false, true);
        }
    }

    // 추가: 토글 상태에 따라 색상을 업데이트하는 함수
    private void UpdateToggleColors(bool isUnimoOn, bool isKartOn)
    {
        // 유니모 토글 색상 업데이트
        if (unimoImage != null) unimoImage.color = isUnimoOn ? onColor : offColor;
        if (unimoText != null) unimoText.color = isUnimoOn ? onTextColor : offTextColor;

        // 카트 토글 색상 업데이트
        if (kartImage != null) kartImage.color = isKartOn ? onColor : offColor;
        if (kartText != null) kartText.color = isKartOn ? onTextColor : offTextColor;
    }
}