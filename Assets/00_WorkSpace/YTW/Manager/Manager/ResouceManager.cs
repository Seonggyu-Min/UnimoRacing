using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;


namespace YTW
{
    // �ڵ� �ּ� ����
    public class ResourceManager : Singleton<ResourceManager>
    {
        // �ε�� ������ �񵿱� �۾� �ڵ��� �ּ�(string)�� Ű�� �Ͽ� �����մϴ�. �̹� �ε� ���� ���¿� ���� �ߺ� ��û�� �����ϰ� ���� �ڵ� ����
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
        // �� ������ �� ���̳� ����(�ε� ��û)�Ǿ����� Ƚ���� ����մϴ�. �� Ƚ���� 0�� �Ǿ�߸� �޸𸮿��� ���� ����
        private readonly Dictionary<string, int> _refCounts = new Dictionary<string, int>();
        // InstantiateAsync�� ������ GameObject �ν��Ͻ�. �� �ν��Ͻ��� ���� �ڵ�
        // Addressables.ReleaseInstance�� ȣ���� �� ���� �ڵ��� �ʿ�
        private readonly Dictionary<GameObject, AsyncOperationHandle> _instanceHandles = new Dictionary<GameObject, AsyncOperationHandle>();
        // �񵿱�/��Ƽ������ ��Ȳ���� ��ųʸ� ���� ������ ����ȭ�ϱ� ���� lock ������Ʈ
        private readonly object _lock = new object();
        // ��巹���� �ý����� �񵿱� �ʱ�ȭ �۾��� �Ϸ�Ǿ����� �˸��� ����
        // - ����: TrySetResult(true)
        // - ����: TrySetException(ex)
        private TaskCompletionSource<bool> _initTcs = new TaskCompletionSource<bool>();
        // �ʱ�ȭ�� �Ϸ�Ǿ����� ���θ� ��Ÿ���� �÷���
        private bool _initialized = false;

        protected override void Awake()
        {
            base.Awake();
            // �񵿱� �ʱ�ȭ �޼ҵ带 ȣ���մϴ�. `_ = `�� '�� �۾��� ����� ��ٸ��� �ʰ� �ϴ� ���۸� ���Ѷ�'��� �ǹ�(fire-and-forget ����)
            _ = InitializeAsyncInternal();
        }

        // �ʱ�ȭ ���� �Լ�
        // �ܺ� API(Load/Instantiate/LoadScene) ���� ��, Addressables�� �ʱ�ȭ�Ǿ����� ����
        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;
            Debug.Log("[ResourceManager] EnsureInitializedAsync �����...");
            // ���� �ʱ�ȭ ���̶��, _initTcs.Task�� �Ϸ�� ��(��ȣ���� ���� ��)���� ���⼭ �񵿱������� ���
            await _initTcs.Task;
        }

        // ��巹���� �ý����� ������ �ʱ�ȭ�ϴ� ���� �񵿱� �޼ҵ�
        public async Task InitializeAsyncInternal()
        {
            Debug.Log("[ResourceManager] InitializeAsyncInternal ����");
            try
            {
                // Addressables �ý��� �ʱ�ȭ �񵿱� �ڵ� ȹ��
                var initHandle = Addressables.InitializeAsync();
                try
                {
                    // ���������� ��巹���� ����, īŻ�α� �ε� �� ����
                    await initHandle.Task;
                }
                catch (Exception ex)
                {
                    // �ڵ�.Task ��� �� �߻��� �� �ִ� ���� �α�
                    Debug.LogError($"[ResourceManager] Addressables.InitializeAsync ����: {ex}");
                }

                // �ڵ��� ��ȿ���� ���� (������ Ư�� ��忡�� ��ȿ�� ���ɼ� ����)
                if (initHandle.IsValid())
                {
                    if (initHandle.Status == AsyncOperationStatus.Succeeded)
                        Debug.Log("[ResourceManager] Addressables �ʱ�ȭ ����.");
                    else
                        Debug.LogError($"[ResourceManager] Addressables �ʱ�ȭ ����: {initHandle.OperationException}");
                    // �ʱ�ȭ �ڵ��� �� �̻� �ʿ� �����Ƿ� �����ϰ� ������
                    try { Addressables.Release(initHandle); } catch { }
                }
                else
                {
                    // ������ Play Mode Script�� Asset Database ����� �� �� ��ȿ �ڵ��� ���ƿ� �� ����
                    Debug.LogWarning("[ResourceManager] InitializeAsync ��ȯ �ڵ��� ��ȿ���� �ʽ��ϴ�. (�������� Play Mode Script ������ ���� �߻��� �� ����) " +
                                     "�ʿ�� Addressables ����(Play Mode Script / Content build)�� Ȯ���ϼ���.");
                    // �ڵ��� ��ȿ���� ������ DB ��� ��� ���� ������ �� �����Ƿ� �ʱ�ȭ �������� ó��
                }

                // �ʱ�ȭ ���� �÷��� ���� �� ����ڵ鿡�� �Ϸ� ��ȣ
                _initialized = true;
                _initTcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                // �ʱ�ȭ �ܰ迡�� ����ġ ���� ���� �߻� �� ���� ��ȣ ����
                Debug.LogError($"[ResourceManager] InitializeAsyncInternal ���� ����: {ex}");
                _initTcs.TrySetException(ex);
            }
        }

