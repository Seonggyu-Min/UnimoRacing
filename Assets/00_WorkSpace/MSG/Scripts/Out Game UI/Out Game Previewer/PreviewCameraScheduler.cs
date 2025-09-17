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
        [SerializeField][Range(0, 100)] private int _fps = 15;
        [SerializeField] private LayerMask _previewLayer;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.7f, -2f);
        [SerializeField] private float _fov = 60f;

        private float _interval;
        private List<PreviewJob> _jobs = new();


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
                if (now >= job.NextTime)
                {
                    RenderOne(job);
                    job.NextTime = now + _interval;
                }
            }
        }


        public void Register(int id, Transform target, RawImage raw, RenderTexture rt)
        {
            raw.texture = rt;
            _jobs.Add(new PreviewJob
            {
                Id = id,
                Target = target,
                Raw = raw,
                RT = rt,
                NextTime = 0f
            });
        }

        public void Unregister(int id)
        {
            _jobs.RemoveAll(j => j.Id == id);
        }


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
