using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YTW;

// 이 enum은 슬라이더가 어떤 볼륨을 조절할지 지정합니다.
public enum VolumeType
{
    Master,
    BGM,
    SFX
}

// 이 스크립트를 슬라이더 UI에 붙여 볼륨 조절 기능을 연결합니다.
public class VolumeSliderUI : MonoBehaviour
{
    // 인스펙터에서 어떤 볼륨을 조절할지 선택
    [SerializeField] private VolumeType volumeType;

    // 슬라이더 UI 컴포넌트
    [SerializeField] private Slider volumeSlider;

    private void Awake()
    {
        // NullReferenceException을 방지하기 위해 슬라이더 컴포넌트가 있는지 확인
        if (volumeSlider == null)
        {
            volumeSlider = GetComponent<Slider>();
            if (volumeSlider == null)
            {
                Debug.LogError("[VolumeSliderUI] Slider 컴포넌트가 할당되지 않았습니다!");
                return;
            }
        }

        // 슬라이더의 값이 변경될 때마다 OnVolumeChanged 함수 호출
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private IEnumerator Start()
    {
        // AudioManager가 초기화될 때까지 기다립니다.
        yield return new WaitUntil(() => Manager.Audio != null && Manager.Audio.IsInitialized);

        // AudioManager 초기화가 완료된 후 초기 볼륨 설정
        SetInitialVolume();
    }

    // 슬라이더가 생성될 때 PlayerPrefs에서 저장된 볼륨 값으로 초기화합니다.
    private void SetInitialVolume()
    {
        float savedVolume = 1f; // 기본값
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

    // 슬라이더 값이 변경될 때 호출되는 함수
    private void OnVolumeChanged(float value)
    {
        // AudioManager의 인스턴스가 있는지 확인
        if (Manager.Audio == null || !Manager.Audio.IsInitialized)
        {
            Debug.LogWarning("[VolumeSliderUI] AudioManager가 준비되지 않아 볼륨을 조절할 수 없습니다.");
            return;
        }

        // 선택된 볼륨 종류에 따라 AudioManager의 해당 함수 호출
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
