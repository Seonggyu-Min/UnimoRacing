using System.Collections;
using UnityEngine;

public class UnimoRaceAnimationController : MonoBehaviour
{
    private bool _isSetup = false;

    private UnimoKartAniCtrl _kartAniCtrl;
    private UnimoCharacterAniCtrl _characterAniCtrl;

    [Header("character trigger loop time")]
    [SerializeField] private float _blinkLoopTime;
    [SerializeField] private float _shake1LoopTime;
    [SerializeField] private float _shake2LoopTime;
    private float _blinkDelayStartTime;
    private float _shake1DelayStartTime;
    private float _shake2DelayStartTime;

    private Coroutine _coroutine;

    public bool IsSetup => _isSetup;

    public void Setup()
    {
        _kartAniCtrl = GetComponentInChildren<UnimoKartAniCtrl>();
        _characterAniCtrl = GetComponentInChildren<UnimoCharacterAniCtrl>();
        
        PlayBaseAni();
        _isSetup = true;
    }

    // TODO: 애니메이션 재생 관련 아이템이나 등등 관련해서 처리 필요하면 진행하기
    public IEnumerator CO_BaseLoopAni()
    {
        _blinkDelayStartTime = Random.Range(1.0f, _blinkLoopTime); ;
        _shake1DelayStartTime = Random.Range(1.0f, _shake1LoopTime);
        _shake2DelayStartTime = Random.Range(1.0f, _shake2LoopTime);

        float dt= 0.0f;
        while (true)
        {
            dt = Time.deltaTime;
            _blinkDelayStartTime -= dt;
            _shake1DelayStartTime -= dt;
            _shake2DelayStartTime -= dt;

            if(_blinkDelayStartTime <= 0)
            {
                _blinkDelayStartTime = Random.Range(1.0f, _blinkLoopTime);
                _characterAniCtrl.SetTriggerBlink();
            }

            if (_shake1DelayStartTime <= 0)
            {
                _shake1DelayStartTime = Random.Range(1.0f, _shake1LoopTime);
                _characterAniCtrl.SetTriggerShake1();
            }

            if (_shake2DelayStartTime <= 0)
            {
                _shake2DelayStartTime = Random.Range(1.0f, _shake2LoopTime);
                _characterAniCtrl.SetTriggerShake2();
            }

            yield return null;
        }
    }

    private void PlayBaseAni()
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        _coroutine = StartCoroutine(CO_BaseLoopAni());
        PlayIdleAni();
    }

    public void PlayIdleAni()
    {
        _kartAniCtrl.SetBoolIsStun(false);
        _kartAniCtrl.SetBoolIsMoving(false);
        _kartAniCtrl.SetFloatMovesync();
    }

    public void PlayMoveAni()
    {
        _kartAniCtrl.SetBoolIsStun(false);
        _kartAniCtrl.SetBoolIsMoving(true);
        _kartAniCtrl.SetFloatMovesync();
    }

}