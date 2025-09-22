using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToggleVisualUpdater : MonoBehaviour
{
    public Toggle toggle;
    public Image targetImage;
    public TMP_Text targetText;

    public Color onColor = Color.white;
    public Color offColor = Color.grey;
    public Color onTextColor = Color.black;
    public Color offTextColor = Color.white;

    void Start()
    {
        if (toggle == null || targetImage == null)
        {
            Debug.LogError("토글 또는 대상 이미지가 할당되지 않았습니다!");
            return;
        }
        // 토글 상태가 변경될 때마다 색상 변경을 처리하는 리스너를 추가합니다.
        toggle.onValueChanged.AddListener(OnToggleValueChanged);
        // 초기 색상 설정
        OnToggleValueChanged(toggle.isOn);
    }

    private void OnToggleValueChanged(bool isOn)
    {
        // 이미지 색상 변경
        targetImage.color = isOn ? onColor : offColor;

        // 텍스트 컴포넌트가 할당되어 있다면 글씨 색상도 변경
        if (targetText != null)
        {
            targetText.color = isOn ? onTextColor : offTextColor;
        }
    }
}

