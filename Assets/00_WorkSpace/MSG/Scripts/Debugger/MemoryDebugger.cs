using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;


namespace MSG
{
    public class MemoryDebugger : MonoBehaviour
    {
        [SerializeField] private bool _useDebugger = true;
        [SerializeField] private float interval = 0.1f;
        private float _nextTime;

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Update()
        {
            if (_useDebugger)
            {
                if (Time.realtimeSinceStartup >= _nextTime)
                {
                    LogMemory();
                    _nextTime = Time.realtimeSinceStartup + interval;
                }
            }
        }

        private void LogMemory()
        {
            long monoUsed = Profiler.GetMonoUsedSizeLong();
            long totalAlloc = Profiler.GetTotalAllocatedMemoryLong();
            long totalReserve = Profiler.GetTotalReservedMemoryLong();
            long tempAlloc = Profiler.GetTempAllocatorSize();

            Debug.Log($"[Memory] Mono: {ToMB(monoUsed)} MB, " +
                      $"Allocated: {ToMB(totalAlloc)} MB, " +
                      $"Reserved: {ToMB(totalReserve)} MB, " +
                      $"Temp: {ToMB(tempAlloc)} MB");
        }

        private string ToMB(long bytes) => (bytes / (1024f * 1024f)).ToString("F1");
    }
}
