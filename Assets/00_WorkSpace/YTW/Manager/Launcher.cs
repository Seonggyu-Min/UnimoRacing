using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UI;

namespace YTW
{
    [DefaultExecutionOrder(-10000)]
    public class Launcher : MonoBehaviour
    {
        [Header("Top UI")]
        [SerializeField] private GameObject topPanel;

        [Header("UI")]
        [SerializeField] TextMeshProUGUI statusText;
        [SerializeField] Slider progressBar;
        [SerializeField] Button startButton;

        [Header("Update Panel")]
        [SerializeField] GameObject updateConfirmPanel;
        [SerializeField] TextMeshProUGUI updateInfoText;
        [SerializeField] Button patchButton;

        [Header("Restart Panel")]
        [SerializeField] private GameObject restartConfirmPanel;
        [SerializeField] private TextMeshProUGUI restartInfoText;
        [SerializeField] private Button restartYesButton;

        [Header("Patch Labels")]
        [SerializeField] List<string> patchLabels = new() { "Remote" };

        [Header("Debug/Options")]
        [SerializeField] bool enableVerboseLogs = true;
        [SerializeField] bool autoClearCacheForTest = false;


        [Header("Progress Weights (pre-patch)")]
        [SerializeField] float weightInit = 0.2f;      // EnsureInitializedAsync
        [SerializeField] float weightCatalog = 0.3f;   // TryUpdateCatalogsAsync
        [SerializeField] float weightSizing = 0.5f;    // LoadLocations + GetDownloadSize

        [Header("UX")]
        [SerializeField] private float minTopPanelVisibleSeconds = 1.0f; // 최소 노출 시간
        [SerializeField] private float finalFillHoldSeconds = 0.5f; // 100% 후 잠깐 홀드
        [SerializeField] private float progressAnimSeconds = 0.5f;  // 진행바 애니메이션 시간

        private float _topPanelShownAt = -1f;
        private TaskCompletionSource<bool> _progressTcs;
        private float _cumProgress = 0f;
        Coroutine _progressCo;
        public static TaskCompletionSource<bool> PatchGate = new();


        async void Start()
        {
            InitializeUIOnStart();

            try
            {
                Log("Launcher Start - Addressables 초기화 시작");

                // 리소스/어드레서블 시스템 초기화
                await RunTask("리소스 시스템 준비 중...", _cumProgress + weightInit, Manager.Resource.EnsureInitializedAsync());
                _cumProgress += weightInit;

                // 카탈로그 체크만
                if (statusText) statusText.text = "최신 버전 확인 중...";
                bool needUpdate = await CheckCatalogUpdatesNoApplyAsync();
                StartSmoothProgress(Mathf.Min(_cumProgress + weightCatalog, 0.99f));
                _cumProgress = Mathf.Min(_cumProgress + weightCatalog, 0.99f);

                var (locs, sizeBytes) = await GetPatchTargetsAndSizeAsync();
                Debug.Log($"[Patch] locations={locs?.Count ?? 0}, sizeBytes={sizeBytes}");

                if (needUpdate || sizeBytes > 0)
                {
                    // 업데이트 있음: 오디오 초기화하지 않음. 구버전/무음 상태 유지
                    ShowUpdateConfirmPanel(sizeBytes, locs, catalogsUpdated: needUpdate);
                }
                else
                {
                    // 업데이트 없음: 오디오 초기화 후 런처 종료
                    await AfterPatchInitAndStartAsync();
                }
            }
            catch (Exception ex)
            {
                LogError($"Launcher Start 예외: {ex}");
                if (statusText) statusText.text = "오프라인으로 시작합니다";
                await AfterPatchInitAndStartAsync();
            }
            
        }

        // 시작 시 UI 기본 상태 설정
        private void InitializeUIOnStart()
        {
            if (startButton) startButton.gameObject.SetActive(false);
            if (updateConfirmPanel) updateConfirmPanel.SetActive(false);
            if (restartConfirmPanel) restartConfirmPanel.SetActive(false);

            if (topPanel)
            {
                topPanel.SetActive(true);                   
                _topPanelShownAt = Time.realtimeSinceStartup; 
            }


            if (progressBar)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.value = 0f;
            }
            if (statusText) statusText.text = "초기화 중...";
        }

