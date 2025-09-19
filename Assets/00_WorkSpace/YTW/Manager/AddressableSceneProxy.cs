using System.Threading.Tasks;
using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YTW
{
    [DefaultExecutionOrder(-10000)]
    public class AddressableSceneProxy : MonoBehaviour
    {
        [SerializeField] private string addressablesSceneName;
        [SerializeField] private bool leaveRoomOnLoad = false; // �κ� ���Ͻö�� true

        private async void Start()
        {
            // ��ġ ������ ����
            if (!YTW.Launcher.PatchGate.Task.IsCompleted)
                await YTW.Launcher.PatchGate.Task;

            await YTW.ResourceManager.Instance.EnsureInitializedAsync();

            // Addressables �� �̱� �ε�
            await YTW.ResourceManager.Instance.LoadSceneAsync(addressablesSceneName, LoadSceneMode.Single);

            if (leaveRoomOnLoad && PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
        }
    }
}
