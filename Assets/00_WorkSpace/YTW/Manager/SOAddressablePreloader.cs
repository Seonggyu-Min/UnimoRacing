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
        // �̹� �ѹ� �������� ������ ���� (�� ���ε�/��ġ �� ����� ���)
        if (_ran) { Destroy(this); return; }
        _ran = true;

        // ��ġ/�ٿ�ε尡 ���� �ڿ��� �ε� ���� (LoadRefAsync�� PatchGate�� �� ��ٸ��Ƿ�)
        if (!YTW.Launcher.PatchGate.Task.IsCompleted)
            await YTW.Launcher.PatchGate.Task;

        await YTW.ResourceManager.Instance.EnsureInitializedAsync();

        var tasks = new List<Task>();
        if (loadFromResources)
        {
            var karts = Resources.LoadAll<UnimoKartSO>(kartSOPath);
            Debug.Log($"[Preloader] Kart SO count = {karts.Length} at '{kartSOPath}'");
            foreach (var so in Resources.LoadAll<UnimoKartSO>(kartSOPath))
            {
                tasks.Add(so.EnsureKartPrefabAsync());
                tasks.Add(so.EnsureKartSpriteAsync());
            }
            var chars = Resources.LoadAll<UnimoCharacterSO>(charSOPath);
            Debug.Log($"[Preloader] Char SO count = {chars.Length} at '{charSOPath}'");
            foreach (var so in Resources.LoadAll<UnimoCharacterSO>(charSOPath))
            {
                tasks.Add(so.EnsureCharacterPrefabAsync());
                tasks.Add(so.EnsureCharacterSpriteAsync());
            }
        }
        else
        {
            foreach (var so in kartSOs) if (so) { tasks.Add(so.EnsureKartPrefabAsync()); tasks.Add(so.EnsureKartSpriteAsync()); }
            foreach (var so in characterSOs) if (so) { tasks.Add(so.EnsureCharacterPrefabAsync()); tasks.Add(so.EnsureCharacterSpriteAsync()); }
        }
        await Task.WhenAll(tasks);
    }
}