        // ���� �ε�
        // �ּ�(address)�� �ش��ϴ� ������ �񵿱� �ε��մϴ�. �ߺ� ��û �� ���� �ڵ��� �����ϸ�, ���� ī��Ʈ�� ����
        public async Task<T> LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            // 1) Addressables �ʱ�ȭ ����
            await EnsureInitializedAsync();

            AsyncOperationHandle handleToAwait; // �� �޼��� �ۿ����� ����� �ڵ��� ��Ƶ� ����

            // 2) ���� ����(��ųʸ�) ������ lock���� ��ȣ
            lock (_lock)
            {
                // �̹� �ε�(�Ǵ� �ε� ��)�� ��� ���� �ڵ� ���� + refCount ����
                if (_handles.TryGetValue(address, out var cachedHandle))
                {
                    // �ִٸ�, ���� Ƚ���� �ø��� �ش� �ڵ��� ��ȯ�Ͽ� lock �ۿ��� await �ϵ��� �մϴ�.
                    _refCounts[address]++;
                    Debug.Log($"[ResourceManager] ���� �ڵ� ��ȯ (����: {_refCounts[address]}): {address}");
                    handleToAwait = cachedHandle;
                }
                else
                {
                    // ó�� ��û: �� �ε� ����, �ڵ�� refCount ���
                    var newHandle = Addressables.LoadAssetAsync<T>(address);
                    _handles[address] = newHandle;
                    _refCounts[address] = 1; // ù ��û�̹Ƿ� ���� Ƚ���� 1
                    handleToAwait = newHandle;
                }
            }

