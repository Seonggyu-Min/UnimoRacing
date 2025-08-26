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
    // 코드 주석 참고
    public class ResourceManager : Singleton<ResourceManager>
    {
        // 로드된 에셋의 비동기 작업 핸들을 주소(string)를 키로 하여 저장합니다. 이미 로드 중인 에셋에 대한 중복 요청을 방지하고 동일 핸들 재사용
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new Dictionary<string, AsyncOperationHandle>();
        // 각 에셋이 몇 번이나 참조(로드 요청)되었는지 횟수를 기록합니다. 이 횟수가 0이 되어야만 메모리에서 해제 가능
        private readonly Dictionary<string, int> _refCounts = new Dictionary<string, int>();
        // InstantiateAsync로 생성한 GameObject 인스턴스. 그 인스턴스를 만든 핸들
        // Addressables.ReleaseInstance를 호출할 때 원래 핸들이 필요
        private readonly Dictionary<GameObject, AsyncOperationHandle> _instanceHandles = new Dictionary<GameObject, AsyncOperationHandle>();
        // 비동기/멀티스레드 상황에서 딕셔너리 동시 접근을 직렬화하기 위한 lock 오브젝트
        private readonly object _lock = new object();
        // 어드레서블 시스템의 비동기 초기화 작업이 완료되었음을 알리는 역할
        // - 성공: TrySetResult(true)
        // - 실패: TrySetException(ex)
        private TaskCompletionSource<bool> _initTcs = new TaskCompletionSource<bool>();
        // 초기화가 완료되었는지 여부를 나타내는 플래그
        private bool _initialized = false;

        protected override void Awake()
        {
            base.Awake();
            // 비동기 초기화 메소드를 호출합니다. `_ = `는 '이 작업의 결과를 기다리지 않고 일단 시작만 시켜라'라는 의미(fire-and-forget 패턴)
            _ = InitializeAsyncInternal();
        }

        // 초기화 보장 함수
        // 외부 API(Load/Instantiate/LoadScene) 진입 시, Addressables가 초기화되었는지 보장
        private async Task EnsureInitializedAsync()
        {
            if (_initialized) return;
            Debug.Log("[ResourceManager] EnsureInitializedAsync 대기중...");
            // 아직 초기화 중이라면, _initTcs.Task가 완료될 때(신호등이 켜질 때)까지 여기서 비동기적으로 대기
            await _initTcs.Task;
        }

        // 어드레서블 시스템을 실제로 초기화하는 내부 비동기 메소드
        public async Task InitializeAsyncInternal()
        {
            Debug.Log("[ResourceManager] InitializeAsyncInternal 시작");
            try
            {
                // Addressables 시스템 초기화 비동기 핸들 획득
                var initHandle = Addressables.InitializeAsync();
                try
                {
                    // 내부적으로 어드레서블 설정, 카탈로그 로드 등 수행
                    await initHandle.Task;
                }
                catch (Exception ex)
                {
                    // 핸들.Task 대기 중 발생할 수 있는 예외 로깅
                    Debug.LogError($"[ResourceManager] Addressables.InitializeAsync 예외: {ex}");
                }

                // 핸들이 유효한지 점검 (에디터 특정 모드에서 무효일 가능성 있음)
                if (initHandle.IsValid())
                {
                    if (initHandle.Status == AsyncOperationStatus.Succeeded)
                        Debug.Log("[ResourceManager] Addressables 초기화 성공.");
                    else
                        Debug.LogError($"[ResourceManager] Addressables 초기화 실패: {initHandle.OperationException}");
                    // 초기화 핸들은 더 이상 필요 없으므로 안전하게 릴리스
                    try { Addressables.Release(initHandle); } catch { }
                }
                else
                {
                    // 에디터 Play Mode Script가 Asset Database 모드일 때 등 무효 핸들이 돌아올 수 있음
                    Debug.LogWarning("[ResourceManager] InitializeAsync 반환 핸들이 유효하지 않습니다. (에디터의 Play Mode Script 설정에 의해 발생할 수 있음) " +
                                     "필요시 Addressables 설정(Play Mode Script / Content build)을 확인하세요.");
                    // 핸들이 무효여도 에디터 DB 모드 등에서 정상 동작할 수 있으므로 초기화 성공으로 처리
                }

                // 초기화 성공 플래그 설정 및 대기자들에게 완료 신호
                _initialized = true;
                _initTcs.TrySetResult(true);
            }
            catch (Exception ex)
            {
                // 초기화 단계에서 예기치 못한 예외 발생 시 실패 신호 전파
                Debug.LogError($"[ResourceManager] InitializeAsyncInternal 최종 예외: {ex}");
                _initTcs.TrySetException(ex);
            }
        }

        // 에셋 로드
        // 주소(address)에 해당하는 에셋을 비동기 로드합니다. 중복 요청 시 동일 핸들을 재사용하며, 참조 카운트를 증가
        public async Task<T> LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            // 1) Addressables 초기화 보장
            await EnsureInitializedAsync();

            AsyncOperationHandle handleToAwait; // 이 메서드 밖에서도 대기할 핸들을 담아둘 변수

            // 2) 공유 상태(딕셔너리) 접근은 lock으로 보호
            lock (_lock)
            {
                // 이미 로드(또는 로드 중)인 경우 기존 핸들 재사용 + refCount 증가
                if (_handles.TryGetValue(address, out var cachedHandle))
                {
                    // 있다면, 참조 횟수를 올리고 해당 핸들을 반환하여 lock 밖에서 await 하도록 합니다.
                    _refCounts[address]++;
                    Debug.Log($"[ResourceManager] 기존 핸들 반환 (참조: {_refCounts[address]}): {address}");
                    handleToAwait = cachedHandle;
                }
                else
                {
                    // 처음 요청: 새 로드 시작, 핸들과 refCount 기록
                    var newHandle = Addressables.LoadAssetAsync<T>(address);
                    _handles[address] = newHandle;
                    _refCounts[address] = 1; // 첫 요청이므로 참조 횟수는 1
                    handleToAwait = newHandle;
                }
            }

            // 3) 로드 완료까지 비동기 대기 (예외 발생 가능)
            try
            {
                await handleToAwait.Task;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManager] 에셋 로드중 예외: {address} => {ex}");
            }

            // 4) 안정성 검사: 핸들 유효성
            if (!handleToAwait.IsValid())
            {
                Debug.LogError($"[ResourceManager] 핸들이 유효하지 않습니다: {address}");
                Release(address);
                return null;
            }

            // 5) 로드 결과 확인
            if (handleToAwait.Status == AsyncOperationStatus.Succeeded)
            {
                try
                {
                    // Convert<T>()로 형 변환 후 결과 반환
                    var converted = _handles[address].Convert<T>();
                    return converted.Result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ResourceManager] 변환 또는 결과 접근 실패: {address} => {ex}");
                    return null;
                }
            }
            else
            {
                // 실패 시 로그 후 정리
                Debug.LogError($"[ResourceManager] 에셋 로드 실패: {address}, 이유: {handleToAwait.OperationException}");
                Release(address);
                return null;
            }
        }

        // 인스턴스 생성
        // 프리팹 주소를 씬에 인스턴스화
        public async Task<GameObject> InstantiateAsync(string address, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrWhiteSpace(address)) return null; // 잘못된 주소 방어

            // 인스턴스 생성 전에도 초기화 보장
            await EnsureInitializedAsync();

            // Instantiate 용 제네릭 핸들 (GameObject)
            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, position, rotation);
            try
            {
                await handle.Task; // 생성 완료 대기
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManager] InstantiateAsync 예외: {address} => {ex}");
            }

            // 유효성/상태 점검
            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError($"[ResourceManager] Instantiate 실패: {address}, ex: {handle.OperationException}");
                // 실패 시 핸들 정리 시도
                try { Addressables.Release(handle); } catch { }
                return null;
            }

            // 성공 시 결과 반환
            var go = handle.Result;
            lock (_lock)
            {
                _instanceHandles[go] = handle; // 추적 등록 (비추적 파괴 방지)
            }
            return go;
        }

        // 에셋 해제 (Release)
        // LoadAsync로 로드된 에셋의 참조를 하나 해제합니다. 참조가 0이 되면 메모리에서 해제
        public void Release(string address)
        {
            if (string.IsNullOrWhiteSpace(address)) return; // 방어 코드

            lock (_lock)
            {
                // 기록된 핸들이 없으면 경고만 남기고 반환
                if (!_handles.TryGetValue(address, out var handle))
                {
                    Debug.LogWarning($"[ResourceManager] Release 시도했으나 핸들 없음: {address}");
                    return;
                }

                // refCount가 없으면 비정상 상태이므로 바로 Release 시도
                if (!_refCounts.ContainsKey(address))
                {
                    // 안전 장치
                    Debug.LogWarning($"[ResourceManager] Release: refCount 기록 없음, 바로 릴리즈: {address}");
                    if (handle.IsValid()) Addressables.Release(handle);
                    _handles.Remove(address);
                    return;
                }

                // 참조 하나 감소
                _refCounts[address]--;
                Debug.Log($"[ResourceManager] Release 호출, 남은 refCount={_refCounts[address]} : {address}");

                // 참조가 0 이하이면 실제 해제
                if (_refCounts[address] <= 0)
                {
                    if (handle.IsValid())
                    {
                        try { Addressables.Release(handle); }
                        catch (Exception ex) { Debug.LogWarning($"[ResourceManager] Addressables.Release 예외: {ex}"); }
                    }

                    // 테이블에서 삭제
                    _handles.Remove(address);
                    _refCounts.Remove(address);
                }
            }
        }


        // 인스턴스 해제 (ReleaseInstance)
        // InstantiateAsync로 생성된 게임 오브젝트를 안전하게 파괴합니다.
        // _instanceHandles에 추적된 경우 Addressables.ReleaseInstance를 사용
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return; // 방어 코드

            lock (_lock)
            {
                if (_instanceHandles.TryGetValue(instance, out var handle))
                {
                    // 추적된 핸들이 있다면 Addressables.ReleaseInstance로 반환
                    Addressables.ReleaseInstance(handle);
                    _instanceHandles.Remove(instance);
                }
                else
                {
                    // 추적되지 않았다면 일반 Destroy 사용 (주소 정보 손실 상태일 수 있음)
                    Debug.LogWarning($"[ResourceManager] 추적되지 않은 인스턴스 파괴 시도: {instance.name}");
                    Destroy(instance);
                }
            }
        }

        // 씬 로드
        // LoadSceneMode
        // single : 새로운 씬을 로드하기 전에 현재 열려있는 모든 씬을 닫음
        // Additive : 현재 열려있는 씬을 그대로 둔 채, 그 위에 새로운 씬을 추가로 불러옴
        public async Task<SceneInstance?> LoadSceneAsync(string sceneAddress, LoadSceneMode mode = LoadSceneMode.Single)
        {
            if (string.IsNullOrWhiteSpace(sceneAddress)) return null;

            await EnsureInitializedAsync();// 초기화 보장

            // 씬 로드는 Addressables.LoadSceneAsync 사용
            var handle = Addressables.LoadSceneAsync(sceneAddress, mode);
            try
            {
                await handle.Task; // 로드 완료 대기
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceManager] LoadSceneAsync 예외: {sceneAddress} => {ex}");
            }

            if (!handle.IsValid())
            {
                Debug.LogError($"[ResourceManager] Scene 핸들 유효하지 않음: {sceneAddress}");
                try { Addressables.Release(handle); } catch { }
                return null;
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                // 성공 시 SceneInstance 반환 (nullable)
                return handle.Result;
            }
            else
            {
                Debug.LogError($"[ResourceManager] Scene 로드 실패: {sceneAddress}, ex: {handle.OperationException}");
                try { Addressables.Release(handle); } catch { }
                return null;
            }
        }
    }
}
