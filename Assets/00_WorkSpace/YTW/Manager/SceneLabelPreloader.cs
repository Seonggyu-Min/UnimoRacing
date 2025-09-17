using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;


namespace YTW
{
    public class SceneLabelPreloader : MonoBehaviour
    {
        [SerializeField] List<string> labels = new() { "Lobby" };
        [SerializeField] bool preloadAssetsToMemory = false;

        private ResourceManager.PreloadTicket _ticket;

        private async void Start()
        {
            await ResourceManager.Instance.EnsureInitializedAsync();
            if (labels == null || labels.Count == 0) return;

            _ticket = await ResourceManager.Instance
                       .PreloadLabelsAsync(labels, preloadAssetsToMemory);
        }

        private void OnDestroy()
        {
            if (_ticket != null && ResourceManager.Instance != null)
                ResourceManager.Instance.ReleasePreload(_ticket);
            _ticket = null;

        }
    }
}
