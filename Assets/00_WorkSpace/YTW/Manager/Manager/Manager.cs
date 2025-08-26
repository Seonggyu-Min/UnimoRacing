using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YTW;

namespace YTW
{
    public class Manager : Singleton<Manager>
    {
        public static AudioManager Audio { get; private set; }
        public static SceneManager Scene { get; private set; }
        public static ResourceManager Resource { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            Audio = AudioManager.Instance;
            Scene = SceneManager.Instance;
            Resource = ResourceManager.Instance;
        }

        // UI �̺�Ʈ�� ���� ���� �Լ�
        public void SetMasterVolume(float value)
        {
            // UI �����̴��κ��� ���� ���� AudioManager�� ���� �Լ��� ����
            if (Audio != null)
                Audio.SetMasterVolume(value);
        }

        public void SetBgmVolume(float value)
        {
            if (Audio != null)
                Audio.SetBGMVolume(value);
        }

        public void SetSfxVolume(float value)
        {
            if (Audio != null)
                Audio.SetSFXVolume(value);
        }
    }

}
