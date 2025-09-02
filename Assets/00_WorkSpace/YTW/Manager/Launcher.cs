using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders; // IResourceLocator 때문에 필요
using UnityEngine.AddressableAssets.ResourceLocators; // IResourceLocator 때문에 필요

namespace YTW
{
    public class Launcher : MonoBehaviour
    {
        [Header("기본 UI")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button startButton;

        [Header("업데이트 확인 패널")]
        [SerializeField] private GameObject updateConfirmPanel;
        [SerializeField] private TextMeshProUGUI updateInfoText;
        [SerializeField] private Button patchButton;

        private Coroutine _progressCoroutine;

        async void Start()
        {
            // UI 초기 설정
            startButton.gameObject.SetActive(false);
            updateConfirmPanel.SetActive(false);
            progressBar.value = 0f;
            progressBar.gameObject.SetActive(true);

            // 각 단계별 초기화 진행
            await RunTask("기본 시스템 초기화 중...", 0.2f, Manager.Resource.InitializeAsyncInternal());
            await RunTask("사운드 데이터 로딩 중...", 0.4f, WaitForAudioInit());

            // 업데이트 확인
            statusText.text = "최신 버전 확인 중...";
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            await checkHandle.Task;
            StartSmoothProgress(0.5f);

            if (checkHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                List<string> catalogsToUpdate = checkHandle.Result;
                if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
                {
                    // *** 로직 수정: 카탈로그를 업데이트하기 전에 다운로드 크기부터 먼저 확인 ***
                    var sizeHandle = Addressables.GetDownloadSizeAsync(catalogsToUpdate);
                    await sizeHandle.Task;
                    long downloadSize = sizeHandle.Result;

                    if (downloadSize > 0)
                    {
                        // 다운로드할 것이 있을 때만 패널을 보여줌
                        ShowUpdateConfirmPanel(downloadSize, catalogsToUpdate);
                    }
                    else
                    {
                        // 다운로드할 번들은 없지만 카탈로그만 업데이트해야 하는 경우 (거의 없음)
                        var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                        await updateHandle.Task;
                        Addressables.Release(updateHandle);
                        AllReady();
                    }
                    Addressables.Release(sizeHandle);
                }
                else
                {
                    // 업데이트가 없을 경우, 바로 시작 준비 완료
                    AllReady();
                }
            }
            Addressables.Release(checkHandle);
        }

        // 각 단계를 실행하고 부드러운 로딩 바를 보여주는 헬퍼 함수
        private async Task RunTask(string status, float targetProgress, Task task)
        {
            statusText.text = status;
            await task;
            StartSmoothProgress(targetProgress);
            await Task.Yield();
        }

        // AudioManager 초기화를 기다리는 Task
        private async Task WaitForAudioInit()
        {
            while (!Manager.Audio.IsInitialized)
            {
                await Task.Yield();
            }
        }

        // 업데이트 확인 패널을 보여주는 함수
        private void ShowUpdateConfirmPanel(long size, List<string> catalogsToUpdate)
        {
            float sizeInMB = size / (1024f * 1024f);
            updateInfoText.text = $"새로운 업데이트가 있습니다.\n다운로드 크기: {sizeInMB:F2} MB";
            updateConfirmPanel.SetActive(true);

            patchButton.onClick.RemoveAllListeners();
            patchButton.onClick.AddListener(() =>
            {
                // 패치 버튼을 누르면 다운로드 시작
                _ = StartDownloadAsync(catalogsToUpdate, "업데이트");
            });
        }

        // 실제 다운로드를 처리하는 비동기 함수
        private async Task StartDownloadAsync(List<string> catalogsToUpdate, string taskName)
        {
            updateConfirmPanel.SetActive(false);

            // 1. 카탈로그부터 업데이트
            var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
            await updateHandle.Task;

            // 2. 이제 업데이트된 카탈로그를 기반으로 다운로드 진행
            var downloadHandle = Addressables.DownloadDependenciesAsync(updateHandle.Result, true);
            while (!downloadHandle.IsDone)
            {
                float percent = downloadHandle.PercentComplete;
                float targetProgress = 0.5f + (percent * 0.5f);
                progressBar.value = Mathf.Lerp(progressBar.value, targetProgress, Time.deltaTime * 5f);
                statusText.text = $"{taskName} 다운로드 중... ({percent * 100:F0}%)";
                await Task.Yield();
            }
            Addressables.Release(downloadHandle);
            Addressables.Release(updateHandle);

            AllReady(); // 다운로드 완료 후 시작 준비
        }

        // 모든 준비가 완료되었을 때 호출되는 함수
        private void AllReady()
        {
            StartSmoothProgress(1f);
            statusText.text = "화면을 터치하여 게임 시작";

            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                Manager.Scene.LoadScene(SceneType.YTW_TestScene1);
            });
        }

        // 로딩 바를 부드럽게 채우는 코루틴을 시작하는 함수
        private void StartSmoothProgress(float target)
        {
            if (_progressCoroutine != null)
            {
                StopCoroutine(_progressCoroutine);
            }
            _progressCoroutine = StartCoroutine(SmoothProgressCoroutine(target));
        }

        // 실제 로딩 바를 부드럽게 움직이는 코루틴
        private IEnumerator SmoothProgressCoroutine(float target)
        {
            float current = progressBar.value;
            float time = 0f;
            const float duration = 0.3f; // duration 동안 부드럽게 

            while (time < duration)
            {
                time += Time.deltaTime;
                progressBar.value = Mathf.Lerp(current, target, time / duration);
                yield return null;
            }
            progressBar.value = target; // 정확한 값으로 마무리
        }
    }
}