            // 3) �ε� �Ϸ���� �񵿱� ��� (���� �߻� ����)
            try
            {
                await handleToAwait.Task;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManager] ���� �ε��� ����: {address} => {ex}");
            }

            // 4) ������ �˻�: �ڵ� ��ȿ��
            if (!handleToAwait.IsValid())
            {
                Debug.LogError($"[ResourceManager] �ڵ��� ��ȿ���� �ʽ��ϴ�: {address}");
                Release(address);
                return null;
            }

            // 5) �ε� ��� Ȯ��
            if (handleToAwait.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    // Convert<T>()�� �� ��ȯ �� ��� ��ȯ
                    var converted = _handles[address].Convert<T>();
                    return converted.Result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ResourceManager] ��ȯ �Ǵ� ��� ���� ����: {address} => {ex}");
                    return null;
                }
            }
            else
            {
                // ���� �� �α� �� ����
                Debug.LogError($"[ResourceManager] ���� �ε� ����: {address}, ����: {handleToAwait.OperationException}");
                Release(address);
                return null;
            }
        }

        // �ν��Ͻ� ����
        // ������ �ּҸ� ���� �ν��Ͻ�ȭ
        public async Task<GameObject> InstantiateAsync(string address, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(address)) return null; // �߸��� �ּ� ���

            // �ν��Ͻ� ���� ������ �ʱ�ȭ ����
            await EnsureInitializedAsync();

            // Instantiate �� ���׸� �ڵ� (GameObject)
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, position, rotation);
            try
            {
                await handle.Task; // ���� �Ϸ� ���
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManager] InstantiateAsync ����: {address} => {ex}");
            }

            // ��ȿ��/���� ����
            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] Instantiate ����: {address}, ex: {handle.OperationException}");
                // ���� �� �ڵ� ���� �õ�
                try { Addressables.Release(handle); } catch { }
                return null;
            }

            // ���� �� ��� ��ȯ
            var go = handle.Result;
            lock (_lock)
            {
                _instanceHandles[go] = handle; // ���� ��� (������ �ı� ����)
            }
            return go;
        }

        // ���� ���� (Release)
        // LoadAsync�� �ε�� ������ ������ �ϳ� �����մϴ�. ������ 0�� �Ǹ� �޸𸮿��� ����
        public void Release(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return; // ��� �ڵ�

            lock (_lock)
            {
                // ��ϵ� �ڵ��� ������ ��� ����� ��ȯ
                if (!_handles.TryGetValue(address, out var handle))
                {
                    Debug.LogWarning($"[ResourceManager] Release �õ������� �ڵ� ����: {address}");
                    return;
                }

                // refCount�� ������ ������ �����̹Ƿ� �ٷ� Release �õ�
                if (!_refCounts.ContainsKey(address))
                {
                    // ���� ��ġ
                    Debug.LogWarning($"[ResourceManager] Release: refCount ��� ����, �ٷ� ������: {address}");
                    if (handle.IsValid()) Addressables.Release(handle);
                    _handles.Remove(address);
                    return;
                }

                // ���� �ϳ� ����
                _refCounts[address]--;
                Debug.Log($"[ResourceManager] Release ȣ��, ���� refCount={_refCounts[address]} : {address}");

                // ������ 0 �����̸� ���� ����
                if (_refCounts[address] <= 0)
                {
                    if (handle.IsValid())
                    {
                        try { Addressables.Release(handle); }
                        catch (Exception ex) { Debug.LogWarning($"[ResourceManager] Addressables.Release ����: {ex}"); }
                    }

                    // ���̺��� ����
                    _handles.Remove(address);
                    _refCounts.Remove(address);
                }
            }
        }


        // �ν��Ͻ� ���� (ReleaseInstance)
        // InstantiateAsync�� ������ ���� ������Ʈ�� �����ϰ� �ı��մϴ�.
        // _instanceHandles�� ������ ��� Addressables.ReleaseInstance�� ���
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return; // ��� �ڵ�

            lock (_lock)
            {
                if (_instanceHandles.TryGetValue(instance, out var handle))
                {
                    // ������ �ڵ��� �ִٸ� Addressables.ReleaseInstance�� ��ȯ
                    Addressables.ReleaseInstance(handle);
                    _instanceHandles.Remove(instance);
                }
                else
                {
                    // �������� �ʾҴٸ� �Ϲ� Destroy ��� (�ּ� ���� �ս� ������ �� ����)
                    Debug.LogWarning($"[ResourceManager] �������� ���� �ν��Ͻ� �ı� �õ�: {instance.name}");
                    Destroy(instance);
                }
            }
        }

        // �� �ε�
        // LoadSceneMode
        // single : ���ο� ���� �ε��ϱ� ���� ���� �����ִ� ��� ���� ����
        // Additive : ���� �����ִ� ���� �״�� �� ä, �� ���� ���ο� ���� �߰��� �ҷ���
        public async Task<SceneInstance?> LoadSceneAsync(string sceneAddress, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneAddress)) return null;

            await EnsureInitializedAsync();// �ʱ�ȭ ����

            // �� �ε�� Addressables.LoadSceneAsync ���
            var handle = Addressables.LoadSceneAsync(sceneAddress, mode);
            try
            {
                await handle.Task; // �ε� �Ϸ� ���
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManager] LoadSceneAsync ����: {sceneAddress} => {ex}");
            }

            if (!handle.IsValid())
            {
                Debug.LogError($"[ResourceManager] Scene �ڵ� ��ȿ���� ����: {sceneAddress}");
                try { Addressables.Release(handle); } catch { }
                return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // ���� �� SceneInstance ��ȯ (nullable)
                return handle.Result;
            }
            else
            {
                Debug.LogError($"[ResourceManager] Scene �ε� ����: {sceneAddress}, ex: {handle.OperationException}");
                try { Addressables.Release(handle); } catch { }
                return null;
            }
        }
    }
}
