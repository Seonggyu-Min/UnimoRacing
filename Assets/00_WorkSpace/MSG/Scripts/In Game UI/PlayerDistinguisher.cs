using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class PlayerDistinguisher : MonoBehaviour
    {
        [SerializeField] private MeshRenderer _meshRenderer;
        [SerializeField] private PlayerRaceData _raceData;

        [SerializeField] private float _height;
        [SerializeField] private float _duration;
        [SerializeField] private Ease _ease = Ease.InOutSine;

        private void OnEnable()
        {
            InGameManager.Instance.OnStateChanged += OnStateChanged;
            _meshRenderer.enabled = false;
        }

        private void OnDisable()
        {
            if (InGameManager.GetInstance != null)
            {
                InGameManager.Instance.OnStateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(RaceState state)
        {
            if (state == RaceState.Racing)
            {
                if (_raceData != null)
                {
                    if (_raceData.View.IsMine)
                    {
                        _meshRenderer.enabled = true;
                    }
                }
            }
        }
    }
}
