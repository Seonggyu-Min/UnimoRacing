using UnityEngine;
using UnityEngine.InputSystem;
using YTW; 

public class AudioTester : MonoBehaviour
{
    // 테스트할 BGM 목록
    private string[] _bgmNames = { "BGM_Lobby", "BGM_Game1", "BGM_Game2" };
    private int _currentBgmIndex = 0;

    [Header("UI 패널")]
    [SerializeField] private GameObject settingsPanel;
    void Start()
    {
        // Manager가 초기화될 시간을 주기 위해 Invoke를 사용
        Invoke(nameof(PlayInitialBGM), 0.1f);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    void PlayInitialBGM()
    {
        // 게임 시작 시 첫 번째 BGM을 재생
        Debug.Log($"첫 BGM 재생: {_bgmNames[_currentBgmIndex]}");
        Manager.Audio.PlayBGM(_bgmNames[_currentBgmIndex]);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Z키를 누르면 이전 BGM으로 변경
        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            _currentBgmIndex--;
            if (_currentBgmIndex < 0)
            {
                _currentBgmIndex = _bgmNames.Length - 1;
            }
            Debug.Log($"BGM 변경: {_bgmNames[_currentBgmIndex]}");
            Manager.Audio.PlayBGM(_bgmNames[_currentBgmIndex]);
        }

        // X키를 누르면 다음 BGM으로 변경
        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            _currentBgmIndex++;
            if (_currentBgmIndex >= _bgmNames.Length)
            {
                _currentBgmIndex = 0;
            }
            Debug.Log($"BGM 변경: {_bgmNames[_currentBgmIndex]}");
            Manager.Audio.PlayBGM(_bgmNames[_currentBgmIndex]);
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame && settingsPanel != null)
        {
            settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        // 마우스 입력이 있는지 확인
        if (Mouse.current == null) return;

        // 마우스 왼쪽 버튼을 클릭하면 SFX 재생
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Debug.Log("SFX 재생: TestSFX");
            Manager.Audio.PlaySFX("TestSFX");
        }
    }
}
