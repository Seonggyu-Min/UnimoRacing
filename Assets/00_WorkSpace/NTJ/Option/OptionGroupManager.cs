using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// PopupBase�� ��ӹ޾� �˾� ����� ����
public class OptionGroupManager : PopupBase
{
    // �ν����Ϳ��� UI ��ư���� �Ҵ��� ����Ʈ
    [SerializeField]
    private List<Button> optionButtons;

    // �� ��ư�� üũ ǥ�� �̹����� ���� ����Ʈ
    [SerializeField]
    private List<Image> checkmarkImages;

    // Start() ��� Awake()�� ����� �ʱ�ȭ ������ �˾��� Ȱ��ȭ�Ǳ� ���� ����
    private void Awake()
    {
        // ����Ʈ�� �ִ� ��ư�鿡 Ŭ�� �����ʸ� �߰��մϴ�.
        for (int i = 0; i < optionButtons.Count; i++)
        {
            int index = i; // Ŭ���� ���� ������ ���� �ε��� ����
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }
    }

    // �˾��� �� �� ȣ��Ǵ� �޼��� (PopupBase���� ���)
    public override void Open()
    {
        base.Open(); // PopupBase�� Open()�� ȣ���Ͽ� ���� ������Ʈ�� Ȱ��ȭ

        OnOptionSelected(1);
    }

    // �˾��� ���� �� ȣ��Ǵ� �޼��� (PopupBase���� ���)
    public override void Close()
    {
        base.Close(); // PopupBase�� Close()�� ȣ���Ͽ� ���� ������Ʈ�� ��Ȱ��ȭ
    }

    private void OnOptionSelected(int selectedIndex)
    {
        // ��� üũ ǥ�ø� ��Ȱ��ȭ�մϴ�.
        for (int i = 0; i < checkmarkImages.Count; i++)
        {
            // ����Ʈ �ε��� ���� Ȯ��
            if (i < checkmarkImages.Count)
            {
                checkmarkImages[i].enabled = false;
            }
        }

        // ���õ� �ɼ��� üũ ǥ�ø� Ȱ��ȭ�մϴ�.
        if (selectedIndex >= 0 && selectedIndex < checkmarkImages.Count)
        {
            checkmarkImages[selectedIndex].enabled = true;
        }
    }

    // TODO: ���⿡ ���� �׷��� ����(ǰ�� ����)�� �����ϴ� �ڵ带 �߰��ϼ���.
    // ���� ���: QualitySettings.SetQualityLevel(selectedIndex, true);
}
