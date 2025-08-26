using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YTW
{
    // 볼륨 세팅 패널에 추가
    public class VolumeSettingsUI : MonoBehaviour
    {
        [Header("볼륨 슬라이더")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        // PlayerPrefs에 사용하는 키 값 (AudioManager와 동일해야 함)
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";

        // 이 게임 오브젝트(설정 창 패널)가 활성화될 때마다 호출됩니다.
        private void OnEnable()
        {
            InitializeSliders();
        }

        // 슬라이더의 값을 PlayerPrefs에 저장된 값으로 초기화하는 함수
        private void InitializeSliders()
        {
            // 각 슬라이더가 null이 아닌지 확인하고, 저장된 값을 불러와 value에 할당합니다.
            // 저장된 값이 없으면 기본값으로 1f (100%)를 사용합니다.
            if (_masterSlider != null)
            {
                _masterSlider.value = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1f);
            }

            if (_bgmSlider != null)
            {
                _bgmSlider.value = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 1f);
            }

            if (_sfxSlider != null)
            {
                _sfxSlider.value = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1f);
            }
        }
    }
}