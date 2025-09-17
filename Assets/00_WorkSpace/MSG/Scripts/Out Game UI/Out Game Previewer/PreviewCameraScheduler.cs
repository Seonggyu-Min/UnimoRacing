using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class PreviewCameraScheduler : MonoBehaviour
    {
        private class PreviewJob
        {
            public int Id;
            public Transform Target;
            public RenderTexture RT;
            public RawImage Raw;
            public float NextTime;
        }

        [SerializeField] private Camera _previewCam;
        [SerializeField][Range(1, 100)] private int _fps = 15;
        [SerializeField] private LayerMask _previewLayer;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.7f, -2f);
        [SerializeField] private float _fov = 60f;

        private float _interval;
        private Dictionary<int, PreviewJob> _jobs = new();


        private void Awake()
        {
            if (_previewCam == null)
                if (!TryGetComponent(out _previewCam))
                    Debug.LogWarning("[PreviewCameraScheduler] Camera를 찾을 수 없습니다.");

            _previewCam.enabled = false;
        }

        private void LateUpdate()
        {
            CalculateInterval();

            float now = Time.unscaledTime;

            foreach (var job in _jobs)
            {
                if (now >= job.Value.NextTime)
                {
                    RenderOne(job.Value);
                    job.Value.NextTime = now + _interval;
                }
            }
        }


        public void Register(int id, Transform target, RawImage raw, RenderTexture rt)
        {
            raw.texture = rt;
            if (_jobs.TryGetValue(id, out var job))
            {
                job.Target = target;
                job.RT = rt;
                job.Raw = raw;
                job.NextTime = 0f;
            }
            else
            {
                _jobs[id] = new PreviewJob { Id = id, Target = target, RT = rt, Raw = raw, NextTime = 0f };
            }
        }

        public void Unregister(int id) => _jobs.Remove(id);


        private void RenderOne(PreviewJob job)
        {
            if (job.Target == null || job.RT == null) return;

            _previewCam.cullingMask = _previewLayer;
            _previewCam.targetTexture = job.RT;

            _previewCam.transform.position = job.Target.position + _offset;
            _previewCam.fieldOfView = _fov;

            _previewCam.Render();
            _previewCam.targetTexture = null;
        }

        private void CalculateInterval()
        {
            _interval = 1f / _fps;
        }
    }
}
