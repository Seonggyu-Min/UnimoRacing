using Firebase.Auth;
using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG
{
    public static class AccountDeletion
    {
        /// <summary>
        /// 회원 탈퇴를 하기 위해 사용하는 메서드입니다. Task가 잘 완료 되었을 때, 로그인 씬으로 보내면 됩니다.
        /// </summary>
        /// <param name="extendedCleanup">다른 User의 경로에서도 내 정보 삭제할 지 여부입니다.</param>
        /// <returns></returns>
        public static async Task<bool> DeleteAccountAsync(bool extendedCleanup = true)
        {
            var auth = FirebaseAuth.DefaultInstance;
            var user = auth.CurrentUser;
            if (user == null)
            {
                Debug.LogError("[DeleteMyAccount] Not signed in.");
                return false;
            }

            string uid = user.UserId;

            try
            {
                // RequiresRecentLogin 요구하면 받아야 될 듯
                //if (!string.IsNullOrEmpty(emailForReauth) && !string.IsNullOrEmpty(passwordForReauth))
                //{
                //    var cred = EmailAuthProvider.GetCredential(emailForReauth, passwordForReauth);
                //    await user.ReauthenticateAsync(cred);
                //}

                // 1) 삭제에 필요한 정보 수집
                // 닉네임
                string nickname = null;
                var nickSnap = await GetAsync(DBRoutes.Nickname(uid));
                if (nickSnap.Exists) nickname = nickSnap.Value?.ToString();

                // 내 친구 목록
                List<string> friends = new();
                var myFriendsSnap = await GetAsync(DBRoutes.FriendListRoot(uid));
                if (myFriendsSnap.Exists)
                    friends = myFriendsSnap.Children.Select(ch => ch.Key).ToList();

                // 내 pairId 인덱스
                List<string> pairIds = new();
                var pairIndexSnap = await GetAsync(DBRoutes.Nicknames(nickname));
                if (pairIndexSnap.Exists)
                {
                    pairIds = pairIndexSnap.Children.Select(ch => ch.Key).ToList();
                }

                // 2) DB 멀티 삭제 맵 구성
                var updates = new Dictionary<string, object>
                {
                    // 내 사용자 트리 전체
                    [DBRoutes.Users(uid)] = null,

                    // presence
                    [DBRoutes.Presence(uid)] = null,

                    // inbox / outbox
                    [DBRoutes.InBoxRoot(uid)] = null,
                    [DBRoutes.OutBoxRoot(uid)] = null,
                };

                // 닉네임 역인덱스
                if (!string.IsNullOrEmpty(nickname))
                {
                    updates[DBRoutes.Nicknames(nickname)] = null;
                }

                if (extendedCleanup)
                {
                    // 상대방 친구목록에서 나 제거
                    foreach (var otherUid in friends)
                    {
                        updates[DBRoutes.Friend(otherUid, uid)] = null;
                    }

                    // 친구 링크 및 내 역인덱스 제거
                    foreach (var pairId in pairIds)
                    {
                        updates[DBRoutes.FriendLinks(pairId)] = null;
                    }
                }

                // 3) 멀티 업데이트 실행
                await UpdateAsync(updates);

                // 4) Auth 계정 삭제
                await user.DeleteAsync();

                Debug.Log("[DeleteMyAccount] 삭제 완료.");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DeleteMyAccount] 삭제 실패: {e}");
                return false;
            }
        }


        private static Task<DataSnapshot> GetAsync(string path)
        {
            var tcs = new TaskCompletionSource<DataSnapshot>();
            DatabaseManager.Instance.GetOnMain(
                path,
                snap => tcs.TrySetResult(snap),
                err => tcs.TrySetException(new Exception(err ?? "Get 실패")));
            return tcs.Task;
        }

        private static Task UpdateAsync(Dictionary<string, object> updates)
        {
            var tcs = new TaskCompletionSource<object>();
            DatabaseManager.Instance.UpdateOnMain(
                updates,
                () => tcs.TrySetResult(null),
                err => tcs.TrySetException(new Exception(err ?? "Update 실패")));
            return tcs.Task;
        }
    }
}
