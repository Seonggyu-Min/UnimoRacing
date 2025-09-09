using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class UserInfoIndicator : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nicknameText;        // 닉네임
        [SerializeField] private TMP_Text _goldText;            // 무료재화
        [SerializeField] private TMP_Text _blueHoneyGemText;    // 유료재화
        [SerializeField] private TMP_Text _levelText;           // 레벨
        [SerializeField] private Image _expImage;               // 경험치 바 (Fill Amount로 조절함)

        private Action _unsubGold;
        private Action _unsubBlue;

        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;



        private void OnEnable()
        {
            SetUI();
            SubscribeValueChange();
        }

        private void OnDisable()
        {
            UnsubscribeValueChange();
        }

        private void SetUI()
        {
            DatabaseManager.Instance.GetOnMain(
                DBRoutes.Users(CurrentUid),
                snap =>
                {
                    // 닉네임 가져오기
                    string nickname = "Error";
                    var nicknameSnap = snap.Child(DatabaseKeys.nickname);
                    if (nicknameSnap.Exists)
                    {
                        nickname = nicknameSnap.Value.ToString();
                    }
                    _nicknameText.text = nickname;

                    // 경험치 가져오기
                    int exp = 0;
                    var expSnap = snap.Child(DatabaseKeys.gameData).Child(DatabaseKeys.experience);
                    if (expSnap.Exists)
                    {
                        if (!int.TryParse(expSnap.Value.ToString(), out exp))
                        {
                            Debug.LogWarning($"[UserInfoIndicator] 경험치를 int로 변환할 수 없습니다");
                        }
                    }

                    // 레벨 변환
                    int level = 0;
                    level = ExpToLevel.LevelFromTotalExp(exp);
                    _levelText.text = $"Lv. {level}";

                    // 경험치 바 적용
                    _expImage.fillAmount = ExpToLevel.Fill01FromTotalExp(exp);
                },
                err => Debug.LogWarning($"[UserInfoIndicator] 유저 정보 읽기를 실패했습니다. {err}")
                );
        }

        private void SubscribeValueChange()
        {
            UnsubscribeValueChange();

            _unsubGold = DatabaseManager.Instance.SubscribeValueChanged(
                DBRoutes.Gold(CurrentUid),
                snap =>
                {
                    long v = 0;
                    if (snap != null && snap.Exists && snap.Value != null)
                    {
                        long.TryParse(snap.Value.ToString(), out v);
                    }
                    _goldText.text = v.ToString("N0");
                },
                err => Debug.LogWarning($"[UserInfoIndicator] 골드 구독 에러: {err}")
            );

            _unsubBlue = DatabaseManager.Instance.SubscribeValueChanged(
                DBRoutes.BlueHoneyGem(CurrentUid),
                snap =>
                {
                    long v = 0;
                    if (snap != null && snap.Exists && snap.Value != null)
                    {
                        long.TryParse(snap.Value.ToString(), out v);
                    }
                    _blueHoneyGemText.text = v.ToString("N0");
                },
                err => Debug.LogWarning($"[UserInfoIndicator] 블루허니잼 구독 에러: {err}")
            );
        }

        private void UnsubscribeValueChange()
        {
            _unsubGold?.Invoke();
            _unsubGold = null;

            _unsubBlue?.Invoke();
            _unsubBlue = null;
        }
    }
}
