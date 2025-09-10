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
        [SerializeField] private Button restartNoButton;

        [Header("Patch Labels")]
        [SerializeField] List<string> patchLabels = new() { "Remote" };

        [Header("Debug/Options")]
        [SerializeField] bool enableVerboseLogs = true;
        [SerializeField] bool autoClearCacheForTest = false; // 테스트 때만 true
        //[SerializeField] bool reloadSceneAfterPatch = false; // 필요시 소프트 재시작
        //[SerializeField] bool restartAppAfterPatch = false;  // 거의 권장 X


        [Header("Progress Weights (pre-patch)")]
        [SerializeField] float weightInit = 0.2f;      // EnsureInitializedAsync
        [SerializeField] float weightCatalog = 0.3f;   // TryUpdateCatalogsAsync
        [SerializeField] float weightSizing = 0.5f;    // LoadLocations + GetDownloadSize

        private float _cumProgress = 0f;

        Coroutine _progressCo;

        async void Start()
        {
            InitializeUIOnStart();

            try
            {
                Log("Launcher Start - Addressables 초기화 시작");

                // 0) 리소스 시스템 준비 (오디오 초기화는 나중 단계)
                await RunTask("리소스 시스템 준비 중...", _cumProgress + weightInit, Manager.Resource.EnsureInitializedAsync());
                _cumProgress += weightInit;

                // 1) 카탈로그 최신화
                if (statusText) statusText.text = "최신 버전 확인 중...";
                bool catalogsUpdated = await TryUpdateCatalogsAsync();
                StartSmoothProgress(_cumProgress + weightCatalog);
                _cumProgress += weightCatalog;

                // 2) 라벨 기준으로 다운로드 대상 locations 구성
                var locations = await BuildLocationsFromLabelsAsync(patchLabels);

                // 3) 테스트 전용 캐시 비우기 (에셋 참조 잡히기 전 시점)
                if (autoClearCacheForTest && locations.Count > 0)
                {
                    Log("autoClearCacheForTest: ClearDependencyCacheAsync(locations)");
                    var clearH = Addressables.ClearDependencyCacheAsync(locations, true);
                    await clearH.Task;
                    SafeRelease(clearH);
                }

                // 4) 다운로드 필요 용량 계산
                var sizeH = Addressables.GetDownloadSizeAsync(locations);
                await sizeH.Task;
                StartSmoothProgress(Mathf.Min(_cumProgress + weightSizing, 0.99f)); // 사전 단계는 100% 찍지 않음
                _cumProgress = Mathf.Min(_cumProgress + weightSizing, 0.99f);

                if (sizeH.IsValid() && sizeH.Status == AsyncOperationStatus.Succeeded)
                {
                    long needBytes = sizeH.Result;
                    Log($"필요 용량: {needBytes} bytes, catalogsUpdated={catalogsUpdated}");

                    if (needBytes > 0 || catalogsUpdated)
                    {
                        ShowUpdateConfirmPanel(needBytes, locations, catalogsUpdated);
                    }
                    else
                    {
                        // 다운로드 불필요 → 바로 시작 준비
                        await AfterPatchInitAndStartAsync();
                    }
                }
                else
                {
                    LogWarning($"GetDownloadSizeAsync 실패 또는 Invalid. Status={sizeH.Status}");
                    await AfterPatchInitAndStartAsync();
                }

                SafeRelease(sizeH);
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

        // --- Catalog update ---
        private async Task<bool> TryUpdateCatalogsAsync()
        {
            Log("CheckForCatalogUpdates 호출");
            var checkH = Addressables.CheckForCatalogUpdates(false);
            await checkH.Task;

            bool updated = false;
            bool needUpdate = checkH.IsValid() &&
                              checkH.Status == AsyncOperationStatus.Succeeded &&
                              checkH.Result != null && checkH.Result.Count > 0;


            if (needUpdate)
            {
                Log($"카탈로그 업데이트 필요: {checkH.Result.Count}");
                var updH = Addressables.UpdateCatalogs(checkH.Result, false);
                await updH.Task;
                updated = updH.IsValid() && updH.Status == AsyncOperationStatus.Succeeded;
                SafeRelease(updH);
            }
            else
            {
                Log("카탈로그 변경 없음");
            }


            SafeRelease(checkH);
            return updated;
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

        // 오디오 초기화 완료 대기 (타임아웃만 로그).
        private async Task WaitForAudioInit()
        {
            int tick = 0;
            while (!Manager.Audio.IsInitialized && tick++ < 600) { await Task.Yield(); }
            if (!Manager.Audio.IsInitialized) LogWarning("Audio init timeout");
            else Log("Audio ready");
        }

        // UI 및 다운로드 처리
        private void ShowUpdateConfirmPanel(long sizeBytes, IList<IResourceLocation> locations, bool catalogsUpdated)
        {
            float mb = sizeBytes / (1024f * 1024f);
            string note = (sizeBytes == 0 && catalogsUpdated) ? "\n(메타만 변경되어 다운로드가 없을 수 있습니다)" : string.Empty;

            Log($"[UI] UpdatePanel ON (size={mb:F2}MB, catalogsUpdated={catalogsUpdated}, panel={(updateConfirmPanel ? "OK" : "NULL")})");

            if (updateInfoText)
                updateInfoText.text = $"업데이트가 발견되었습니다\n용량: {mb:F2} MB{note}";
            if (updateConfirmPanel)
                updateConfirmPanel.SetActive(true);

            if (patchButton == null) return;
            patchButton.onClick.RemoveAllListeners();
            patchButton.onClick.AddListener(() =>
            {
                // 사용자가 확인을 누르면 진행바 0으로 초기화하고 패치 시작
                _cumProgress = 0f;
                SetProgress(0f, "업데이트 준비 중...");

                if (updateConfirmPanel) updateConfirmPanel.SetActive(false);
                _ = StartDownloadAsync(locations, "업데이트");
            });
        }

        // 다운로드 시작
        private async Task StartDownloadAsync(IList<IResourceLocation> locations, string taskName)
        {
            try
            {
                var dlH = Addressables.DownloadDependenciesAsync(locations, true);
                while (!dlH.IsDone)
                {
                    var st = dlH.GetDownloadStatus();
                    float percent = Mathf.Clamp01(st.Percent);     // 0.0 ~ 1.0
                    SetProgress(percent, $"{taskName} 다운로드 중... ({percent * 100f:F0}%)");
                    await Task.Yield();
                }

                // 마지막 보정
                SetProgress(1f, $"{taskName} 다운로드 완료 (100%)");
                Log($"DownloadDependenciesAsync 완료: {dlH.Status}");
                SafeRelease(dlH);
            }
            catch (Exception ex)
            {
                LogError($"StartDownloadAsync exception: {ex}");
            }

            // 패치 적용 후 → 재시작 안내
            await ApplyPatchedContentAsync();
        }

        // 패치 적용 후 필요한 재로딩을 수행한 뒤 시작 준비 상태로 전환
        private async Task ApplyPatchedContentAsync()
        {
            try
            {
                if (Manager.Audio != null && Manager.Audio.IsInitialized)
                    await Manager.Audio.ReloadAllAudioClipsAfterPatchAsync();
            }
            catch (Exception ex)
            {
                LogWarning($"오디오 재로딩 중 예외: {ex}");
            }

            // 100% 이후 패널 노출
            ShowRestartConfirmPanel();
        }

        private void ShowRestartConfirmPanel()
        {
            if (restartConfirmPanel == null)
            {
                AllReady();
                return;
            }

            if (restartInfoText) restartInfoText.text = "패치가 완료되었습니다.\n재시작하시겠습니까?";
            restartConfirmPanel.SetActive(true);

            if (restartYesButton)
            {
                restartYesButton.onClick.RemoveAllListeners();
                restartYesButton.onClick.AddListener(() =>
                {
                    restartConfirmPanel.SetActive(false);
                    _ = RestartGameAsync();
                });
            }

            if (restartNoButton)
            {
                restartNoButton.onClick.RemoveAllListeners();
                restartNoButton.onClick.AddListener(() =>
                {
                    restartConfirmPanel.SetActive(false);
                    AllReady(); // 재시작 없이 계속
                });
            }
        }

        // 패치 이후 오디오 초기화가 아직이라면 수행하고, 최종적으로 시작 가능 상태로 전환
        private async Task AfterPatchInitAndStartAsync()
        {
            if (Manager.Audio != null && !Manager.Audio.IsInitialized)
            {
                Log("Audio InitializeAsync (after patch)");
                await Manager.Audio.InitializeAsync();
            }

            AllReady();
        }

        private void AllReady()
        {
            StartSmoothProgress(1f);
            //if (statusText) statusText.text = "Tap to Start";

            //if (startButton == null) return;
            //startButton.gameObject.SetActive(true);
            //startButton.onClick.RemoveAllListeners();
            //startButton.onClick.AddListener(() =>
            //{
            //    // 실제 첫 씬/다음 씬으로 교체
            //    Manager.Scene.LoadScene(SceneType.YTW_TestScene1);
            //});

            topPanel.gameObject.SetActive(false);
        }

        private async Task RestartGameAsync()
        {
            SetProgress(0f, "재시작 중...");

            // 메모리 정리
            Resources.UnloadUnusedAssets();
            GC.Collect();

            await Task.Delay(300); // 살짝 대기 (UI 반영 여유)

            // 실제 프로젝트의 최초 부트 씬/초기 씬으로 교체
            Manager.Scene.LoadScene(SceneType.YTW_TestScene3);
        }

        private void StartSmoothProgress(float target)
        {
            if (progressBar == null) return;
            if (_progressCo != null) StopCoroutine(_progressCo);
            _progressCo = StartCoroutine(SmoothProgressCoroutine(target));
        }

        private IEnumerator SmoothProgressCoroutine(float target)
        {
            float start = progressBar.value;
            float t = 0f;
            const float dur = 0.3f;


            while (t < dur)
            {
                t += Time.deltaTime;
                progressBar.value = Mathf.Lerp(start, target, t / dur);
                yield return null;
            }

            progressBar.value = target;
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
