using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryManager : MonoBehaviour
{
    // 이벤트로 장착된 아이템 정보를 다른 스크립트에 전달합니다.
    public static event Action<string, Texture, int> OnItemEquipped;

    [Header("Category Toggles")]
    public Toggle unimoToggle;
    public Toggle kartToggle;

    [Header("Category UI Groups")]
    public GameObject unimoUIGroup;
    public GameObject kartUIGroup;

    [Header("Toggle Colors")]
    public Color onColor = Color.white;
    public Color offColor = Color.grey;
    public Color onTextColor = Color.black;
    public Color offTextColor = Color.white;

    // 아이템 정보 패널
    [Header("Item Info Panel")]
    public TMP_Text itemNameText;
    public TMP_Text itemLevelText;
    public TMP_Text itemDescriptionText;

    // 현재 장착된 아이템의 3D 텍스처 (다른 스크립트에서도 접근 가능)
    private static Texture _equippedUnimoTexture;
    private static Texture _equippedKartTexture;

    // 현재 선택된 아이템의 ID
    private string _currentEquippedUnimoId;
    private string _currentEquippedKartId;

    private void Start()
    {
        // 토글 리스너 추가
        unimoToggle.onValueChanged.AddListener(OnUnimoToggle);
        kartToggle.onValueChanged.AddListener(OnKartToggle);

        // 초기 상태 설정: 유니모 탭이 기본으로 활성화
        if (unimoToggle.gameObject.activeInHierarchy)
        {
            unimoToggle.isOn = true;
            OnUnimoToggle(true);
        }
    }

    private void OnUnimoToggle(bool isOn)
    {
        if (isOn)
        {
            unimoUIGroup.SetActive(true);
            kartUIGroup.SetActive(false);

            // 유니모 탭 활성화 시 기본 정보 업데이트 (예시)
            // 실제로는 현재 장착된 유니모 데이터를 로드해야 함
            UpdateMainUI("유니모 이름", "유니모 설명", 0, _equippedUnimoTexture);

            UpdateToggleColors(true, false);
        }
    }

    private void OnKartToggle(bool isOn)
    {
        if (isOn)
        {
            unimoUIGroup.SetActive(false);
            kartUIGroup.SetActive(true);

            // 카트 탭 활성화 시 기본 정보 업데이트 (예시)
            // 실제로는 현재 장착된 카트 데이터를 로드해야 함
            UpdateMainUI("초보 붕붕엔진", "초보자를 위한 붕붕엔진입니다.", 3, _equippedKartTexture);

            UpdateToggleColors(false, true);
        }
    }

    /// <summary>
    /// 메인 정보 패널 UI를 업데이트하고, 이벤트로 다른 UI에 정보를 전달합니다.
    /// </summary>
    /// <param name="name">아이템 이름</param>
    /// <param name="description">아이템 설명</param>
    /// <param name="level">아이템 레벨</param>
    /// <param name="texture">아이템 이미지 텍스처</param>
    public void UpdateMainUI(string name, string description, int level, Texture texture)
    {
        itemNameText.text = name;
        itemDescriptionText.text = description;

        if (level > 0)
        {
            itemLevelText.gameObject.SetActive(true);
            itemLevelText.text = "LV." + level;
        }
        else
        {
            itemLevelText.gameObject.SetActive(false);
        }

        // 아이템 장착 시, 이벤트 호출
        // 이 로직은 `KartInventoryUI.cs`나 장착 버튼 클릭 시 호출되어야 합니다.
        // OnItemEquipped?.Invoke(name, texture, level);
    }

    // 토글 상태에 따라 색상을 업데이트하는 함수
    private void UpdateToggleColors(bool isUnimoOn, bool isKartOn)
    {
        // 각 토글의 이미지를 참조하여 색상 변경
        Image unimoImage = unimoToggle.targetGraphic as Image;
        TMP_Text unimoText = unimoToggle.GetComponentInChildren<TMP_Text>();
        Image kartImage = kartToggle.targetGraphic as Image;
        TMP_Text kartText = kartToggle.GetComponentInChildren<TMP_Text>();

        if (unimoImage != null) unimoImage.color = isUnimoOn ? onColor : offColor;
        if (unimoText != null) unimoText.color = isUnimoOn ? onTextColor : offTextColor;

        if (kartImage != null) kartImage.color = isKartOn ? onColor : offColor;
        if (kartText != null) kartText.color = isKartOn ? onTextColor : offTextColor;
    }

    /// <summary>
    /// 다른 스크립트(예: 마이룸2)에서 장착한 아이템 정보를 설정합니다.
    /// </summary>
    public void SetEquippedItem(string itemId, Texture itemTexture)
    {
        if (itemId.StartsWith("unimo"))
        {
            _equippedUnimoTexture = itemTexture;
            _currentEquippedUnimoId = itemId;
        }
        else if (itemId.StartsWith("kart"))
        {
            _equippedKartTexture = itemTexture;
            _currentEquippedKartId = itemId;
        }
    }
}