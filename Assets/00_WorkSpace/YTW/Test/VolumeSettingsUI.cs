using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YTW
{
    // ���� ���� �гο� �߰�
    public class VolumeSettingsUI : MonoBehaviour
    {
        [Header("���� �����̴�")]
        [SerializeField] private Slider _masterSlider;
        [SerializeField] private Slider _bgmSlider;
        [SerializeField] private Slider _sfxSlider;

        private bool _hooked;

        private void OnEnable()
        {
            // ����� �Ŵ����� ����/�ʱ�ȭ�� ������ ��ٷȴٰ� UI�� ����
            StartCoroutine(InitWhenAudioReady());
            ResetToDefault();
        }

        private void OnDisable()
        {
            UnhookEvents();
        }

        private IEnumerator InitWhenAudioReady()
        {
            // Manager.Audio == null �̰ų� �ʱ�ȭ ���̸� ���
            while (Manager.Audio == null || !Manager.Audio.IsInitialized)
                yield return null;

            // �ʱ� �����̴� �� ����ȭ (AudioManager�� PlayerPrefs�� ������ ���� ���)
            // Ű �̸��� AudioManager�� �����ؾ� ��
            if (_masterSlider) _masterSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 1f));
            if (_bgmSlider) _bgmSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("BGMVolume", 1f));
            if (_sfxSlider) _sfxSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("SFXVolume", 1f));

            HookEvents();
        }

        private void HookEvents()
        {
            if (_hooked || Manager.Audio == null) return;

            // �����̴� ��ȭ AudioManager API ȣ��
            if (_masterSlider) _masterSlider.onValueChanged.AddListener(Manager.Audio.SetMasterVolume);
            if (_bgmSlider) _bgmSlider.onValueChanged.AddListener(Manager.Audio.SetBGMVolume);
            if (_sfxSlider) _sfxSlider.onValueChanged.AddListener(Manager.Audio.SetSFXVolume);

            _hooked = true;
        }

        private void UnhookEvents()
        {
            if (!_hooked) return;

            // ���� ��������Ʈ�� �����ؾ� �ϹǷ� Manager.Audio�� ������� ���� ����
            if (Manager.Audio != null)
            {
                if (_masterSlider) _masterSlider.onValueChanged.RemoveListener(Manager.Audio.SetMasterVolume);
                if (_bgmSlider) _bgmSlider.onValueChanged.RemoveListener(Manager.Audio.SetBGMVolume);
                if (_sfxSlider) _sfxSlider.onValueChanged.RemoveListener(Manager.Audio.SetSFXVolume);
            }
            else
            {
                // ���� �幮 ���̽��� Manager.Audio�� �ı��� ���¶��, �ߺ� ���� ���������� ��ü ����
                if (_masterSlider) _masterSlider.onValueChanged.RemoveAllListeners();
                if (_bgmSlider) _bgmSlider.onValueChanged.RemoveAllListeners();
                if (_sfxSlider) _sfxSlider.onValueChanged.RemoveAllListeners();
            }

            _hooked = false;
        }

        // �⺻������ �ʱ�ȭ
        public void ResetToDefault()
        {
            if (_masterSlider) _masterSlider.value = 1f;
            if (_bgmSlider) _bgmSlider.value = 1f;
            if (_sfxSlider) _sfxSlider.value = 1f;
        }
    }
}