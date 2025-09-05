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
        [SerializeField] bool autoClearCacheForTest = false; // �׽�Ʈ ���� true
        //[SerializeField] bool reloadSceneAfterPatch = false; // �ʿ�� ����Ʈ �����
        //[SerializeField] bool restartAppAfterPatch = false;  // ���� ���� X


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
                Log("Launcher Start - Addressables �ʱ�ȭ ����");

                // 0) ���ҽ� �ý��� �غ� (����� �ʱ�ȭ�� ���� �ܰ�)
                await RunTask("���ҽ� �ý��� �غ� ��...", _cumProgress + weightInit, Manager.Resource.EnsureInitializedAsync());
                _cumProgress += weightInit;

                // 1) īŻ�α� �ֽ�ȭ
                if (statusText) statusText.text = "�ֽ� ���� Ȯ�� ��...";
                bool catalogsUpdated = await TryUpdateCatalogsAsync();
                StartSmoothProgress(_cumProgress + weightCatalog);
                _cumProgress += weightCatalog;

                // 2) �� �������� �ٿ�ε� ��� locations ����
                var locations = await BuildLocationsFromLabelsAsync(patchLabels);

                // 3) �׽�Ʈ ���� ĳ�� ���� (���� ���� ������ �� ����)
                if (autoClearCacheForTest && locations.Count > 0)
                {
                    Log("autoClearCacheForTest: ClearDependencyCacheAsync(locations)");
                    var clearH = Addressables.ClearDependencyCacheAsync(locations, true);
                    await clearH.Task;
                    SafeRelease(clearH);
                }

                // 4) �ٿ�ε� �ʿ� �뷮 ���
                var sizeH = Addressables.GetDownloadSizeAsync(locations);
                await sizeH.Task;
                StartSmoothProgress(Mathf.Min(_cumProgress + weightSizing, 0.99f)); // ���� �ܰ�� 100% ���� ����
                _cumProgress = Mathf.Min(_cumProgress + weightSizing, 0.99f);

                if (sizeH.IsValid() && sizeH.Status == AsyncOperationStatus.Succeeded)
                {
                    long needBytes = sizeH.Result;
                    Log($"�ʿ� �뷮: {needBytes} bytes, catalogsUpdated={catalogsUpdated}");

                    if (needBytes > 0 || catalogsUpdated)
                    {
                        ShowUpdateConfirmPanel(needBytes, locations, catalogsUpdated);
                    }
                    else
                    {
                        // �ٿ�ε� ���ʿ� �� �ٷ� ���� �غ�
                        await AfterPatchInitAndStartAsync();
                    }
                }
                else
                {
                    LogWarning($"GetDownloadSizeAsync ���� �Ǵ� Invalid. Status={sizeH.Status}");
                    await AfterPatchInitAndStartAsync();
                }

                SafeRelease(sizeH);
            }
            catch (Exception ex)
            {
                LogError($"Launcher Start ����: {ex}");
                if (statusText) statusText.text = "������������ �����մϴ�";
                await AfterPatchInitAndStartAsync();
            }
        }

        // ���� �� UI �⺻ ���� ����
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
            if (statusText) statusText.text = "�ʱ�ȭ ��...";
        }

        // ���� �ؽ�Ʈ + �񵿱� �۾� + ��ǥ ����ġ(0~1)�� ������ �̵�
        private async Task RunTask(string status, float targetProgress, Task task)
        {
            if (statusText != null) statusText.text = status;
            try
            {
                await task;
            }
            catch (Exception e)
            {
                LogWarning($"RunTask ����: {e}");
            }
            StartSmoothProgress(targetProgress);
            await Task.Yield();
        }

        // --- Catalog update ---
        private async Task<bool> TryUpdateCatalogsAsync()
        {
            Log("CheckForCatalogUpdates ȣ��");
            var checkH = Addressables.CheckForCatalogUpdates(false);
            await checkH.Task;

            bool updated = false;
            bool needUpdate = checkH.IsValid() &&
                              checkH.Status == AsyncOperationStatus.Succeeded &&
                              checkH.Result != null && checkH.Result.Count > 0;


            if (needUpdate)
            {
                Log($"īŻ�α� ������Ʈ �ʿ�: {checkH.Result.Count}");
                var updH = Addressables.UpdateCatalogs(checkH.Result, false);
                await updH.Task;
                updated = updH.IsValid() && updH.Status == AsyncOperationStatus.Succeeded;
                SafeRelease(updH);
            }
            else
            {
                Log("īŻ�α� ���� ����");
            }


            SafeRelease(checkH);
            return updated;
        }

        // �� �� �����̼�
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

            var result = locH.Result; // ��� ���� �� �ڵ鸸 ����
            Addressables.Release(locH);
            return result;
        }

        // ����� �ʱ�ȭ �Ϸ� ��� (Ÿ�Ӿƿ��� �α�).
        private async Task WaitForAudioInit()
        {
            int tick = 0;
            while (!Manager.Audio.IsInitialized && tick++ < 600) { await Task.Yield(); }
            if (!Manager.Audio.IsInitialized) LogWarning("Audio init timeout");
            else Log("Audio ready");
        }

        // UI �� �ٿ�ε� ó��
        private void ShowUpdateConfirmPanel(long sizeBytes, IList<IResourceLocation> locations, bool catalogsUpdated)
        {
            float mb = sizeBytes / (1024f * 1024f);
            string note = (sizeBytes == 0 && catalogsUpdated) ? "\n(��Ÿ�� ����Ǿ� �ٿ�ε尡 ���� �� �ֽ��ϴ�)" : string.Empty;

            Log($"[UI] UpdatePanel ON (size={mb:F2}MB, catalogsUpdated={catalogsUpdated}, panel={(updateConfirmPanel ? "OK" : "NULL")})");

            if (updateInfoText)
                updateInfoText.text = $"������Ʈ�� �߰ߵǾ����ϴ�\n�뷮: {mb:F2} MB{note}";
            if (updateConfirmPanel)
                updateConfirmPanel.SetActive(true);

            if (patchButton == null) return;
            patchButton.onClick.RemoveAllListeners();
            patchButton.onClick.AddListener(() =>
            {
                // ����ڰ� Ȯ���� ������ ����� 0���� �ʱ�ȭ�ϰ� ��ġ ����
                _cumProgress = 0f;
                SetProgress(0f, "������Ʈ �غ� ��...");

                if (updateConfirmPanel) updateConfirmPanel.SetActive(false);
                _ = StartDownloadAsync(locations, "������Ʈ");
            });
        }

        // �ٿ�ε� ����
        private async Task StartDownloadAsync(IList<IResourceLocation> locations, string taskName)
        {
            try
            {
                var dlH = Addressables.DownloadDependenciesAsync(locations, true);
                while (!dlH.IsDone)
                {
                    var st = dlH.GetDownloadStatus();
                    float percent = Mathf.Clamp01(st.Percent);     // 0.0 ~ 1.0
                    SetProgress(percent, $"{taskName} �ٿ�ε� ��... ({percent * 100f:F0}%)");
                    await Task.Yield();
                }

                // ������ ����
                SetProgress(1f, $"{taskName} �ٿ�ε� �Ϸ� (100%)");
                Log($"DownloadDependenciesAsync �Ϸ�: {dlH.Status}");
                SafeRelease(dlH);
            }
            catch (Exception ex)
            {
                LogError($"StartDownloadAsync exception: {ex}");
            }

            // ��ġ ���� �� �� ����� �ȳ�
            await ApplyPatchedContentAsync();
        }

        // ��ġ ���� �� �ʿ��� ��ε��� ������ �� ���� �غ� ���·� ��ȯ
        private async Task ApplyPatchedContentAsync()
        {
            try
            {
                if (Manager.Audio != null && Manager.Audio.IsInitialized)
                    await Manager.Audio.ReloadAllAudioClipsAfterPatchAsync();
            }
            catch (Exception ex)
            {
                LogWarning($"����� ��ε� �� ����: {ex}");
            }

            // 100% ���� �г� ����
            ShowRestartConfirmPanel();
        }

        private void ShowRestartConfirmPanel()
        {
            if (restartConfirmPanel == null)
            {
                AllReady();
                return;
            }

            if (restartInfoText) restartInfoText.text = "��ġ�� �Ϸ�Ǿ����ϴ�.\n������Ͻðڽ��ϱ�?";
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
                    AllReady(); // ����� ���� ���
                });
            }
        }

        // ��ġ ���� ����� �ʱ�ȭ�� �����̶�� �����ϰ�, ���������� ���� ���� ���·� ��ȯ
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
            if (statusText) statusText.text = "Tap to Start";

            if (startButton == null) return;
            startButton.gameObject.SetActive(true);
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() =>
            {
                // ���� ù ��/���� ������ ��ü
                Manager.Scene.LoadScene(SceneType.YTW_TestScene1);
            });
        }

        private async Task RestartGameAsync()
        {
            SetProgress(0f, "����� ��...");

            // �޸� ����
            Resources.UnloadUnusedAssets();
            GC.Collect();

            await Task.Delay(300); // ��¦ ��� (UI �ݿ� ����)

            // ���� ������Ʈ�� ���� ��Ʈ ��/�ʱ� ������ ��ü
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

        // ���൵/���� �ϰ� ����
        private void SetProgress(float value01, string text = null)
        {
            if (progressBar) progressBar.value = Mathf.Clamp01(value01);
            if (statusText && text != null) statusText.text = text;
        }

        // Addressables �ڵ� ���� ���� ��ƿ
        private void SafeRelease<T>(AsyncOperationHandle<T> h)
        {
            try
            {
                if (h.IsValid()) Addressables.Release(h);
            }
            catch (Exception e)
            {
                LogError($"Release<T> ����: {e}");
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
                LogError($"Release ����: {e}");
            }
        }



        // �α� ��� ��ƿ
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
