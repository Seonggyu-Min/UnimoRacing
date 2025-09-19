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
        [SerializeField] private bool leaveRoomOnLoad = false; // 로비 프록시라면 true

        private async void Start()
        {
            // 패치 끝나길 보장
            if (!YTW.Launcher.PatchGate.Task.IsCompleted)
                await YTW.Launcher.PatchGate.Task;

            await YTW.ResourceManager.Instance.EnsureInitializedAsync();

            // Addressables 씬 싱글 로드
            await YTW.ResourceManager.Instance.LoadSceneAsync(addressablesSceneName, LoadSceneMode.Single);

            if (leaveRoomOnLoad && PhotonNetwork.InRoom)
                PhotonNetwork.LeaveRoom();
        }
    }
}
