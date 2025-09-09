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

        private bool _hooked;

        private void OnEnable()
        {
            // 오디오 매니저가 생성/초기화될 때까지 기다렸다가 UI와 연결
            StartCoroutine(InitWhenAudioReady());
            ResetToDefault();
        }

        private void OnDisable()
        {
            UnhookEvents();
        }

        private IEnumerator InitWhenAudioReady()
        {
            // Manager.Audio == null 이거나 초기화 전이면 대기
            while (Manager.Audio == null || !Manager.Audio.IsInitialized)
                yield return null;

            // 초기 슬라이더 값 동기화 (AudioManager가 PlayerPrefs에 저장한 값을 사용)
            // 키 이름은 AudioManager와 동일해야 함
            if (_masterSlider) _masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 1f));
            if (_bgmSlider) _bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("BGMVolume", 1f));
            if (_sfxSlider) _sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", 1f));

            HookEvents();
        }

        private void HookEvents()
        {
            if (_hooked || Manager.Audio == null) return;

            // 슬라이더 변화 AudioManager API 호출
            if (_masterSlider) _masterSlider.onValueChanged.AddListener(Manager.Audio.SetMasterVolume);
            if (_bgmSlider) _bgmSlider.onValueChanged.AddListener(Manager.Audio.SetBGMVolume);
            if (_sfxSlider) _sfxSlider.onValueChanged.AddListener(Manager.Audio.SetSFXVolume);

            _hooked = true;
        }

        private void UnhookEvents()
        {
            if (!_hooked) return;

            // 같은 델리게이트로 제거해야 하므로 Manager.Audio가 살아있을 때만 제거
            if (Manager.Audio != null)
            {
                if (_masterSlider) _masterSlider.onValueChanged.RemoveListener(Manager.Audio.SetMasterVolume);
                if (_bgmSlider) _bgmSlider.onValueChanged.RemoveListener(Manager.Audio.SetBGMVolume);
                if (_sfxSlider) _sfxSlider.onValueChanged.RemoveListener(Manager.Audio.SetSFXVolume);
            }
            else
            {
                // 아주 드문 케이스로 Manager.Audio가 파괴된 상태라면, 중복 연결 방지용으로 전체 제거
                if (_masterSlider) _masterSlider.onValueChanged.RemoveAllListeners();
                if (_bgmSlider) _bgmSlider.onValueChanged.RemoveAllListeners();
                if (_sfxSlider) _sfxSlider.onValueChanged.RemoveAllListeners();
            }

            _hooked = false;
        }

        // 기본값으로 초기화
        public void ResetToDefault()
        {
            if (_masterSlider) _masterSlider.value = 1f;
            if (_bgmSlider) _bgmSlider.value = 1f;
            if (_sfxSlider) _sfxSlider.value = 1f;
        }
    }
}