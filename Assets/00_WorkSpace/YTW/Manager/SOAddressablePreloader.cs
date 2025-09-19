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

    private static bool _ran; // ���μ��� ���� 1ȸ��

    private async void Awake()
    {
        if (_ran) { Destroy(this); return; }
        _ran = true;
        DontDestroyOnLoad(gameObject); // �� ���Ʋ��� ����

        if (!YTW.Launcher.PatchGate.Task.IsCompleted) // ��ġ �� ���� ����
            await YTW.Launcher.PatchGate.Task;

        await YTW.ResourceManager.Instance.EnsureInitializedAsync();

        var tasks = new List<Task>();
        if (loadFromResources)
        {
            var karts = Resources.LoadAll<UnimoKartSO>(kartSOPath);
            var chars = Resources.LoadAll<UnimoCharacterSO>(charSOPath);

            kartSOs = new List<UnimoKartSO>(karts);              // ����Ʈ�� �����
            characterSOs = new List<UnimoCharacterSO>(chars);

            // ��ε� ����
            foreach (var so in kartSOs) so.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            foreach (var so in characterSOs) so.hideFlags |= HideFlags.DontUnloadUnusedAsset;

            foreach (var so in kartSOs) { tasks.Add(so.EnsureKartPrefabAsync()); tasks.Add(so.EnsureKartSpriteAsync()); }
            foreach (var so in characterSOs) { tasks.Add(so.EnsureCharacterPrefabAsync()); tasks.Add(so.EnsureCharacterSpriteAsync()); }
        }
        else
        {
            // �ν����Ϳ� �־��� ����Ʈ ��ü�� ������ ����
            foreach (var so in kartSOs) if (so) { tasks.Add(so.EnsureKartPrefabAsync()); tasks.Add(so.EnsureKartSpriteAsync()); }
            foreach (var so in characterSOs) if (so) { tasks.Add(so.EnsureCharacterPrefabAsync()); tasks.Add(so.EnsureCharacterSpriteAsync()); }
        }

        await Task.WhenAll(tasks); //
    }
    
}
