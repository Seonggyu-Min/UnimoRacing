using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class DBInitializer : MonoBehaviour
    {
        [SerializeField] private AuthFlowController _authFlowController;
        [SerializeField] private GameObject _nicknameObj; // 끈 상태로 시작
        [SerializeField] private TMP_Text _infoText;
        [SerializeField] private TMP_InputField _nicknameInputField;

        [Header("Init elements")]
        [SerializeField][Min(0)] private int[] _initInventoryUnimos = new int[0];    // 최초 회원가입 시 가져야할 유니모 배열 (인덱스)
        [SerializeField][Min(0)] private int[] _initInventoryKarts = new int[0];     // 최초 회원가입 시 가져야할 카트 배열 (인덱스)
        [SerializeField][Min(0)] private int _initEquippedUnimo;                     // 최초 회원가입 시 장착하고 있을 유니모 (인덱스)
        [SerializeField][Min(0)] private int _initEquippedKart;                      // 최초 회원가입 시 장착하고 있을 카트 (인덱스)
        [SerializeField][Min(0)] private int _initMoney1;                            // 최초 회원가입 시 가져야할 돈1의 양
        [SerializeField][Min(0)] private int _initMoney2;                            // 최초 회원가입 시 가져야할 돈2의 양
        [SerializeField][Min(0)] private int _initMoney3;                            // 최초 회원가입 시 가져야할 돈3의 양


        private const int INIT_LEVEL = 1; // 1이 강화가 안된, 소지 여부만을 검증하는 레벨로 간주
        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;


        private void Start()
        {
            _authFlowController.OnAuthSucceeded += CheckNicknameSet;
        }

        private void OnDisable()
        {
            if (_authFlowController != null)
            {
                _authFlowController.OnAuthSucceeded -= CheckNicknameSet;
            }
        }

        private void CheckNicknameSet()
        {
            DatabaseManager.Instance.GetOnMain((DBRoutes.Nickname(FirebaseManager.Instance.Auth.CurrentUser.UserId)), snap =>
            {
                if (!snap.Exists) // 닉네임이 없으면 최초 회원가입으로 가정
                {
                    _nicknameObj.SetActive(true);
                    InitDB();
                }
            });
        }

        public void OnClickSetNickName()
        {
            string newNickname = _nicknameInputField.text;

            DatabaseManager.Instance.GetOnMain(DBRoutes.Nicknames(newNickname), snap =>
            {
                if (snap.Exists)
                {
                    _infoText.text = $"닉네임: {newNickname}가 이미 존재합니다";
                }
                else
                {
                    Dictionary<string, object> updates = new()
                    {
                        { DBRoutes.Nickname(FirebaseManager.Instance.Auth.CurrentUser.UserId), newNickname}, // users/{uid}/nickname에 자신의 닉네임 설정
                        { DBRoutes.Nicknames(newNickname), FirebaseManager.Instance.Auth.CurrentUser.UserId} // 빠른 조회를 위해 역인덱스로 nicknames/{newNickname}에 uid 설정
                    };

                    DatabaseManager.Instance.UpdateOnMain(updates,
                        onSuccess: () => _infoText.text = $"{newNickname}로 닉네임 설정 완료. 다음으로 넘어갈 수 있다는 안내 문구 띄우기",
                        onError: err => _infoText.text = $"닉네임 설정 오류: {err}"
                        );
                }
            });
        }

        private void InitDB()
        {
            Dictionary<string, object> updates = new();

            updates[DBRoutes.Wins(CurrentUid)] = 0;
            updates[DBRoutes.Losses(CurrentUid)] = 0;
            updates[DBRoutes.Experience(CurrentUid)] = 0;

            updates[DBRoutes.Money1(CurrentUid)] = _initMoney1;
            updates[DBRoutes.Money2(CurrentUid)] = _initMoney2;
            updates[DBRoutes.Money3(CurrentUid)] = _initMoney3;

            if (_initInventoryUnimos != null)
            {
                foreach (int unimoId in _initInventoryUnimos)
                {
                    updates[DBRoutes.UnimoInventory(CurrentUid, unimoId)] = INIT_LEVEL;
                }
            }
            if (_initInventoryKarts != null)
            {
                foreach (int kartId in _initInventoryKarts)
                {
                    updates[DBRoutes.KartInventory(CurrentUid, kartId)] = INIT_LEVEL;
                }
            }

            updates[DBRoutes.EquippedKart(CurrentUid)] = _initEquippedKart;
            updates[DBRoutes.EquippedUnimo(CurrentUid)] = _initEquippedUnimo;

            DatabaseManager.Instance.UpdateOnMain(updates,
                () => Debug.Log("[DBInitializer] DB 초기화 완료"),
                err => Debug.LogError($"[DBInitializer] DB 초기화 실패: {err}"));
        }
    }
}
