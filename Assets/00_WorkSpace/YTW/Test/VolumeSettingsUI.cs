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

        // PlayerPrefs�� ����ϴ� Ű �� (AudioManager�� �����ؾ� ��)
        private const string MASTER_VOLUME_KEY = "MasterVolume";
        private const string BGM_VOLUME_KEY = "BGMVolume";
        private const string SFX_VOLUME_KEY = "SFXVolume";

        // �� ���� ������Ʈ(���� â �г�)�� Ȱ��ȭ�� ������ ȣ��˴ϴ�.
        private void OnEnable()
        {
            InitializeSliders();
        }

        // �����̴��� ���� PlayerPrefs�� ����� ������ �ʱ�ȭ�ϴ� �Լ�
        private void InitializeSliders()
        {
            // �� �����̴��� null�� �ƴ��� Ȯ���ϰ�, ����� ���� �ҷ��� value�� �Ҵ��մϴ�.
            // ����� ���� ������ �⺻������ 1f (100%)�� ����մϴ�.
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