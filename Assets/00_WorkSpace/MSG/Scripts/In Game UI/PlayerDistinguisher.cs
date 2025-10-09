using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PlayerDistinguisher : MonoBehaviour
    {
        [SerializeField] private PlayerRaceData _raceData;

        [SerializeField] private float _height;
        [SerializeField] private float _duration;
        [SerializeField] private Ease _ease = Ease.InOutSine;

        private void OnEnable()
        {
            if (_raceData != null)
            {
                if (_raceData.View.IsMine)
                {
                    gameObject.SetActive(true);

                    // DoTween y포지션이 계속 변해서 아래처럼 쓰면 현재는 안됨
                    //transform.DOMoveY(transform.position.y + _height, _duration)
                    //    .SetEase(_ease)
                    //    .SetLoops(-1, LoopType.Yoyo);
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }
    }
}
