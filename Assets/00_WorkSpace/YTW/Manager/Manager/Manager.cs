using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YTW;

public class Manager : Singleton<Manager>
{
    public static AudioManager Audio { get; private set; }
    public static SceneManager Scene { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        Audio = AudioManager.Instance;
        Scene = SceneManager.Instance;
    }

    // UI 이벤트를 위한 연결 함수
    public void SetMasterVolume(float value)
    {
        // UI 슬라이더로부터 받은 값을 AudioManager의 실제 함수로 전달
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
