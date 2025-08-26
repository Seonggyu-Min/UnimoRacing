using UnityEngine;

public class MatchManager : MonoBehaviour
{
    // �̱��� ���ٿ� (UI���� MatchManager.Instance�� ȣ�� ����)
    public static MatchManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // �� �ٲ� ����
    }

    // ��Ī ���� (�ӽ�)
    public void StartMatch()
    {
        Debug.Log("[MatchManager] ��Ī ���� ��û (�ӽ� ����)");
    }

    // ��Ī ��� (�ӽ�)
    public void CancelMatch()
    {
        Debug.Log("[MatchManager] ��Ī ��� ��û (�ӽ� ����)");
    }

    public void ToggleReady()
    {
        Debug.Log("[MatchManager] Ready (�ӽ� ����)");
    }

    public void SelectMap()
    {
        Debug.Log("[MatchManager] �� ���� (�ӽ� ����)");
    }
    public void TryStartGame()
    {
        Debug.Log("[MatchManager] ���� ���� (�ӽ� ����)");
    }
}