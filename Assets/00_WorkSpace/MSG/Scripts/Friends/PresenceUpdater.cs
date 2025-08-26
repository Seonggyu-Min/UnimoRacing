using Firebase.Database;
using Firebase.Extensions;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


namespace MSG
{
    // TODO: 추가적으로 파티에서 직접 나오도록 parties/{partyId}/members/{uid} 에서 자신 유저를 삭제하는 방식으로 파티 상태 업데이트 추가 해야됨
    public class PresenceUpdater : MonoBehaviour
    {
        [SerializeField] private float _heartbeatIntervalSeconds = 30f; // Heartbeat를 보내는 간격 (초 단위)
        [SerializeField] private float _killCountDownSeconds = 10f; // 앱이 포커스를 잃거나 종료될 때, 온라인 상태를 false로 전환하는 시간 (초 단위)

        private Coroutine _heartBeatCO;
        private Coroutine _killCountDownCO;

        private void OnEnable()
        {
            // uid가 존재한다는 보장이 있는, 로비 및 게임 씬에서 실행해야 됨
            StartHeartbeat();
        }

        private void OnDisable()
        {
            StopHeartbeat();
        }


        private string CurrentUid => FirebaseManager.Instance?.Auth.CurrentUser?.UserId;

        /// <summary>
        /// 플레이어의 온라인 상태를 업데이트합니다.
        /// 로그인 성공 시, 로그아웃 시 호출합니다
        /// </summary>
        /// <param name="isOnline">온라인 상태 여부를 전달합니다.</param>
        public void SetOnlineStatus(bool isOnline)
        {
            // 로그인 성공 시 online true 전환, lastSeen 업데이트
            // 로그아웃 시 online false 전환
            Dictionary<string, object> updates = new()
            {
                { DBRoutes.OnlineStatus(CurrentUid), isOnline },
                { DBRoutes.LastSeen(CurrentUid), ServerValue.Timestamp }
            };

            DatabaseManager.Instance.UpdateOnMain(
                updates,
                () => Debug.Log($"유저: {CurrentUid}의 온라인 상태 {isOnline} 로 전환 완료"),
                error => Debug.LogError($"유저: {CurrentUid}의 온라인 상태 전환 실패: {error}")
            );
        }

        /// <summary>
        /// 플레이어가 방에 있는지 여부를 업데이트합니다.
        /// 방에 들어갈 때와 나올 때마다 호출합니다.
        /// </summary>
        /// <param name="isInRoom">방에 있는지 여부를 전달합니다.</param>
        /// <param name="roomName">방 이름을 전달합니다. 방에 있지 않으면 전달하지 않아도 됩니다. (Optional Parameter)</param>
        public void SetInRoomStatus(bool isInRoom, string roomName = null)
        {
            // 큐 잡히면 inRoom true 전환, lastSeen 업데이트, roomName 업데이트
            // 큐 나오면 inRoom false 전환, lastSeen 업데이트, roomName null로 업데이트
            Dictionary<string, object> updates = new()
            {
                { DBRoutes.InRoomStatus(CurrentUid), isInRoom },
                { DBRoutes.LastSeen(CurrentUid), ServerValue.Timestamp },
                { DBRoutes.RoomName(CurrentUid), isInRoom ? roomName : null }
            };

            DatabaseManager.Instance.UpdateOnMain(
                updates,
                () => Debug.Log($"유저: {CurrentUid}의 방 상태 {isInRoom} 로 전환 완료, 방 이름: {roomName}"),
                error => Debug.LogError($"유저: {CurrentUid}의 방 상태 전환 실패: {error}")
            );
        }

