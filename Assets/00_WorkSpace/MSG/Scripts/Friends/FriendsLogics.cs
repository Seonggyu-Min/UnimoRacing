using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// 팬아웃을 통해 읽기를 빠르게 하는 동시에, 여러 곳의 데이터를 동일하게 보장하기 위해서 원자적으로 Update 수행하기 위한 친구 로직 클래스입니다.
    /// </summary>
    public class FriendsLogics : SceneSingleton<FriendsLogics>
    {
        /// <summary>
        /// 친구 요청을 보내는 메서드입니다.
        /// </summary>
        /// <param name="fromUid">발신인의 uid</param>
        /// <param name="toUid">수신인의 uid</param>
        /// <param name="onSuccess">성공 시 받을 Action (Optional Parameter)</param>
        /// <param name="onError">실패 시 받을 Action (Optional Parameter)</param>
        public void SendRequest(string fromUid, string toUid, Action onSuccess = null, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(fromUid) || string.IsNullOrEmpty(toUid))
            {
                onError?.Invoke("fromUid 또는 toUid가 비어있습니다");
                return;
            }
            if (fromUid == toUid)
            {
                onError?.Invoke("자기 자신에게 친구 요청을 보낼 수 없습니다.");
                return;
            }

            string pairId = DBPathMaker.ComposePairId(fromUid, toUid);
            string link = DBRoutes.FriendLinks(pairId);

            DatabaseManager.Instance.GetOnMain(link, snap =>
            {
                if (snap.Exists)
                {
                    string currentStatus = snap.Child(DatabaseKeys.status).Value?.ToString();

                    if (currentStatus == DatabaseKeys.accepted) // 이미 친구라면 return
                    {
                        onSuccess?.Invoke();
                        return;
                    }
                    else if (currentStatus == DatabaseKeys.pending)  // 이미 요청을 보냈다면 return
                    {
                        onError?.Invoke("이미 요청을 보냈습니다.");
                        return;
                    }
                    else if (currentStatus == DatabaseKeys.rejected) // 이미 거절된 상태라면 (추가 작업 필요시)
                    {
                        // 요청이 거절된 상태에서 다시 요청을 보내는 경우
                        // 만약 이미 거절된 상태에서 다시 요청 못보내게 하려면 여기서 추가 처리 해야될 듯
                    }
                }

                Dictionary<string, object> updates = new()
                {
                    { DBPathMaker.Join(link, DatabaseKeys.from), fromUid },
                    { DBPathMaker.Join(link, DatabaseKeys.to), toUid },
                    { DBPathMaker.Join(link, DatabaseKeys.status), DatabaseKeys.pending },
                    { DBPathMaker.Join(link, DatabaseKeys.requestedAt), ServerValue.Timestamp },

                    { DBPathMaker.Join(DBRoutes.OutBox(fromUid, pairId), DatabaseKeys.status), DatabaseKeys.pending },
                    { DBPathMaker.Join(DBRoutes.OutBox (fromUid, pairId), DatabaseKeys.requestedAt), ServerValue.Timestamp },

                    { DBPathMaker.Join(DBRoutes.InBox (toUid, pairId), DatabaseKeys.status), DatabaseKeys.pending },
                    { DBPathMaker.Join(DBRoutes.InBox (toUid, pairId), DatabaseKeys.requestedAt), ServerValue.Timestamp }
                };

                DatabaseManager.Instance.UpdateOnMain(updates, onSuccess, onError);
            }, onError);
        }

        /// <summary>
        /// 친구 요청을 수락하는 메서드입니다.
        /// </summary>
        /// <param name="pairId">수신인과 발신인 uid의 pair 값</param>
        /// <param name="actorUid">요청을 수락하는 클라이언트의 uid</param>
        /// <param name="onSuccess">성공 시 받을 Action (Optional Parameter)</param>
        /// <param name="onError">실패 시 받을 Action (Optional Parameter)</param>
        public void AcceptRequest(string pairId, string actorUid, Action onSuccess = null, Action<string> onError = null) 
        {
            if (string.IsNullOrEmpty(pairId) || string.IsNullOrEmpty(actorUid))
            {
                onError?.Invoke("pairId 또는 actorUid가 비어있습니다");
                return;
            }

            string link = DBRoutes.FriendLinks(pairId);
            DatabaseManager.Instance.GetOnMain(link, snap =>
            {
                if (snap.Exists)
                {
                    string fromUid = snap.Child(DatabaseKeys.from).Value?.ToString();
                    string toUid = snap.Child(DatabaseKeys.to).Value?.ToString();

                    if (string.IsNullOrEmpty(toUid))
                    {
                        onError?.Invoke("수신자 uid가 비어있습니다.");
                        return;
                    }
                    if (actorUid != toUid) 
                    { 
                        onError?.Invoke("수신자만 수락할 수 있습니다."); 
                        return; 
                    }

                    string currentStatus = snap.Child(DatabaseKeys.status).Value?.ToString();

                    Dictionary<string, object> updates = new();

                    if (currentStatus != DatabaseKeys.accepted)
                    {
                        updates[DBPathMaker.Join(link, DatabaseKeys.status)] = DatabaseKeys.accepted;
                        updates[DBPathMaker.Join(link, DatabaseKeys.acceptedAt)] = ServerValue.Timestamp;
                    }

                    updates[DBPathMaker.Join(DBRoutes.OutBox(fromUid, pairId), DatabaseKeys.status)] = DatabaseKeys.accepted;
                    updates[DBPathMaker.Join(DBRoutes.InBox(toUid, pairId), DatabaseKeys.status)] = DatabaseKeys.accepted;

                    // 양쪽 리스트 캐시 (간단히 true로 저장)
                    updates[DBRoutes.Friend(fromUid, toUid)] = true;
                    updates[DBRoutes.Friend(toUid, fromUid)] = true;

                    if (updates.Count == 0)
                    {
                        // 바꿀 게 없더라도 리스트는 이미 채워져 있다고 가정 → 완료 콜백
                        onSuccess?.Invoke();
                        return;
                    }

                    DatabaseManager.Instance.UpdateOnMain(updates, onSuccess, onError);
                }
                else
                {
                    onError?.Invoke("해당 친구 요청이 존재하지 않습니다.");
                }
            }, onError);
        }

        /// <summary>
        /// 친구 요청을 거절하는 메서드입니다.
        /// </summary>
        /// <param name="pairId">수신인과 발신인 uid의 pair 값</param>
        /// <param name="actorUid">요청을 거절하는 클라이언트의 uid</param>
        /// <param name="onSuccess">성공 시 받을 Action (Optional Parameter)</param>
        /// <param name="onError">실패 시 받을 Action (Optional Parameter)</param>
        public void RejectRequest(string pairId, string actorUid, Action onSuccess = null, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(pairId) || string.IsNullOrEmpty(actorUid))
            {
                onError?.Invoke("pairId 또는 actorUid가 비어있습니다");
                return;
            }

            string link = DBRoutes.FriendLinks(pairId);
            DatabaseManager.Instance.GetOnMain(link, snap =>
            {
                if (snap.Exists)
                {
                    string fromUid = snap.Child(DatabaseKeys.from).Value?.ToString();
                    string toUid = snap.Child(DatabaseKeys.to).Value?.ToString();

                    if (string.IsNullOrEmpty(toUid))
                    {
                        onError?.Invoke("수신자 uid가 비어있습니다.");
                        return;
                    }
                    if (actorUid != toUid) 
                    { 
                        onError?.Invoke("수신자만 거절할 수 있습니다."); 
                        return;
                    }

                    string currentStatus = snap.Child(DatabaseKeys.status).Value?.ToString();
                    if (currentStatus == DatabaseKeys.rejected) // 이미 거부했다면 return
                    {
                        onSuccess?.Invoke();
                        return;
                    }
                    else if (currentStatus == DatabaseKeys.pending) // 요청이 대기 중이라면 거절 처리 시작
                    {
                        Dictionary<string, object> updates = new()
                        {
                            { DBPathMaker.Join(link, DatabaseKeys.status), DatabaseKeys.rejected },
                            { DBPathMaker.Join(DBRoutes.OutBox(fromUid, pairId), DatabaseKeys.status), DatabaseKeys.rejected },
                            { DBPathMaker.Join(DBRoutes.InBox(toUid, pairId), DatabaseKeys.status), DatabaseKeys.rejected }
                        };

                        DatabaseManager.Instance.UpdateOnMain(updates, onSuccess, onError);
                    }
                    else
                    {
                        onError?.Invoke($"요청 상태가 올바르지 않습니다. 요청 상태: {currentStatus}");
                    }
                }
                else
                {
                    onError?.Invoke("해당 친구 요청이 존재하지 않습니다.");
                }
            }, onError);
        }

        /// <summary>
        /// 친구 요청을 취소하는 메서드입니다.
        /// </summary>
        /// <param name="pairId">수신인과 발신인 uid의 pair 값</param>
        /// <param name="actorUid">요청을 취소하는 클라이언트의 uid</param>
        /// <param name="onSuccess">성공 시 받을 Action (Optional Parameter)</param>
        /// <param name="onError">실패 시 받을 Action (Optional Parameter)</param>
        public void CancelRequest(string pairId, string actorUid, Action onSuccess = null, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(pairId) || string.IsNullOrEmpty(actorUid))
            {
                onError?.Invoke("pairId 또는 actorUid가 비어있습니다");
                return;
            }

            string link = DBRoutes.FriendLinks(pairId);
            DatabaseManager.Instance.GetOnMain(link, snap =>
            {
                if (snap.Exists)
                {
                    string fromUid = snap.Child(DatabaseKeys.from).Value?.ToString();
                    string toUid = snap.Child(DatabaseKeys.to).Value?.ToString();

                    if (string.IsNullOrEmpty(fromUid))
                    {
                        onError?.Invoke("발신자 uid가 비어있습니다.");
                        return;
                    }
                    if (actorUid != fromUid) 
                    { 
                        onError?.Invoke("발신자만 요청을 취소할 수 있습니다."); 
                        return;
                    }

                    string currentStatus = snap.Child(DatabaseKeys.status).Value?.ToString();
                    if (currentStatus == DatabaseKeys.cancelled) // 이미 취소되었다면 return
                    {
                        onSuccess?.Invoke();
                        return;
                    }
                    if (currentStatus == DatabaseKeys.pending) // 요청이 대기 중이라면 취소 처리 시작
                    {
                        Dictionary<string, object> updates = new()
                        {
                            { DBPathMaker.Join(link, DatabaseKeys.status), DatabaseKeys.cancelled },
                            { DBPathMaker.Join(DBRoutes.OutBox(fromUid, pairId), DatabaseKeys.status), DatabaseKeys.cancelled },
                            { DBPathMaker.Join(DBRoutes.InBox(toUid, pairId), DatabaseKeys.status), DatabaseKeys.cancelled }
                        };

                        DatabaseManager.Instance.UpdateOnMain(updates, onSuccess, onError);
                    }
                    else
                    {
                        onError?.Invoke($"요청 상태가 올바르지 않습니다. 요청 상태: {currentStatus}");
                    }
                }
                else
                {
                    onError?.Invoke("해당 친구 요청이 존재하지 않습니다.");
                }
            }, onError);
        }

        /// <summary>
        /// 친구를 제거하는 메서드입니다.
        /// </summary>
        /// <param name="pairId">수신인과 발신인 uid의 pair 값</param>
        /// <param name="actorUid">친구 제거를 요청하는 클라이언트의 uid</param>
        /// <param name="onSuccess">성공 시 받을 Action (Optional Parameter)</param>
        /// <param name="onError">실패 시 받을 Action (Optional Parameter)</param>
        public void RemoveFriend(string pairId, string actorUid, Action onSuccess = null, Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(pairId) || string.IsNullOrEmpty(actorUid))
            {
                onError?.Invoke("pairId 또는 actorUid가 비어있습니다");
                return;
            }

            string link = DBRoutes.FriendLinks(pairId);
            DatabaseManager.Instance.GetOnMain(link, snap =>
            {
                if (snap.Exists)
                {
                    string fromUid = snap.Child(DatabaseKeys.from).Value?.ToString();
                    string toUid = snap.Child(DatabaseKeys.to).Value?.ToString();

                    if (string.IsNullOrEmpty(fromUid) || string.IsNullOrEmpty(toUid))
                    {
                        onError?.Invoke("발신자 또는 수신자 uid가 비어있습니다.");
                        return;
                    }
                    if (actorUid != fromUid && actorUid != toUid) 
                    { 
                        onError?.Invoke("발신자 또는 수신자만 친구를 제거할 수 있습니다."); 
                        return; 
                    }

                    string currentStatus = snap.Child(DatabaseKeys.status).Value?.ToString();
                    if (currentStatus == DatabaseKeys.removed)
                    {
                        onSuccess?.Invoke();
                        return;
                    }
                    if (currentStatus != DatabaseKeys.accepted)
                    {
                        onError?.Invoke($"현재 상태({currentStatus})에서는 친구 제거할 수 없습니다.");
                        return;
                    }

                    Dictionary<string, object> updates = new()
                    {
                        { DBPathMaker.Join(link, DatabaseKeys.status), DatabaseKeys.removed },
                        { DBPathMaker.Join(DBRoutes.OutBox(fromUid, pairId), DatabaseKeys.status), DatabaseKeys.removed },
                        { DBPathMaker.Join(DBRoutes.InBox(toUid, pairId), DatabaseKeys.status), DatabaseKeys.removed },

                        { DBRoutes.Friend(fromUid, toUid), null },
                        { DBRoutes.Friend(toUid, fromUid), null }
                    };

                    DatabaseManager.Instance.UpdateOnMain(updates, onSuccess, onError);
                }
                else
                {
                    onError?.Invoke("해당 친구 관계가 존재하지 않습니다.");
                }
            }, onError);
        }
    }
}