        // 상태 텍스트 + 비동기 작업 + 목표 진행치(0~1)로 스무스 이동
        private async Task RunTask(string status, float targetProgress, Task task)
        {
            if (statusText != null) statusText.text = status;
            try
            {
                await task;
            }
            catch (Exception e)
            {
                LogWarning($"RunTask 예외: {e}");
            }
            StartSmoothProgress(targetProgress);
            await Task.Yield();
        }

        private async Task<(IList<IResourceLocation> locations, long sizeBytes)> GetPatchTargetsAndSizeAsync()
        {
            // 라벨 > 로케이션
            var locations = await BuildLocationsFromLabelsAsync(patchLabels);

            // (테스트 옵션) 캐시 삭제를 먼저 해야 실제 다운로드 필요 용량이 반영됨
            if (autoClearCacheForTest && locations.Count > 0)
            {
                Log("autoClearCacheForTest: ClearDependencyCacheAsync(locations)");
                var clearH = Addressables.ClearDependencyCacheAsync(locations, true);
                await clearH.Task;
                SafeRelease(clearH);
            }

            // 다운로드 필요 용량 계산
            long needBytes = 0;
            if (locations.Count > 0)
            {
                var sizeH = Addressables.GetDownloadSizeAsync(locations);
                await sizeH.Task;
                if (sizeH.IsValid() && sizeH.Status == AsyncOperationStatus.Succeeded)
                    needBytes = sizeH.Result;
                SafeRelease(sizeH);
            }
            return (locations, needBytes);
        }

        private async Task<bool> CheckCatalogUpdatesNoApplyAsync()
        {
            Log("CheckForCatalogUpdates (no apply)");
            var checkH = Addressables.CheckForCatalogUpdates(false);
            await checkH.Task;

            bool needUpdate =
                checkH.IsValid() &&
                checkH.Status == AsyncOperationStatus.Succeeded &&
                checkH.Result != null &&
                checkH.Result.Count > 0;

            if (needUpdate)
                Log($"카탈로그 업데이트 필요: {checkH.Result.Count}");
            else
                Log("카탈로그 변경 없음");

            SafeRelease(checkH);
            return needUpdate;
        }

        // 라벨 → 로케이션
        private async Task<IList<IResourceLocation>> BuildLocationsFromLabelsAsync(IEnumerable<string> labels)
        {
            if (labels == null || !labels.Any())
                return Array.Empty<IResourceLocation>();

            var locH = Addressables.LoadResourceLocationsAsync(labels, Addressables.MergeMode.Union);
            await locH.Task;

            if (!locH.IsValid() || locH.Status != AsyncOperationStatus.Succeeded || locH.Result == null)
            {
                SafeRelease(locH);
                return Array.Empty<IResourceLocation>();
            }

            var result = locH.Result; // 결과 복사 후 핸들만 해제
            Addressables.Release(locH);
            return result;
        }

