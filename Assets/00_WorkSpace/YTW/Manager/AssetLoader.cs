using UnityEngine;

namespace YTW
{
    public class AssetLoader : MonoBehaviour
    {
        //public static async Task<GameObject> InstantiateCharacterAsync(UnimoCharacterSO so, Transform parent)
        //{
        //    if (so == null || so.model == null) return null;

        //    var key = so.model.RuntimeKey.ToString();
        //    var go = await ResourceManager.Instance.InstantiateAsync(key, Vector3.zero, Quaternion.identity);
        //    if (go != null && parent != null) go.transform.SetParent(parent, false);
        //    return go;
        //}

        //public static async Task<GameObject> InstantiateKartAsync(UnimoKartSO so, Transform parent)
        //{
        //    if (so == null || so.model == null) return null;

        //    var key = so.model.RuntimeKey.ToString();
        //    var go = await ResourceManager.Instance.InstantiateAsync(key, Vector3.zero, Quaternion.identity);
        //    if (go != null && parent != null) go.transform.SetParent(parent, false);
        //    return go;
        //}

        //public static async Task<Sprite> LoadPortraitAsync(UnimoCharacterSO so)
        //{
        //    if (so == null || so.portrait == null) return null;
        //    var key = so.portrait.RuntimeKey.ToString();
        //    return await ResourceManager.Instance.LoadAsync<Sprite>(key);
        //}

        //public static async Task<Sprite> LoadKartIconAsync(UnimoKartSO so)
        //{
        //    if (so == null || so.icon == null) return null;
        //    var key = so.icon.RuntimeKey.ToString();
        //    return await ResourceManager.Instance.LoadAsync<Sprite>(key);
    }
}

