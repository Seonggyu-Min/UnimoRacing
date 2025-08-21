using UnityEngine;
using UnityEngine.InputSystem;
using YTW; 

public class AudioTester : MonoBehaviour
{
    // �׽�Ʈ�� BGM ���
    private string[] _bgmNames = { "BGM_Lobby", "BGM_Game1", "BGM_Game2" };
    private int _currentBgmIndex = 0;

    [Header("UI �г�")]
    [SerializeField] private GameObject settingsPanel;
    void Start()
    {
        // Manager�� �ʱ�ȭ�� �ð��� �ֱ� ���� Invoke�� ���
        Invoke(nameof(PlayInitialBGM), 0.1f);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void PlayInitialBGM()
    {
        // ���� ���� �� ù ��° BGM�� ���
        Debug.Log($"ù BGM ���: {_bgmNames[_currentBgmIndex]}");
        Manager.Audio.PlayBGM(_bgmNames[_currentBgmIndex]);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // ZŰ�� ������ ���� BGM���� ����
        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            _currentBgmIndex--;
            if (_currentBgmIndex < 0)
            {
                _currentBgmIndex = _bgmNames.Length - 1;
            }
            Debug.Log($"BGM ����: {_bgmNames[_currentBgmIndex]}");
            Manager.Audio.PlayBGM(_bgmNames[_currentBgmIndex]);
        }

        // XŰ�� ������ ���� BGM���� ����
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            _currentBgmIndex++;
            if (_currentBgmIndex >= _bgmNames.Length)
            {
                _currentBgmIndex = 0;
            }
            Debug.Log($"BGM ����: {_bgmNames[_currentBgmIndex]}");
            Manager.Audio.PlayBGM(_bgmNames[_currentBgmIndex]);
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        // ���콺 �Է��� �ִ��� Ȯ��
        if (Mouse.current == null) return;

        // ���콺 ���� ��ư�� Ŭ���ϸ� SFX ���
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("SFX ���: TestSFX");
            Manager.Audio.PlaySFX("TestSFX");
        }
    }
}