        // UI 및 다운로드 처리
        private void ShowUpdateConfirmPanel(long sizeBytes, IList<IResourceLocation> locations, bool catalogsUpdated)
        {
            string sizeText = (sizeBytes < 0)
                ? "확인 후 계산됩니다"
                : $"{(sizeBytes / (1024f * 1024f)):F2} MB";

            Log($"[UI] UpdatePanel ON (size={(sizeBytes < 0 ? "Unknown" : sizeText)}, panel={(updateConfirmPanel ? "OK" : "NULL")})");

            if (updateInfoText)
                updateInfoText.text = $"업데이트가 발견되었습니다\n용량: {sizeText}" +
                                      ((sizeBytes == 0 && catalogsUpdated) ? "\n(메타만 변경되어 다운로드가 없을 수 있습니다)" : string.Empty);

            if (updateConfirmPanel) updateConfirmPanel.SetActive(true);

            if (patchButton == null) return;
            patchButton.onClick.RemoveAllListeners();
            patchButton.onClick.AddListener(async () =>
            {
                // 사용자가 확인을 누른 뒤에만 카탈로그를 적용
                _cumProgress = 0f;
                SetProgress(0f, "업데이트 준비 중...");
                updateConfirmPanel?.SetActive(false);

                // 카탈로그 적용
                var checkH = Addressables.CheckForCatalogUpdates(false);
                await checkH.Task;
                var list = (checkH.IsValid() && checkH.Status == AsyncOperationStatus.Succeeded) ? checkH.Result : null;
                SafeRelease(checkH);

                if (list != null && list.Count > 0)
                {
                    var updH = Addressables.UpdateCatalogs(list, false);
                    await updH.Task;
                    Log($"UpdateCatalogs 결과: {updH.Status}");
                    SafeRelease(updH);
                    // 새 카탈로그 기준 라벨 → 로케이션 재구성
                    locations = await BuildLocationsFromLabelsAsync(patchLabels);
                    sizeBytes = -1;
                }

            
                // (테스트 옵션) 캐시 삭제: 사이즈 계산보다 먼저 해야 실제 다운로드 필요 용량이 반영됨
                if (autoClearCacheForTest && locations != null && locations.Count > 0)
                {
                    var clearH = Addressables.ClearDependencyCacheAsync(locations, true);
                    await clearH.Task;
                    SafeRelease(clearH);
                    sizeBytes = -1;
                }

                // 필요 용량 재계산(필요 시)
                if (sizeBytes < 0)
                {
                    long need = 0;
                    var sizeH = Addressables.GetDownloadSizeAsync(locations);
                    await sizeH.Task;
                    if (sizeH.IsValid() && sizeH.Status == AsyncOperationStatus.Succeeded) need = sizeH.Result;
                    SafeRelease(sizeH);
                    sizeBytes = need;
                }

                // 다운로드 or 바로 적용
                if (locations == null || locations.Count == 0 || sizeBytes <= 0)
                    await ApplyPatchedContentAsync();
                else
                    await StartDownloadAsync(locations, "업데이트");
            });
        }

        // 다운로드 시작
        private async Task StartDownloadAsync(IList<IResourceLocation> locations, string taskName)
        {
            try
            {
                if (locations == null || locations.Count == 0)
                {
                    Log("다운로드 대상이 없습니다.");
                    await ApplyPatchedContentAsync();
                    return;
                }

                var dlH = Addressables.DownloadDependenciesAsync(locations, false);
                while (!dlH.IsDone)
                {
                    var st = dlH.GetDownloadStatus();
                    float percent = Mathf.Clamp01(st.Percent);     // 0.0 ~ 1.0
                    SetProgress(percent, $"{taskName} 다운로드 중... ({percent * 100f:F0}%)");
                    await Task.Yield();
                }

                SetProgress(1f, $"{taskName} 다운로드 완료 (100%)");
                Log($"DownloadDependenciesAsync 완료: {dlH.Status}");
                SafeRelease(dlH);
            }
            catch (Exception ex)
            {
                LogError($"StartDownloadAsync exception: {ex}");
            }

            // 패치 적용 후 재시작 안내
            await ApplyPatchedContentAsync();
        }

        // 패치 적용 후: 오디오 리로드 금지(재시작 후 새 카탈로그로 로드되게)
        private async Task ApplyPatchedContentAsync()
        {
            await ShowRestartConfirmPanel();
        }

        private async Task ShowRestartConfirmPanel()
        {
            if (restartConfirmPanel == null)
            {
                await AllReadyAsync();
                return;
            }

            if (restartInfoText) restartInfoText.text = "패치가 완료되었습니다.\n재시작하시겠습니까?";
            restartConfirmPanel.SetActive(true);

            if (restartYesButton)
            {
                restartYesButton.onClick.RemoveAllListeners();
                restartYesButton.onClick.AddListener(async () =>
                {
                    restartConfirmPanel.SetActive(false);
                    await RestartGameAsync();
                });
            }
        }