        /// <summary>
        /// 플레이어의 게임 상태를 업데이트합니다.
        /// 게임 씬 시작 시, 로비 씬 시작 시 호출합니다.
        /// </summary>
        /// <param name="isInGame">게임 중 여부를 전달합니다</param>
        public void SetInGameStatus(bool isInGame)
        {
            // 게임 씬 들어가면 inGame true 전환, lastSeen 업데이트
            // 게임 씬 나가면 inGame false 전환, lastSeen 업데이트

            Dictionary<string, object> updates = new()
            {
                { DBRoutes.InGameStatus(CurrentUid), isInGame },
                { DBRoutes.LastSeen(CurrentUid), ServerValue.Timestamp }
            };

            DatabaseManager.Instance.UpdateOnMain(
                updates,
                () => Debug.Log($"유저: {CurrentUid}의 게임 상태 {isInGame} 로 전환 완료"),
                error => Debug.LogError($"유저: {CurrentUid}의 게임 상태 전환 실패: {error}")
            );
        }


        // 해당 메서드는 PartyServices 클래스로 이동함

        /// <summary>
        /// 플레이어가 파티에 있는지 여부를 업데이트합니다.
        /// </summary>
        /// <param name="partyId">플레이어가 파티에 있다면 partyId를 전달합니다. 없으면 전달하지 않아도 됩니다. (Optional Parameter)</param>
        //public void SetPartyStatus(bool isInParty, string partyId = null)
        //{
        //    // 파티 들어가면 partyId 업데이트, lastSeen 업데이트
        //    // 파티 나가면 partyId null로 업데이트, lastSeen 업데이트

        //    Dictionary<string, object> updates = new()
        //    {
        //        { DBRoutes.InPartyStatus(CurrentUid), isInParty },
        //        { DBRoutes.PartyIdForPresence(CurrentUid), isInParty ? partyId : null },
        //        { DBRoutes.LastSeen(CurrentUid), ServerValue.Timestamp }
        //    };

