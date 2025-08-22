using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "NewAudioData", menuName = "Audio/Audio Data")]
public class AudioData : ScriptableObject
{
    [Header("���� ����")]
    public string ClipName;
    public AudioClip Clip;

    [Header("���� ����")]
    [Range(0f, 1f)] public float Volume = 1.0f;
    [Range(0.1f, 3f)] public float Pitch = 1.0f;
    public bool Loop = false;
    public AudioMixerGroup MixerGroup;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // ClipName�� ��������� ���� ���ϸ��� �⺻������ ���
        if (string.IsNullOrWhiteSpace(ClipName))
        {
            ClipName = name;
        }

        if (Clip == null)
        {
            Debug.LogWarning($"[AudioData] '{ClipName}'�� AudioClip�� �Ҵ���� �ʾҽ��ϴ�.", this);
        }
    }
#endif
}
