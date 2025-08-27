using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YTW;

// �� enum�� �����̴��� � ������ �������� �����մϴ�.
public enum VolumeType
{
    Master,
    BGM,
    SFX
}

// �� ��ũ��Ʈ�� �����̴� UI�� �ٿ� ���� ���� ����� �����մϴ�.
public class VolumeSliderUI : MonoBehaviour
{
    // �ν����Ϳ��� � ������ �������� ����
    [SerializeField] private VolumeType volumeType;

    // �����̴� UI ������Ʈ
    [SerializeField] private Slider volumeSlider;

    private void Awake()
    {
        // NullReferenceException�� �����ϱ� ���� �����̴� ������Ʈ�� �ִ��� Ȯ��
        if (volumeSlider == null)
        {
            volumeSlider = GetComponent<Slider>();
            if (volumeSlider == null)
            {
                Debug.LogError("[VolumeSliderUI] Slider ������Ʈ�� �Ҵ���� �ʾҽ��ϴ�!");
                return;
            }
        }

        // �����̴��� ���� ����� ������ OnVolumeChanged �Լ� ȣ��
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private IEnumerator Start()
    {
        // AudioManager�� �ʱ�ȭ�� ������ ��ٸ��ϴ�.
        yield return new WaitUntil(() => Manager.Audio != null && Manager.Audio.IsInitialized);

        // AudioManager �ʱ�ȭ�� �Ϸ�� �� �ʱ� ���� ����
        SetInitialVolume();
    }

    // �����̴��� ������ �� PlayerPrefs���� ����� ���� ������ �ʱ�ȭ�մϴ�.
    private void SetInitialVolume()
    {
        float savedVolume = 1f; // �⺻��
        switch (volumeType)
        {
            case VolumeType.Master:
                savedVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
                break;
            case VolumeType.BGM:
                savedVolume = PlayerPrefs.GetFloat("BGMVolume", 1f);
                break;
            case VolumeType.SFX:
                savedVolume = PlayerPrefs.GetFloat("SFXVolume", 1f);
                break;
        }
        volumeSlider.value = savedVolume;
    }

    // �����̴� ���� ����� �� ȣ��Ǵ� �Լ�
    private void OnVolumeChanged(float value)
    {
        // AudioManager�� �ν��Ͻ��� �ִ��� Ȯ��
        if (Manager.Audio == null || !Manager.Audio.IsInitialized)
        {
            Debug.LogWarning("[VolumeSliderUI] AudioManager�� �غ���� �ʾ� ������ ������ �� �����ϴ�.");
            return;
        }

        // ���õ� ���� ������ ���� AudioManager�� �ش� �Լ� ȣ��
        switch (volumeType)
        {
            case VolumeType.Master:
                Manager.Audio.SetMasterVolume(value);
                break;
            case VolumeType.BGM:
                Manager.Audio.SetBGMVolume(value);
                break;
            case VolumeType.SFX:
                Manager.Audio.SetSFXVolume(value);
                break;
        }
    }
}
