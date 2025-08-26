using UnityEngine;

public class MatchManager : MonoBehaviour
{
    // 싱글톤 접근용 (UI에서 MatchManager.Instance로 호출 가능)
    public static MatchManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬 바뀌어도 유지
    }

    // 매칭 시작 (임시)
    public void StartMatch()
    {
        Debug.Log("[MatchManager] 매칭 시작 요청 (임시 동작)");
    }

    // 매칭 취소 (임시)
    public void CancelMatch()
    {
        Debug.Log("[MatchManager] 매칭 취소 요청 (임시 동작)");
    }

    public void ToggleReady()
    {
        Debug.Log("[MatchManager] Ready (임시 동작)");
    }

    public void SelectMap()
    {
        Debug.Log("[MatchManager] 맵 선택 (임시 동작)");
    }
    public void TryStartGame()
    {
        Debug.Log("[MatchManager] 게임 시작 (임시 동작)");
    }
}