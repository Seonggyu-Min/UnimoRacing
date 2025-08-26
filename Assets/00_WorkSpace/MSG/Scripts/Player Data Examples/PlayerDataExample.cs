using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class PlayerDataExample : MonoBehaviour
    {
        #region Fields

        [SerializeField] private TMP_Text _testText;
        [SerializeField] private TMP_InputField _nicknameInputField;

        [SerializeField] private Button _showUidButton;
        [SerializeField] private Button _showSignedDateButton;
        [SerializeField] private Button _setNicknameButton;
        [SerializeField] private Button _showNicknameButton;
        [SerializeField] private Button _showLevelButton;
        [SerializeField] private Button _showExpButton;
        [SerializeField] private Button _getExpButton;
        [SerializeField] private Button _getUnimoButton;
        [SerializeField] private Button _getKartButton;
        [SerializeField] private Button _showOwnedUnimosButton;
        [SerializeField] private Button _showOwnedKartsButton;
        [SerializeField] private Button _showOwnedSkinsButton;
        [SerializeField] private Button _equipUnimoButton;
        [SerializeField] private Button _equipKartButton;
        [SerializeField] private Button _equippedUnimoButton;
        [SerializeField] private Button _showEquippedKartButton;
        [SerializeField] private Button _showKartUpgradesButton;
        [SerializeField] private Button _addFriendButton;
        [SerializeField] private Button _showFriendListButton;
        [SerializeField] private Button _getFriendRequestListButton;
        [SerializeField] private Button _acceptFriendRequestButton;
        [SerializeField] private Button _rejectFriendRequestButton;
        [SerializeField] private Button _cancelFriendRequestButton;
        [SerializeField] private Button _removeFriendButton;


        [SerializeField] FriendsLogics _friendLogics;
        string _friendUid = "CObPZErspoMBVFFkqDsxhYIzm1l2";
        //"YMVX4vwl3aYwmWZEVPBnXXW9qew1"; // 예시로 친구 요청할 UID를 하드코딩
        #endregion

        private void OnEnable()
        {
            _showUidButton.onClick.AddListener(OnClickShowUid);
            _showSignedDateButton.onClick.AddListener(OnClickShowSignedDate);
            _setNicknameButton.onClick.AddListener(OnClickSetNickname);
            _showNicknameButton.onClick.AddListener(OnClickShowNickname);
            _showLevelButton.onClick.AddListener(OnClickShowLevel);
            _showExpButton.onClick.AddListener(OnClickShowExp);
            _getExpButton.onClick.AddListener(OnClickGetExp);
            _getUnimoButton.onClick.AddListener(OnClickGetUnimo);
            _getKartButton.onClick.AddListener(OnClickGetKart);
            _showOwnedUnimosButton.onClick.AddListener(OnClickShowOwnedUnimos);
            _showOwnedKartsButton.onClick.AddListener(OnClickShowOwnedKarts);
            _showOwnedSkinsButton.onClick.AddListener(OnClickShowOwnedSkins);
            _equipUnimoButton.onClick.AddListener(OnClickEquipUnimo);
            _equipKartButton.onClick.AddListener(OnClickEquipKart);
            _equippedUnimoButton.onClick.AddListener(OnClickEquippedUnimo);
            _showEquippedKartButton.onClick.AddListener(OnClickShowEqippedKart);
            _showKartUpgradesButton.onClick.AddListener(OnClickShowKartUpgrades);
            _addFriendButton.onClick.AddListener(OnClickAddFriend);
            _showFriendListButton.onClick.AddListener(OnClickShowFriendList);
            _getFriendRequestListButton.onClick.AddListener(OnClickGetFriendRequestList);
            _acceptFriendRequestButton.onClick.AddListener(OnClickAccecpFriendRequest);
            _rejectFriendRequestButton.onClick.AddListener(OnClickRejectFriendRequest);
            _cancelFriendRequestButton.onClick.AddListener(OnClickCancleFriendRequest);
            _removeFriendButton.onClick.AddListener(OnClickRemoveFriend);
        }

        private void OnDisable()
        {
            _showUidButton.onClick.RemoveListener(OnClickShowUid);
            _showSignedDateButton.onClick.RemoveListener(OnClickShowSignedDate);
            _setNicknameButton.onClick.RemoveListener(OnClickSetNickname);
            _showNicknameButton.onClick.RemoveListener(OnClickShowNickname);
            _showLevelButton.onClick.RemoveListener(OnClickShowLevel);
            _showExpButton.onClick.RemoveListener(OnClickShowExp);
            _getExpButton.onClick.RemoveListener(OnClickGetExp);
            _getUnimoButton.onClick.RemoveListener(OnClickGetUnimo);
            _getKartButton.onClick.RemoveListener(OnClickGetKart);
            _showOwnedUnimosButton.onClick.RemoveListener(OnClickShowOwnedUnimos);
            _showOwnedKartsButton.onClick.RemoveListener(OnClickShowOwnedKarts);
            _showOwnedSkinsButton.onClick.RemoveListener(OnClickShowOwnedSkins);
            _equipUnimoButton.onClick.RemoveListener(OnClickEquipUnimo);
            _equipKartButton.onClick.RemoveListener(OnClickEquipKart);
            _equippedUnimoButton.onClick.RemoveListener(OnClickEquippedUnimo);
            _showEquippedKartButton.onClick.RemoveListener(OnClickShowEqippedKart);
            _showKartUpgradesButton.onClick.RemoveListener(OnClickShowKartUpgrades);
            _addFriendButton.onClick.RemoveListener(OnClickAddFriend);
            _showFriendListButton.onClick.RemoveListener(OnClickShowFriendList);
            _getFriendRequestListButton.onClick.RemoveListener(OnClickGetFriendRequestList);
            _acceptFriendRequestButton.onClick.RemoveListener(OnClickAccecpFriendRequest);
            _rejectFriendRequestButton.onClick.RemoveListener(OnClickRejectFriendRequest);
            _cancelFriendRequestButton.onClick.RemoveListener(OnClickCancleFriendRequest);
            _removeFriendButton.onClick.RemoveListener(OnClickRemoveFriend);
        }


        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;

        private void OnClickShowUid()
        {
            _testText.text = $"현재 UID: {CurrentUid}";
        }

        private void OnClickShowSignedDate()
        {
            var createdMs = FirebaseManager.Instance.Auth.CurrentUser.Metadata?.CreationTimestamp ?? 0;
            var created = DateTimeOffset.FromUnixTimeMilliseconds((long)createdMs).UtcDateTime;
            _testText.text = $"가입일: {created:yyyy-MM-dd HH:mm:ss}";
        }

        private void OnClickSetNickname()
        {
            // 먼저 자신 uid의 닉네임이 DB에 없는지 확인하고 첫 로그인이라고 간주하고 닉네임 설정 창을 띄워야 합니다
            DatabaseManager.Instance.GetOnMain((DBRoutes.Nickname(CurrentUid)), snap =>
            {
                if (snap.Exists)
                {
                    _testText.text = "이미 닉네임이 설정되어 있습니다.";
                    return;
                }
            });

            // 닉네임이 없으면, 다음 작업 실행
            string newNickname = _nicknameInputField.text;

            DatabaseManager.Instance.GetOnMain(DBRoutes.Nicknames(newNickname), snap =>
            {
                if (snap.Exists)
                {
                    _testText.text = $"닉네임: {newNickname}가 이미 존재합니다";
                }
                else
                {
                    var updates = new Dictionary<string, object>
                    {
                        { DBRoutes.Nickname(CurrentUid), newNickname}, // users/{uid}/nickname에 자신의 닉네임 설정
                        { DBRoutes.Nicknames(newNickname), CurrentUid} // 빠른 조회를 위해 역인덱스로 nicknames/{newNickname}에 uid 설정
                    };

                    DatabaseManager.Instance.UpdateOnMain(updates,
                        onSuccess: () => _testText.text = $"{newNickname}로 닉네임 설정 완료",
                        onError: err => _testText.text = $"닉네임 설정 오류: {err}"
                        );
                }
            });
        }

        private void OnClickShowNickname()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.Nickname(CurrentUid),
                snap => _testText.text = $"현재 닉네임: {snap.Value}",
                err => _testText.text = $"현재 닉네임 읽기 오류: {err}"
                );
        }

        private void OnClickShowLevel()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.Experience(CurrentUid),
                snap =>
                {
                    long exp = 0;
                    if (snap.Exists && snap.Value != null)
                    {
                        long.TryParse(snap.Value.ToString(), out exp);
                    }

                    _testText.text = $"레벨: {ExpToLevel.Convert((int)snap.Value)}";
                },
                err => _testText.text = $"레벨 읽기 오류: {err}"
                );
        }

        private void OnClickShowExp()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.Experience(CurrentUid),
                snap => _testText.text = $"경험치: {snap.Value}",
                err => _testText.text = $"경험치 읽기: {err}"
                );
        }

        private void OnClickGetExp()
        {
            int expGain = 100; // 예시로 100 경험치를 얻는다고 가정

            DatabaseManager.Instance.IncrementToLongOnMainWithTransaction(DBRoutes.Experience(CurrentUid),
                expGain,
                onSuccess: v => _testText.text = $"경험치 {expGain}을 얻음. 현재 경험치: {v}",
                onError: err => _testText.text = $"경험치 쓰기 오류: {err}");
        }

        private void OnClickGetUnimo()
        {
            int unimoId = 0; // 예시로 0번 Unimo를 얻는다고 가정

            DatabaseManager.Instance.SetOnMain(DBRoutes.UnimoInventory(CurrentUid, unimoId),
                1,  // 1은 Unimo의 강화 레벨을 의미, 0이면 미보유
                onSuccess: () => _testText.text = $"유니모 id({unimoId}) 획득",
                onError: err => _testText.text = $"유니모 획득 오류: {err}");
        }

        private void OnClickGetKart()
        {
            int kartId = 0; // 예시로 0번 Kart를 얻는다고 가정

            DatabaseManager.Instance.SetOnMain(DBRoutes.KartInventory(CurrentUid, kartId),
                1,  // 1은 Kart의 강화 레벨을 의미, 0이면 미보유
                onSuccess: () => _testText.text = $"카트 id({kartId}) 획득",
                onError: err => _testText.text = $"카트 획득 오류: {err}");
        }

        private void OnClickShowOwnedUnimos()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.UnimosInventory(CurrentUid), snap =>
            {
                if (!snap.Exists)
                {
                    _testText.text = "소유한 유니모가 없습니다";
                    return;
                }

                List<string> lines = new();
                foreach (var c in snap.Children)
                {
                    string id = c.Key;
                    string level = c.Value?.ToString() ?? "0";
                    lines.Add($"유니모 id: {id}, 레벨: {level}");
                }
                _testText.text = string.Join("\n", lines);
            },
            err => _testText.text = $"소유한 유니모 읽기 오류: {err}");
        }

        private void OnClickShowOwnedKarts()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.KartsInventory(CurrentUid), snap =>
            {
                if (!snap.Exists)
                {
                    _testText.text = "소유한 카트가 없습니다";
                    return;
                }
                List<string> lines = new();
                foreach (var c in snap.Children)
                {
                    string id = c.Key;
                    string level = c.Value?.ToString() ?? "0";
                    lines.Add($"카트 id: {id}, 레벨: {level}");
                }
                _testText.text = string.Join("\n", lines);
            },
            err => _testText.text = $"소유한 카트 읽기 오류: {err}");
        }

        // 아직 외형 관련해서는 미구현 상태, OBT 이후에 구현 예정
        private void OnClickShowOwnedSkins()
        {

        }

        private void OnClickEquipUnimo()
        {
            int unimoId = 0; // 예시로 0번 Unimo를 장착한다고 가정

            DatabaseManager.Instance.SetOnMain(DBRoutes.Unimos(CurrentUid),
                unimoId, // 장착할 Unimo의 ID
                onSuccess: () => _testText.text = $"유니모 id({unimoId}) 장착 완료",
                onError: err => _testText.text = $"유니모 장착 오류: {err}");
        }

        private void OnClickEquipKart()
        {
            int kartId = 0; // 예시로 0번 Kart를 장착한다고 가정

            DatabaseManager.Instance.SetOnMain(DBRoutes.Karts(CurrentUid),
                kartId, // 장착할 Kart의 ID
                onSuccess: () => _testText.text = $"카트 id({kartId}) 장착 완료",
                onError: err => _testText.text = $"카트 장착 오류: {err}");
        }

        private void OnClickEquippedUnimo()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.Unimos(CurrentUid),
                snap => _testText.text = $"장착 중인 유니모: {snap.Value ?? "없음"}",
                err => _testText.text = $"장착 중인 유니모 읽기 오류: {err}");
        }

        private void OnClickShowEqippedKart()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.Karts(CurrentUid),
                snap => _testText.text = $"장착 중인 카트: {snap.Value ?? "없음"}",
                err => _testText.text = $"장착 중인 카트 읽기 오류: {err}");
        }

        private void OnClickShowKartUpgrades()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.KartsInventory(CurrentUid), snap =>
            {
                if (!snap.Exists)
                {
                    _testText.text = "소유한 카트가 없습니다";
                    return;
                }

                List<string> lines = new();
                foreach (var c in snap.Children)
                {
                    string id = c.Key;
                    string lvl = c.Value?.ToString() ?? "0";
                    lines.Add($"카트 id: {id}, 업그레이드: {lvl}");
                }
                _testText.text = $"{string.Join("\n", lines)}";
            },
            err => _testText.text = $"카트 업그레이드 읽기 오류: {err}");
        }

        private void OnClickAddFriend()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.Nickname(_friendUid), snap =>
            {
                string friendNickname = snap.Value?.ToString() ?? _friendUid;

                _friendLogics.SendRequest(CurrentUid, _friendUid,
                    onSuccess: () => _testText.text = $"{friendNickname}에게 친구 요청을 보냈습니다.",
                    onError: err => _testText.text = $"친구 요청 실패: {err}");
            },
            err =>
            {
                // 닉네임 읽기는 실패했지만, 친구 요청은 보내기
                _friendLogics.SendRequest(CurrentUid, _friendUid,
                    onSuccess: () => _testText.text = $"{_friendUid}에게 친구 요청을 보냈습니다. (닉네임 조회 실패)",
                    onError: e => _testText.text = $"친구 요청 실패: {e}");
            });

            // 반대로 닉네임 기반 요청도 필요시 가능합니다
        }

        private void OnClickShowFriendList()
        {
            _testText.text = "친구 목록을 불러오는 중…";

            // 내 리스트 루트 읽기
            DatabaseManager.Instance.GetOnMain(DBRoutes.FriendListRoot(CurrentUid), snap =>
            {
                if (!snap.Exists || !snap.HasChildren)
                {
                    _testText.text = "친구가 없습니다.";
                    return;
                }

                List<string> friendUids = new();
                foreach (var c in snap.Children)
                {
                    // 키가 상대 uid
                    friendUids.Add(c.Key);
                }

                // 닉네임 병행 조회
                List<string> lines = new();
                int left = friendUids.Count;

                foreach (var otherUid in friendUids)
                {
                    DatabaseManager.Instance.GetOnMain(DBRoutes.Nickname(otherUid),
                        s2 =>
                        {
                            var nick = s2.Value?.ToString() ?? otherUid;
                            lines.Add($"{nick} ({otherUid})");

                            if (--left == 0)
                            {
                                lines.Sort(StringComparer.OrdinalIgnoreCase);
                                _testText.text = "친구 목록\n" + string.Join("\n", lines);
                            }
                        },
                        err =>
                        {
                            Debug.LogWarning($"닉네임 읽기 오류 ({otherUid}): {err}");
                            lines.Add(otherUid);

                            if (--left == 0)
                            {
                                lines.Sort(StringComparer.OrdinalIgnoreCase);
                                _testText.text = "친구 목록\n" + string.Join("\n", lines);
                            }
                        });
                }
            },
            err => _testText.text = $"친구 목록 읽기 실패: {err}");
        }

        private void OnClickGetFriendRequestList()
        {
            string inboxRoot = DBRoutes.InBoxRoot(CurrentUid);
            DatabaseManager.Instance.GetOnMain(inboxRoot, snap =>
            {
                if (!snap.Exists)
                {
                    _testText.text = "받은 친구 요청이 없습니다.";
                    return;
                }

                var pairIds = new List<string>();
                foreach (var c in snap.Children)
                {
                    string status = c.Child(DatabaseKeys.status).Value?.ToString() ?? c.Value?.ToString();
                    if (status == DatabaseKeys.pending) pairIds.Add(c.Key);
                }

                if (pairIds.Count == 0)
                {
                    _testText.text = "받은 친구 요청이 없습니다.";
                    return;
                }

                int left = pairIds.Count;
                List<string> lines = new();

                foreach (var pid in pairIds)
                {
                    // 누가 보냈는지 중앙 문서에서 확인
                    var linkPath = DBRoutes.FriendLinks(pid);
                    DatabaseManager.Instance.GetOnMain(linkPath, s2 =>
                    {
                        string from = s2.Child(DatabaseKeys.from).Value?.ToString();
                        string other = !string.IsNullOrEmpty(from) ? from : DBPathMaker.ComposePairId(pid, CurrentUid);
                        if (string.IsNullOrEmpty(other)) other = pid;

                        // 닉네임 읽기(선택)
                        DatabaseManager.Instance.GetOnMain(DBRoutes.Nickname(other), s3 =>
                        {
                            string nick = s3.Value?.ToString() ?? other;
                            lines.Add($"요청 from: {nick} ({other})");
                            if (--left == 0) _testText.text = "받은 친구 요청:\n" + string.Join("\n", lines);
                        },
                        err =>
                        {
                            lines.Add($"요청 from: {other}");
                            if (--left == 0) _testText.text = "받은 친구 요청:\n" + string.Join("\n", lines);
                        });
                    },
                    err =>
                    {
                        lines.Add($"요청 pairId: {pid} (상세 읽기 실패)");
                        if (--left == 0) _testText.text = "받은 친구 요청:\n" + string.Join("\n", lines);
                    });
                }
            },
            err => _testText.text = $"받은 친구 요청 읽기 오류: {err}");
        }

        private void OnClickAccecpFriendRequest()
        {
            string pairId = DBPathMaker.ComposePairId(CurrentUid, _friendUid);
            _testText.text = "친구 요청 수락 중…";

            _friendLogics.AcceptRequest(pairId, CurrentUid,
                onSuccess: () =>
                {
                    _testText.text = "친구 요청을 수락했습니다.";
                },
                onError: err =>
                {
                    _testText.text = $"친구 요청 수락 실패: {err}";
                });
        }

        private void OnClickRejectFriendRequest()
        {
            string pairId = DBPathMaker.ComposePairId(CurrentUid, _friendUid);
            _testText.text = "친구 요청 거절 중…";

            _friendLogics.RejectRequest(pairId, CurrentUid,
                onSuccess: () =>
                {
                    _testText.text = "친구 요청을 거절했습니다.";
                },
                onError: err =>
                {
                    _testText.text = $"친구 요청 거절 실패: {err}";
                });
        }

        private void OnClickCancleFriendRequest()
        {
            string pairId = DBPathMaker.ComposePairId(CurrentUid, _friendUid);
            _testText.text = "친구 요청 취소 중…";

            _friendLogics.CancelRequest(pairId, CurrentUid,
                onSuccess: () =>
                {
                    _testText.text = "친구 요청을 취소했습니다.";
                },
                onError: err =>
                {
                    _testText.text = $"친구 요청 취소 실패: {err}";
                });
        }

        private void OnClickRemoveFriend()
        {
            string pairId = DBPathMaker.ComposePairId(CurrentUid, _friendUid);
            _testText.text = "친구 삭제 중…";

            _friendLogics.RemoveFriend(pairId, CurrentUid,
                onSuccess: () =>
                {
                    _testText.text = "친구를 삭제했습니다.";
                },
                onError: err =>
                {
                    _testText.text = $"친구 삭제 실패: {err}";
                });
        }

        #region Dedicated Methods
        // 친구 목록을 불러오기 위해 OutBoxRoot 및 InBoxRoot를 전부 조회하는 것이 비용이 너무 커 보류
        // 자신 이외의 uid에 접근하는 방식은 좋지 않다고 판단하여 InBox/OutBox 방식으로 주고받도록 설계하였으나
        // 해당 방식은 서버에서 권위적으로 users/{friendUid}/friends/list를 수정해줘야 함
        // 그러나 해당 방식은 Firebase Functions를 사용해야 되는데, 해당 기능은 유료 요금제에서만 지원함
        // 따라서 직접 users/{myUid}/friends/list 및 users/{friendUid}/friends/list 를 직접 수정하는 방법으로 바꾸고,
        // 복잡하지만 보안 규칙을 해당 경로에서 ComposePairId를 사용하여 검증하는 식으로 사용해야 될 것 같음

        //private void OnClickShowFriendList()
        //{
        //    _testText.text = "친구 목록을 불러오는 중…";

        //    // 상태 초기화
        //    _pairIds = new HashSet<string>();
        //    _acceptedUids = new HashSet<string>();
        //    _pendingPairReads = 2;   // outbox + inbox

        //    // 1) /outbox/{myUid}
        //    DatabaseManager.Instance.GetOnMain(DBRoutes.OutBoxRoot(CurrentUid), snap1 =>
        //    {
        //        if (snap1.Exists)
        //        {
        //            foreach (var c in snap1.Children)
        //                _pairIds.Add(c.Key);
        //        }
        //        FinishCollectingPairIds(CurrentUid);
        //    },
        //    err =>
        //    {
        //        Debug.LogWarning($"Outbox 조회 오류: {err}");
        //        FinishCollectingPairIds(CurrentUid);
        //    });

        //    // 2) /inbox/{myUid}
        //    DatabaseManager.Instance.GetOnMain(DBRoutes.InBoxRoot(CurrentUid), snap2 =>
        //    {
        //        if (snap2.Exists)
        //        {
        //            foreach (var c in snap2.Children)
        //                _pairIds.Add(c.Key);
        //        }
        //        FinishCollectingPairIds(CurrentUid);
        //    },
        //    err =>
        //    {
        //        Debug.LogWarning($"Inbox 조회 오류: {err}");
        //        FinishCollectingPairIds(CurrentUid);
        //    });
        //}

        //// outbox/inbox 두 요청 완료 후 friendLinks를 확인해 accepted만 add
        //private void FinishCollectingPairIds(string myUid)
        //{
        //    if (--_pendingPairReads > 0) return;

        //    if (_pairIds == null || _pairIds.Count == 0)
        //    {
        //        _testText.text = "친구 링크가 없습니다.";
        //        return;
        //    }

        //    _linksLeft = _pairIds.Count;

        //    foreach (var pairId in _pairIds)
        //    {
        //        var linkPath = DBRoutes.FriendLinks(pairId);

        //        DatabaseManager.Instance.GetOnMain(linkPath, snap =>
        //        {
        //            if (snap.Exists)
        //            {
        //                var status = snap.Child(DatabaseKeys.status).Value?.ToString();
        //                if (status == DatabaseKeys.accepted)
        //                {
        //                    var otherUid = OtherUidFromPair(pairId, myUid);
        //                    if (!string.IsNullOrEmpty(otherUid))
        //                        _acceptedUids.Add(otherUid);
        //                }
        //            }
        //            AfterFriendLinksScanned();
        //        },
        //        err =>
        //        {
        //            Debug.LogWarning($"friendLinks 읽기 오류 ({linkPath}): {err}");
        //            AfterFriendLinksScanned();
        //        });
        //    }
        //}

        //private void AfterFriendLinksScanned()
        //{
        //    if (--_linksLeft > 0) return;

        //    if (_acceptedUids == null || _acceptedUids.Count == 0)
        //    {
        //        _testText.text = "수락된 친구가 없습니다.";
        //        return;
        //    }

        //    // 수락된 uid들에 대해 닉네임 병행 조회
        //    _nickLeft = _acceptedUids.Count;
        //    var lines = new List<string>();

        //    foreach (var uid in _acceptedUids)
        //    {
        //        DatabaseManager.Instance.GetOnMain(DBRoutes.Nickname(uid), snap =>
        //        {
        //            var nick = snap.Value?.ToString() ?? uid;
        //            lines.Add($"{nick} ({uid})");
        //            if (--_nickLeft == 0)
        //            {
        //                lines.Sort(StringComparer.OrdinalIgnoreCase);
        //                _testText.text = "친구 목록\n" + string.Join("\n", lines);
        //            }
        //        },
        //        err =>
        //        {
        //            Debug.LogWarning($"닉네임 읽기 오류 ({uid}): {err}");
        //            lines.Add(uid);
        //            if (--_nickLeft == 0)
        //            {
        //                lines.Sort(StringComparer.OrdinalIgnoreCase);
        //                _testText.text = "친구 목록\n" + string.Join("\n", lines);
        //            }
        //        });
        //    }
        //}

        //private string OtherUidFromPair(string pairId, string myUid)
        //{
        //    // pairId = "{minUid}_{maxUid}"
        //    var parts = pairId.Split('_');
        //    if (parts.Length != 2) return null;
        //    if (parts[0] == myUid) return parts[1];
        //    if (parts[1] == myUid) return parts[0];
        //    return null;
        //}
        #endregion
    }
}