        //    DatabaseManager.Instance.UpdateOnMain(
        //        updates,
        //        () => Debug.Log($"유저: {CurrentUid}의 파티 ID {partyId} 로 업데이트 완료"),
        //        error => Debug.LogError($"유저: {CurrentUid}의 파티 ID 업데이트 실패: {error}")
        //    );
        //}


        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                StopHeartbeat();
                StartKillCountDown();
            }
            else
            {
                StopKillCountDown();
                StartHeartbeat();
            }
        }

        private void OnApplicationQuit()
        {
            StopHeartbeat();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (!focus)
            {
                OnApplicationPause(true);
            }
            else
            {
                OnApplicationPause(false);
            }
        }

        // OnDisconnect에서 online false 전환, inRoom false 전환, inGame false 전환, partyId null로 업데이트
        // 로그인 직후 및 재연결 시 호출해야 합니다.
        public async Task SetUpOnDisconnect()
        {
            var presenceRef = FirebaseManager.Instance.Database.GetReference(DBRoutes.Presence(CurrentUid));

            await presenceRef.OnDisconnect().UpdateChildren(
                new Dictionary<string, object>
                {
                    ["online"] = false,
                    ["inRoom"] = false,
                    ["inGame"] = false,
                    ["partyId"] = null,  // 파티 ID 삭제
                    ["roomName"] = null, // 방 이름 삭제
                    ["lastSeen"] = ServerValue.Timestamp, // 서버시간
                });
        }

        // 예외 상황 처리를 위해 Heartbeat를 사용하여 주기적으로 상태 업데이트

        // 로비 씬과 게임 씬에서 Heartbeat를 시작하고, 앱이 포커스를 잃거나 종료될 때 Heartbeat를 중지합니다.
        private void StartHeartbeat()
        {
            if (_heartBeatCO != null)
            {
                StopCoroutine(_heartBeatCO);
            }
            _heartBeatCO = StartCoroutine(HeartbeatCoroutine());
        }

        private void StopHeartbeat()
        {
            if (_heartBeatCO != null)
            {
                StopCoroutine(_heartBeatCO);
                _heartBeatCO = null;
            }
        }

        private IEnumerator HeartbeatCoroutine()
        {
            var wait = new WaitForSeconds(_heartbeatIntervalSeconds); // _heartbeatIntervalSeconds초마다 한 번 씩 HeartBeat 전송

            while (true)
            {
                DatabaseManager.Instance.SetOnMain(DBRoutes.Heartbeat(CurrentUid),
                    ServerValue.Timestamp,
                    () => Debug.Log($"유저: {CurrentUid}의 Heartbeat 업데이트 완료"),
                    error => Debug.LogError($"유저: {CurrentUid}의 Heartbeat 업데이트 실패: {error}"
                    ));

                yield return wait;
            }
        }

        private void StartKillCountDown()
        {
            if (_killCountDownCO != null)
            {
                StopCoroutine(_killCountDownCO);
            }
            _killCountDownCO = StartCoroutine(KillCountDownCoroutine());
        }

        private void StopKillCountDown()
        {
            if (_killCountDownCO != null)
            {
                StopCoroutine(_killCountDownCO);
                _killCountDownCO = null;
            }
        }

        private IEnumerator KillCountDownCoroutine()
        {
            yield return new WaitForSeconds(_killCountDownSeconds);

            // 앱이 포커스를 잃거나 종료될 때, 온라인 상태를 false로 전환하고, 방과 파티 상태를 false로 전환
            SetOnlineStatus(false);
            // SetInRoomStatus(false); 이건 온라인 여부를 판단하지 않기때문에 업데이트 불필요
            // SetInGameStatus(false); 이건 게임 씬 진입 시점에서 처리해야 될 듯??
            // SetPartyStatus(false); // 이건 PartyServices에서 처리해야 됨
            StopHeartbeat();
        }


        /// <summary>
        /// 온라인 상태 판별을 위해 사용하는 메서드입니다.
        /// 게임 진입 직전, 파티 초대 직전에 해당 유저의 온라인 상태를 확인합니다.
        /// 혹은 UI에서 친구의 온라인 상태를 확인할 때 사용합니다.
        /// </summary>
        /// <param name="uid">확인할 유저의 uid를 전달합니다.</param>
        /// <param name="onOnlineSuccess">온라인 여부를 전달받을 Action<bool>을 전달합니다.</param>
        /// <param name="onError">에러 로그를 전달받을 Action<string>을 전달합니다. (Optional Parameter)</param>
        /// <param name="validMs">HeartBeat를 통한 오프라인 판단 기준 ms입니다. 기본 60초입니다. (Optional Parameter)</param>
        public void IsOnline(string uid, Action<bool> onOnlineSuccess, Action<string> onError = null, int validMs = 60000)
        {
            if (string.IsNullOrEmpty(uid))
            {
                onOnlineSuccess?.Invoke(false);
                return;
            }

            // 클라이언트 시간 얻기
            long clientNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // presence/{uid}를 online상태와 heartbeat 교차 검증을 위해 Snapshot으로 읽기
            FirebaseManager.Instance.Database.GetReference(DBRoutes.Presence(uid)).GetValueAsync()
            .ContinueWithOnMainThread(presenceTask =>
            {
                if (presenceTask.IsCanceled || presenceTask.IsFaulted || presenceTask.Result == null || !presenceTask.Result.Exists)
                {
                    onOnlineSuccess?.Invoke(false); // 유저의 Presence 정보가 없어 false 처리. 첫 로그인 후 로비에 진입하지 않아 정보가 없을 수도 있음
                    return;
                }

                var snap = presenceTask.Result;

                // online 파싱
                bool online = false;
                var onlineChild = snap.Child(DatabaseKeys.online);
                if (onlineChild.Exists && onlineChild.Value is bool b)
                {
                    online = b;
                }

                // heartbeat 파싱
                long lastHb = 0;
                var hbChild = snap.Child(DatabaseKeys.heartbeat);
                if (hbChild.Exists)
                {
                    try
                    {
                        lastHb = Convert.ToInt64(hbChild.Value);
                    }
                    catch
                    {
                        // 변환 실패 시 기본값 0으로 설정
                    }
                }

                bool isAlive = (clientNow - lastHb) <= validMs; // TODO: 현재는 서버 시간 보정이 없어 클라이언트가 시간을 바꾸면 isAlive가 잘못 판단될 수 있음
                onOnlineSuccess?.Invoke(online && isAlive);
            });
        }


        /// <summary>
        /// 온라인 상태 판별을 위해 사용하는 메서드입니다.
        /// 방 진입 상태, 게임 진입 상태, 파티 진입 상태를 추가로 확인하는 오버로딩입니다.
        /// 게임 진입 직전, 파티 초대 직전에 해당 유저의 온라인 상태를 확인합니다.
        /// 혹은 UI에서 친구의 온라인 상태를 확인할 때 사용합니다.
        /// </summary>
        /// <param name="uid">확인할 유저의 uid를 전달합니다.</param>
        /// <param name="onOnlineSuccess">온라인 여부를 전달받을 Action<bool>을 전달합니다.</param>
        /// <param name="onInRoomSuccess">방 진입 여부를 전달받을 Action<bool>을 전달합니다.</param>
        /// <param name="onInGameSuccess">게임 중 여부를 전달받을 Action<bool>을 전달합니다.</param>
        /// <param name="onInPartySuccess">파티 참가 여부를 전달받을 Action<bool>을 전달합니다.</param>
        /// <param name="onError">에러 로그를 전달받을 Action<string>을 전달합니다. (Optional Parameter)</param>
        /// <param name="validMs">HeartBeat를 통한 오프라인 판단 기준 ms입니다. 기본 60초입니다. (Optional Parameter)</param>
        public void IsOnline(string uid, Action<bool> onOnlineSuccess, Action<bool> onInRoomSuccess,
            Action<bool> onInGameSuccess, Action<bool> onInPartySuccess, Action<string> onError = null, int validMs = 60000)
        {
            if (string.IsNullOrEmpty(uid))
            {
                onOnlineSuccess?.Invoke(false);
                return;
            }

            // 클라이언트 시간 얻기
            long clientNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // presence/{uid}를 online상태와 heartbeat 교차 검증을 위해 Snapshot으로 읽기
            FirebaseManager.Instance.Database.GetReference(DBRoutes.Presence(uid)).GetValueAsync()
                .ContinueWithOnMainThread(presenceTask =>
                {
                    if (presenceTask.IsCanceled || presenceTask.IsFaulted || presenceTask.Result == null || !presenceTask.Result.Exists)
                    {
                        onOnlineSuccess?.Invoke(false); // 유저의 Presence 정보가 없어 false 처리. 첫 로그인 후 로비에 진입하지 않아 정보가 없을 수도 있음
                        return;
                    }

                    var snap = presenceTask.Result;

                    // online 파싱
                    bool online = false;
                    var onlineChild = snap.Child(DatabaseKeys.online);
                    if (onlineChild.Exists && onlineChild.Value is bool b)
                    {
                        online = b;
                    }

                    // inRoom 파싱
                    bool inRoom = false;
                    var inRoomChild = snap.Child(DatabaseKeys.inRoom);
                    if (inRoomChild.Exists && inRoomChild.Value is bool r)
                    {
                        inRoom = r;
                    }
                    onInRoomSuccess?.Invoke(inRoom);

                    // inGame 파싱
                    bool inGame = false;
                    var inGameChild = snap.Child(DatabaseKeys.inGame);
                    if (inGameChild.Exists && inGameChild.Value is bool g)
                    {
                        inGame = g;
                    }
                    onInGameSuccess?.Invoke(inGame);

                    // inParty 파싱
                    bool inParty = false;
                    var inPartyChild = snap.Child(DatabaseKeys.inParty);
                    if (inPartyChild.Exists && inPartyChild.Value is bool p)
                    {
                        inParty = p;
                    }
                    onInPartySuccess?.Invoke(inParty);

                    // heartbeat 파싱
                    long lastHb = 0;
                    var hbChild = snap.Child(DatabaseKeys.heartbeat);
                    if (hbChild.Exists)
                    {
                        try
                        {
                            lastHb = Convert.ToInt64(hbChild.Value);
                        }
                        catch
                        {
                            // 변환 실패 시 기본값 0으로 설정
                        }
                    }

                    bool isAlive = (clientNow - lastHb) <= validMs; // TODO: 현재는 서버 시간 보정이 없어 클라이언트가 시간을 바꾸면 isAlive가 잘못 판단될 수 있음
                    onOnlineSuccess?.Invoke(online && isAlive);
                });
        }
    }
}

