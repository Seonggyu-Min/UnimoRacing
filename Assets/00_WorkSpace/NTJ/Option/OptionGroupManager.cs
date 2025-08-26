using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionGroupManager : MonoBehaviour
{
    // �ν����Ϳ��� UI ��ư���� �Ҵ��� ����Ʈ
    [SerializeField]
    private List<Button> optionButtons;

    // �� ��ư�� üũ ǥ�� �̹����� ���� ����Ʈ
    [SerializeField]
    private List<Image> checkmarkImages;

    [SerializeField] private Button closeButton;

    private void Start()
    {
        // ����Ʈ�� �ִ� ��ư�鿡 Ŭ�� �����ʸ� �߰��մϴ�.
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i; // Ŭ���� ���� ������ ���� �ε��� ����
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseFriendsUI);
        }

        // ���� ���� �� �⺻ �ɼ��� �����մϴ�. (��: Medium)
        OnOptionSelected(1);
    }

    private void OnOptionSelected(int selectedIndex)
    {
        // ��� üũ ǥ�ø� ��Ȱ��ȭ�մϴ�.
        for (int i = 0; i < checkmarkImages.Count; i++)
        {
            checkmarkImages[i].enabled = false;
        }

        // ���õ� �ɼ��� üũ ǥ�ø� Ȱ��ȭ�մϴ�.
        checkmarkImages[selectedIndex].enabled = true; }

         public void CloseFriendsUI()
    {
        // �� ��ũ��Ʈ�� �پ��ִ� ���� ������Ʈ(ģ�� â �г�)�� ��Ȱ��ȭ�մϴ�.
        gameObject.SetActive(false);
    }

    // TODO: ���⿡ ���� �׷��� ����(ǰ�� ����)�� �����ϴ� �ڵ带 �߰��ϼ���.
    // ���� ���: QualitySettings.SetQualityLevel(selectedIndex, true);
}
