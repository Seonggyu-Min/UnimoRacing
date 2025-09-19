using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class SOAddressablePreloader : MonoBehaviour
{
    public bool loadFromResources = true;
    public string kartSOPath = "SO/Unimo/Kart";
    public string charSOPath = "SO/Unimo/Character";
    public List<UnimoKartSO> kartSOs;
    public List<UnimoCharacterSO> characterSOs;

    private static bool _ran; // 프로세스 동안 1회만

    private async void Awake()
    {
        if (_ran) { Destroy(this); return; }
        _ran = true;
        DontDestroyOnLoad(gameObject); // 씬 갈아껴도 생존

        if (!YTW.Launcher.PatchGate.Task.IsCompleted) // 패치 후 시작 보장
            await YTW.Launcher.PatchGate.Task;

        await YTW.ResourceManager.Instance.EnsureInitializedAsync();

        var tasks = new List<Task>();
        if (loadFromResources)
        {
            var karts = Resources.LoadAll<UnimoKartSO>(kartSOPath);
            var chars = Resources.LoadAll<UnimoCharacterSO>(charSOPath);

            kartSOs = new List<UnimoKartSO>(karts);              // 리스트에 붙잡기
            characterSOs = new List<UnimoCharacterSO>(chars);

            // 언로드 방지
            foreach (var so in kartSOs) so.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            foreach (var so in characterSOs) so.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            foreach (var so in kartSOs) { tasks.Add(so.EnsureKartPrefabAsync()); tasks.Add(so.EnsureKartSpriteAsync()); }
            foreach (var so in characterSOs) { tasks.Add(so.EnsureCharacterPrefabAsync()); tasks.Add(so.EnsureCharacterSpriteAsync()); }
        }
        else
        {
            // 인스펙터에 넣어준 리스트 자체가 참조를 유지
            foreach (var so in kartSOs) if (so) { tasks.Add(so.EnsureKartPrefabAsync()); tasks.Add(so.EnsureKartSpriteAsync()); }
            foreach (var so in characterSOs) if (so) { tasks.Add(so.EnsureCharacterPrefabAsync()); tasks.Add(so.EnsureCharacterSpriteAsync()); }
        }

        await Task.WhenAll(tasks); //
    }
    
}
