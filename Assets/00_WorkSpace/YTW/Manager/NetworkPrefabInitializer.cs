using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace YTW
{
    [DefaultExecutionOrder(-500)]
    public class NetworkPrefabInitializer : MonoBehaviour
    {
        [SerializeField] private string _networkPrefabLabel = "NetworkPrefab";
        private static bool _done;

        private async void Awake()
        {
            if (_done) { Destroy(gameObject); return; }
            _done = true;
            DontDestroyOnLoad(gameObject);

            await NetworkAssetLoader.Instance.InitializeAndPreloadAsync(_networkPrefabLabel);
            Debug.Log("[NetworkPrefabInitializer] 에셋 준비 완료.");
        }
    }
}
