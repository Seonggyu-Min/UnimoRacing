using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders; // IResourceLocator ������ �ʿ�
using UnityEngine.AddressableAssets.ResourceLocators; // IResourceLocator ������ �ʿ�

namespace YTW
{
    public class Launcher : MonoBehaviour
    {
        [Header("�⺻ UI")]
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Button startButton;

        [Header("������Ʈ Ȯ�� �г�")]
        [SerializeField] private GameObject updateConfirmPanel;
        [SerializeField] private TextMeshProUGUI updateInfoText;
        [SerializeField] private Button patchButton;

        private Coroutine _progressCoroutine;

        async void Start()
        {
            // UI �ʱ� ����
            startButton.gameObject.SetActive(false);
            updateConfirmPanel.SetActive(false);
            progressBar.value = 0f;
            progressBar.gameObject.SetActive(true);

            // �� �ܰ躰 �ʱ�ȭ ����
            await RunTask("�⺻ �ý��� �ʱ�ȭ ��...", 0.2f, Manager.Resource.InitializeAsyncInternal());
            await RunTask("���� ������ �ε� ��...", 0.4f, WaitForAudioInit());

            // ������Ʈ Ȯ��
            statusText.text = "�ֽ� ���� Ȯ�� ��...";
            var checkHandle = Addressables.CheckForCatalogUpdates(false);
            await checkHandle.Task;
            StartSmoothProgress(0.5f);

            if (checkHandle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                List<string> catalogsToUpdate = checkHandle.Result;
                if (catalogsToUpdate != null && catalogsToUpdate.Count > 0)
                {
                    // *** ���� ����: īŻ�α׸� ������Ʈ�ϱ� ���� �ٿ�ε� ũ����� ���� Ȯ�� ***
                    var sizeHandle = Addressables.GetDownloadSizeAsync(catalogsToUpdate);
                    await sizeHandle.Task;
                    long downloadSize = sizeHandle.Result;

                    if (downloadSize > 0)
                    {
                        // �ٿ�ε��� ���� ���� ���� �г��� ������
                        ShowUpdateConfirmPanel(downloadSize, catalogsToUpdate);
                    }
                    else
                    {
                        // �ٿ�ε��� ������ ������ īŻ�α׸� ������Ʈ�ؾ� �ϴ� ��� (���� ����)
                        var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
                        await updateHandle.Task;
                        Addressables.Release(updateHandle);
                        AllReady();
                    }
                    Addressables.Release(sizeHandle);
                }
                else
                {
                    // ������Ʈ�� ���� ���, �ٷ� ���� �غ� �Ϸ�
                    AllReady();
                }
            }
            Addressables.Release(checkHandle);
        }

        // �� �ܰ踦 �����ϰ� �ε巯�� �ε� �ٸ� �����ִ� ���� �Լ�
        private async Task RunTask(string status, float targetProgress, Task task)
        {
            statusText.text = status;
            await task;
            StartSmoothProgress(targetProgress);
            await Task.Yield();
        }

        // AudioManager �ʱ�ȭ�� ��ٸ��� Task
        private async Task WaitForAudioInit()
        {
            while (!Manager.Audio.IsInitialized)
            {
                await Task.Yield();
            }
        }

        // ������Ʈ Ȯ�� �г��� �����ִ� �Լ�
        private void ShowUpdateConfirmPanel(long size, List<string> catalogsToUpdate)
        {
            float sizeInMB = size / (1024f * 1024f);
            updateInfoText.text = $"���ο� ������Ʈ�� �ֽ��ϴ�.\n�ٿ�ε� ũ��: {sizeInMB:F2} MB";
            updateConfirmPanel.SetActive(true);

            patchButton.onClick.RemoveAllListeners();
            patchButton.onClick.AddListener(() =>
            {
                // ��ġ ��ư�� ������ �ٿ�ε� ����
                _ = StartDownloadAsync(catalogsToUpdate, "������Ʈ");
            });
        }

        // ���� �ٿ�ε带 ó���ϴ� �񵿱� �Լ�
        private async Task StartDownloadAsync(List<string> catalogsToUpdate, string taskName)
        {
            updateConfirmPanel.SetActive(false);

            // 1. īŻ�α׺��� ������Ʈ
            var updateHandle = Addressables.UpdateCatalogs(catalogsToUpdate, false);
            await updateHandle.Task;

            // 2. ���� ������Ʈ�� īŻ�α׸� ������� �ٿ�ε� ����
            var downloadHandle = Addressables.DownloadDependenciesAsync(updateHandle.Result, true);
            while (!downloadHandle.IsDone)
            {
                float percent = downloadHandle.PercentComplete;
                float targetProgress = 0.5f + (percent * 0.5f);
                progressBar.value = Mathf.Lerp(progressBar.value, targetProgress, Time.deltaTime * 5f);
                statusText.text = $"{taskName} �ٿ�ε� ��... ({percent * 100:F0}%)";
                await Task.Yield();
            }
            Addressables.Release(downloadHandle);
            Addressables.Release(updateHandle);

            AllReady(); // �ٿ�ε� �Ϸ� �� ���� �غ�
        }

        // ��� �غ� �Ϸ�Ǿ��� �� ȣ��Ǵ� �Լ�
        private void AllReady()
        {
            StartSmoothProgress(1f);
            statusText.text = "ȭ���� ��ġ�Ͽ� ���� ����";

            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                Manager.Scene.LoadScene(SceneType.YTW_TestScene1);
            });
        }

        // �ε� �ٸ� �ε巴�� ä��� �ڷ�ƾ�� �����ϴ� �Լ�
        private void StartSmoothProgress(float target)
        {
            if (_progressCoroutine != null)
            {
                StopCoroutine(_progressCoroutine);
            }
            _progressCoroutine = StartCoroutine(SmoothProgressCoroutine(target));
        }

        // ���� �ε� �ٸ� �ε巴�� �����̴� �ڷ�ƾ
        private IEnumerator SmoothProgressCoroutine(float target)
        {
            float current = progressBar.value;
            float time = 0f;
            const float duration = 0.3f; // duration ���� �ε巴�� 

            while (time < duration)
            {
                time += Time.deltaTime;
                progressBar.value = Mathf.Lerp(current, target, time / duration);
                yield return null;
            }
            progressBar.value = target; // ��Ȯ�� ������ ������
        }
    }
}