        // 업데이트 없음일 때: 오디오 초기화 + 런처 종료
        private async Task AfterPatchInitAndStartAsync()
        {
            if (!PatchGate.Task.IsCompleted)
                PatchGate.TrySetResult(true);

            if (Manager.Audio != null && !Manager.Audio.IsInitialized)
            {
                Log("Audio InitializeAsync (after patch)");
                await Manager.Audio.InitializeAsync();
            }

            await AllReadyAsync();
        }
        //
        private async Task AllReadyAsync()
        {
            // 진행바를 반드시 100%까지 애니메이션
            await StartSmoothProgressAsync(1f);

            // 100% 유지
            if (finalFillHoldSeconds > 0f)
                await Task.Delay(Mathf.RoundToInt(finalFillHoldSeconds * 1000f));

            // 전체 패널 최소 노출 시간 보장
            if (_topPanelShownAt > 0f && minTopPanelVisibleSeconds > 0f)
            {
                float elapsed = Time.realtimeSinceStartup - _topPanelShownAt;
                float remain = minTopPanelVisibleSeconds - elapsed;
                if (remain > 0f)
                    await Task.Delay(Mathf.RoundToInt(remain * 1000f));
            }

            // 이제 게임 시작 가능
            if (topPanel) topPanel.SetActive(false);
        }

        private async Task RestartGameAsync()
        {
            SetProgress(0f, "재시작 중...");

            // 메모리 정리
            Resources.UnloadUnusedAssets();
            GC.Collect();

            await Task.Delay(300); // 살짝 대기 (UI 반영 여유)

            if (!PatchGate.Task.IsCompleted)
                PatchGate.TrySetResult(true);

            // 실제 프로젝트의 최초 부트 씬/초기 씬으로 교체
            // Manager.Scene.LoadScene(SceneType.YTW_TestScene3);
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        //private void DisableTopPanel()
        //{
        //    Debug.Log("topPanel 비활성화");
        //    topPanel.gameObject.SetActive(false);
        //}

        private void StartSmoothProgress(float target)
        {
            _ = StartSmoothProgressAsync(target);
        }

        private Task StartSmoothProgressAsync(float target)
        {
            if (progressBar == null) return Task.CompletedTask;

            if (_progressCo != null) StopCoroutine(_progressCo);
            // 이전 대기자가 있으면 깨워주기 (중간 취소 방지)
            _progressTcs?.TrySetResult(true);

            _progressTcs = new TaskCompletionSource<bool>();
            _progressCo = StartCoroutine(SmoothProgressCoroutine(target, progressAnimSeconds, _progressTcs));
            return _progressTcs.Task;
        }

        private IEnumerator SmoothProgressCoroutine(float target, float dur, TaskCompletionSource<bool> tcs)
        {
            float start = progressBar.value;
            float t = 0f;
            dur = Mathf.Max(0.01f, dur);

            // UI는 타임스케일 영향 없이 보여주려면 unscaledDeltaTime 권장
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                progressBar.value = Mathf.Lerp(start, target, t / dur);
                yield return null;
            }

            progressBar.value = target;
            tcs?.TrySetResult(true);
        }

        // 진행도/상태 일괄 설정
        private void SetProgress(float value01, string text = null)
        {
            if (progressBar) progressBar.value = Mathf.Clamp01(value01);
            if (statusText && text != null) statusText.text = text;
        }

        // Addressables 핸들 안전 해제 유틸
        private void SafeRelease<T>(AsyncOperationHandle<T> h)
        {
            try
            {
                if (h.IsValid()) Addressables.Release(h);
            }
            catch (Exception e)
            {
                LogError($"Release<T> 예외: {e}");
            }
        }


        private void SafeRelease(AsyncOperationHandle h)
        {
            try
            {
                if (h.IsValid()) Addressables.Release(h);
            }
            catch (Exception e)
            {
                LogError($"Release 예외: {e}");
            }
        }



        // 로그 출력 유틸
        private void Log(string m)
        {
            if (enableVerboseLogs) Debug.Log($"[Launcher] {m}");
        }


        private void LogWarning(string m)
        {
            if (enableVerboseLogs) Debug.LogWarning($"[Launcher] {m}");
        }


        private void LogError(string m)
        {
            Debug.LogError($"[Launcher] {m}");
        }
    }
}
