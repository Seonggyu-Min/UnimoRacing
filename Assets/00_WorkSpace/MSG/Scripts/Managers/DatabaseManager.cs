using Firebase.Database;
using Firebase.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG
{
    public class DatabaseManager : Singleton<DatabaseManager>
    {
        #region Properties

        private FirebaseDatabase DB
        {
            get
            {
                if (FirebaseManager.Instance == null)
                {
                    throw new InvalidOperationException("FirebaseManager instance is null");
                }
                if (!FirebaseManager.Instance.IsReady)
                {
                    throw new InvalidOperationException("FirebaseManager is not ready yet. Please wait until it is initialized.");
                }

                return FirebaseManager.Instance.Database;
            }
        }

        #endregion


        #region Unity Methods

        private void Awake()
        {
            SingletonInit();
        }

        #endregion


        #region Private Methods
        // 순차 실행, 동시 실행, 타임아웃, 리트라이 등을 구현할 수 있기에 분리해둠

        // ------- 기본 읽기/쓰기 -------

        // 지정된 경로의 데이터를 가져옵니다.
        private async Task<DataSnapshot> GetAsync(string path)
        {
            var snap = await DB.GetReference(path).GetValueAsync();
            return snap;
        }

        // 지정된 경로의 데이터를 덮어씁니다.
        private async Task SetAsync(object value, string path)
        {
            await DB.GetReference(path).SetValueAsync(value);
        }

        // 여러 경로를 동시에 업데이트합니다.
        private async Task UpdateAsync(Dictionary<string, object> updatesByPath)
        {
            await DB.RootReference.UpdateChildrenAsync(updatesByPath);
        }

        // 해당 경로의 노드를 삭제합니다.
        private async Task RemoveAsync(string path)
        {
            await DB.GetReference(path).RemoveValueAsync();
        }

        // ------- 트랜잭션 처리 -------

        // 지정된 경로에서 트랜잭션을 실행합니다.
        private Task<DataSnapshot> RunTransactionAsync(Func<MutableData, TransactionResult> handler, string path, bool fireLocalEvents = false)
        {
            return DB.GetReference(path).RunTransaction(handler, fireLocalEvents);
        }

        // 지정된 경로의 숫자를 증감시킵니다.
        private Task<DataSnapshot> IncrementAsync(long delta, string path)
        {
            return RunTransactionAsync(mutable =>
            {
                long current = 0;
                try
                {
                    if (mutable.Value != null)
                    {
                        current = Convert.ToInt64(mutable.Value);
                    }
                }
                catch
                {
                    // 파싱 실패 시 0으로 간주
                }

                mutable.Value = current + delta;
                return TransactionResult.Success(mutable);
            }, path);
        }

        #endregion


        #region Log Methods

        private static void LogCanceled(string op) => Debug.LogWarning($"[DB] {op} 취소됨");
        private static void LogError(string op, Exception ex) => Debug.LogError($"[DB] {op} 실패: {ex}");
        private static void LogSuccess(string op, string extra = null)
            => Debug.Log(string.IsNullOrEmpty(extra) ? $"[DB] {op} 성공" : $"[DB] {op} 성공: {extra}");

        #endregion


        #region Public API Methods

        /// <summary>
        /// 지정된 경로의 데이터를 가져옵니다.
        /// </summary>
        /// <param name="path">경로 string을 전달합니다. DBRoutes를 사용하여 전달하는 것이 좋습니다.</param>
        /// <param name="onSuccess">읽기에 성공했을 때 호출됩니다. (Optional Parameter)</param>
        /// <param name="onError">읽기에 실패했을 때 에러 메시지를 전달합니다. (Optional Parameter)</param>
        public void GetOnMain(string path, Action<DataSnapshot> onSuccess = null, Action<string> onError = null)
        {
            var op = $"Get ({path}) ";
            try
            {
                GetAsync(path).ContinueWithOnMainThread(t =>
                {
                    if (t.IsCanceled)
                    {
                        LogCanceled(op);
                        onError?.Invoke("Canceled");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        LogError(op, t.Exception);
                        if (onError != null)
                        {
                            string msg;
                            if (t.Exception != null && t.Exception.Message != null)
                            {
                                msg = t.Exception.Message;
                            }
                            else
                            {
                                msg = "Error";
                            }
                            onError(msg);
                        }
                        return;
                    }

                    LogSuccess(op, t.Result.Exists ? t.Result.GetRawJsonValue() : "null");
                    onSuccess?.Invoke(t.Result);
                });
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// 지정된 경로에 데이터를 덮어씁니다.
        /// </summary>
        /// <param name="path">경로 string을 전달합니다. DBRoutes를 사용하여 전달하는 것이 좋습니다.</param>
        /// <param name="value">저장할 값을 전달합니다.</param>
        /// <param name="onSuccess">읽기에 성공했을 때 호출됩니다. (Optional Parameter)</param>
        /// <param name="onError">읽기에 실패했을 때 에러 메시지를 전달합니다. (Optional Parameter)</param>
        public void SetOnMain(string path, object value, Action onSuccess = null, Action<string> onError = null)
        {
            var op = $"Set {path}) ";
            try
            {
                SetAsync(value, path).ContinueWithOnMainThread(t =>
                {
                    if (t.IsCanceled)
                    {
                        LogCanceled(op);
                        onError?.Invoke("Canceled");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        LogError(op, t.Exception);
                        if (onError != null)
                        {
                            string msg;
                            if (t.Exception != null && t.Exception.Message != null)
                            {
                                msg = t.Exception.Message;
                            }
                            else
                            {
                                msg = "Error";
                            }
                            onError(msg);
                        }
                        return;
                    }

                    LogSuccess(op);
                    onSuccess?.Invoke();
                });
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// 여러 경로를 동시에 업데이트합니다.
        /// </summary>
        /// <param name="updateByPaths">경로 string을 전달합니다. DBRoutes를 사용하여 전달하는 것이 좋습니다.</param>
        /// <param name="onSuccess">읽기에 성공했을 때 호출됩니다. (Optional Parameter)</param>
        /// <param name="onError">읽기에 실패했을 때 에러 메시지를 전달합니다. (Optional Parameter)</param>
        public void UpdateOnMain(Dictionary<string, object> updateByPaths, Action onSuccess = null, Action<string> onError = null)
        {
            const string op = "Update ";
            try
            {
                UpdateAsync(updateByPaths).ContinueWithOnMainThread(t =>
                {
                    if (t.IsCanceled)
                    {
                        LogCanceled(op);
                        onError?.Invoke("Canceled");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        LogError(op, t.Exception);
                        if (onError != null)
                        {
                            string msg;
                            if (t.Exception != null && t.Exception.Message != null)
                            {
                                msg = t.Exception.Message;
                            }
                            else
                            {
                                msg = "Error";
                            }
                            onError(msg);
                        }
                        return;
                    }

                    LogSuccess(op);
                    onSuccess?.Invoke();
                });
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// 지정된 경로의 노드를 삭제합니다.
        /// </summary>
        /// <param name="path">경로 string을 전달합니다. DBRoutes를 사용하여 전달하는 것이 좋습니다.</param>
        /// <param name="onSuccess">읽기에 성공했을 때 호출됩니다. (Optional Parameter)</param>
        /// <param name="onError">읽기에 실패했을 때 에러 메시지를 전달합니다. (Optional Parameter)</param>
        public void RemoveOnMain(string path, Action onSuccess = null, Action<string> onError = null)
        {
            var op = $"Remove ({path}) ";
            try
            {
                RemoveAsync(path).ContinueWithOnMainThread(t =>
                {
                    if (t.IsCanceled)
                    {
                        LogCanceled(op);
                        onError?.Invoke("Canceled");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        LogError(op, t.Exception);
                        if (onError != null)
                        {
                            string msg;
                            if (t.Exception != null && t.Exception.Message != null)
                            {
                                msg = t.Exception.Message;
                            }
                            else
                            {
                                msg = "Error";
                            }
                            onError(msg);
                        }
                        return;
                    }

                    LogSuccess(op);
                    onSuccess?.Invoke();
                });
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// 지정된 경로의 숫자를 트랜잭션을 통해 증감시킵니다.
        /// </summary>
        /// <param name="path">경로 string을 전달합니다. DBRoutes를 사용하여 전달하는 것이 좋습니다.</param>
        /// <param name="delta">증감 값을 전달합니다. 음수를 전달할 경우 감소합니다.</param>
        /// <param name="onSuccess">읽기에 성공했을 때 호출됩니다. (Optional Parameter)</param>
        /// <param name="onError">읽기에 실패했을 때 에러 메시지를 전달합니다. (Optional Parameter)</param>
        public void IncrementOnMainWithTransaction(string path, long delta, Action<DataSnapshot> onSuccess = null, Action<string> onError = null)
        {
            var op = $"Increment ({path}) by {delta} ";
            try
            {
                IncrementAsync(delta, path).ContinueWithOnMainThread(t =>
                {
                    if (t.IsCanceled)
                    {
                        LogCanceled(op);
                        onError?.Invoke("Canceled");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        LogError(op, t.Exception);
                        if (onError != null)
                        {
                            string msg;
                            if (t.Exception != null && t.Exception.Message != null)
                            {
                                msg = t.Exception.Message;
                            }
                            else
                            {
                                msg = "Error";
                            }
                            onError(msg);
                        }
                        return;
                    }

                    LogSuccess(op, t.Result?.Value?.ToString());
                    onSuccess?.Invoke(t.Result);
                });
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// 지정된 경로의 숫자를 트랜잭션을 통해 증감시킨 후 결과값을 long으로 변환하여 반환합니다.
        /// </summary>
        /// <param name="path">경로 string을 전달합니다. DBRoutes를 사용하여 전달하는 것이 좋습니다.</param>
        /// <param name="delta">증감 값을 전달합니다. 음수를 전달할 경우 감소합니다.</param>
        /// <param name="onSuccess">읽기에 성공했을 때 호출됩니다. (Optional Parameter)</param>
        /// <param name="onError">읽기에 실패했을 때 에러 메시지를 전달합니다. (Optional Parameter)</param>
        public void IncrementToLongOnMainWithTransaction(string path, long delta, Action<long> onSuccess = null, Action<string> onError = null)
        {
            IncrementOnMainWithTransaction(path, delta,
                onSuccess: snap =>
                {
                    long v = 0;
                    try
                    {
                        if (snap != null && snap.Exists && snap.Value != null)
                        {
                            long.TryParse(snap.Value.ToString(), out v);
                        }
                    }
                    catch
                    {
                        // 파싱 실패 시 기본값 0으로 설정
                    }

                    onSuccess?.Invoke(v);
                },
                onError: onError);
        }

        /// <summary>
        /// 사용자 정의 트랜잭션을 실행한 후 결과 DataSnapshot을 반환합니다.
        /// </summary>
        /// <param name="path">경로 string을 전달합니다. DBRoutes를 사용하여 전달하는 것이 좋습니다.</param>
        /// <param name="handler">MutableData를 수정한 뒤 TransactionResult.Success(MutableData) 또는 TransactionResult.Abort()를 반환하는 Func을 전달하세요.</param>
        /// <param name="onSuccess">읽기에 성공했을 때 호출됩니다. (Optional Parameter)</param>
        /// <param name="onError">읽기에 실패했을 때 에러 메시지를 전달합니다. (Optional Parameter)</param>
        /// <param name="fireLocalEvents"></param>
        public void RunTransactionOnMain(string path, Func<MutableData, TransactionResult> handler, Action<DataSnapshot> onSuccess = null, Action<string> onError = null, bool fireLocalEvents = false)
        {
            var op = $"Transaction ({path}) ";
            try
            {
                RunTransactionAsync(handler, path, fireLocalEvents).ContinueWithOnMainThread(t =>
                {
                    if (t.IsCanceled)
                    {
                        LogCanceled(op);
                        onError?.Invoke("Canceled");
                        return;
                    }
                    if (t.IsFaulted)
                    {
                        LogError(op, t.Exception);
                        if (onError != null)
                        {
                            string msg;
                            if (t.Exception != null && t.Exception.Message != null)
                            {
                                msg = t.Exception.Message;
                            }
                            else
                            {
                                msg = "Error";
                            }

                            onError(msg);
                        }
                        return;
                    }

                    LogSuccess(op, t.Result?.Value?.ToString());
                    onSuccess?.Invoke(t.Result);
                });
            }
            catch (Exception e)
            {
                onError?.Invoke(e.Message);
            }
        }

        // TODO: 타임아웃, 리스너 등록 해제, 클래스 오브젝트 직렬화?
        #endregion
    }
}
