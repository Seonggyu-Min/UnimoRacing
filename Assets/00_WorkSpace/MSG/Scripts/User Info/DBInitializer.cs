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
        [SerializeField] private GameObject _tapToStartObj; // tap to start로 다음 씬으로 넘어가게 하는 오브젝트

        [Header("Init elements")]
        [SerializeField][Min(20000)] private int[] _initInventoryUnimos = new int[0];       // 최초 회원가입 시 가져야할 유니모 배열 (인덱스)
        [SerializeField][Min(10000)] private int[] _initInventoryKarts = new int[0];        // 최초 회원가입 시 가져야할 카트 배열 (인덱스)
        [SerializeField][Min(0)] private int _initEquippedUnimo;                            // 최초 회원가입 시 장착하고 있을 유니모 (인덱스)
        [SerializeField][Min(0)] private int _initEquippedKart;                             // 최초 회원가입 시 장착하고 있을 카트 (인덱스)
        [SerializeField][Min(0)] private int _initGold;                                     // 최초 회원가입 시 가져야할 골드의 양
        [SerializeField][Min(0)] private int _initBlueHoneyGem;                             // 최초 회원가입 시 가져야할 블루허니잼의 양
        //[SerializeField][Min(0)] private int _initMoney3;                                 // 최초 회원가입 시 가져야할 돈3의 양
        //[SerializeField][Min(0)] private int _initExp = 100;                              // 최초 회원가입 시 가져야할 경험치의 양 (경험치 테이블은 100으로 되어 있음)

        [Header("Behaviour")]
        [SerializeField][Min(0)] private float _vanishSec = 5f; // 몇 초 뒤에 _infoText가 사라질 지

        private const int INIT_LEVEL = 1; // 1이 강화가 안된, 소지 여부만을 검증하는 레벨로 간주
        private string CurrentUid => FirebaseManager.Instance.Auth.CurrentUser.UserId;
        private Coroutine _textVanishCO;


        private void Start()
        {
            _infoText.text = string.Empty;
            _authFlowController.OnAuthSucceeded += CheckNicknameSet;
        }


        private void OnDisable()
        {
            if (_authFlowController != null)
            {
                _authFlowController.OnAuthSucceeded -= CheckNicknameSet;
            }

            StopVanishCO();
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
                else
                {
                    _tapToStartObj.SetActive(true); // 있으면 바로 다음 씬으로 넘어갈 수 있도록 함
                }
            });
        }

        public void OnClickSetNickName()
        {
            string newNickname = _nicknameInputField.text;

            // TODO: 추가적으로 닉네임 사용 규칙 할거면 여기서 하면 될 듯
            if (string.IsNullOrEmpty(newNickname))
            {
                StartVanishCO("사용할 닉네임을 입력해주세요.");
                return;
            }

            if (newNickname.Length <= 2 || newNickname.Length > 16)
            {
                StartVanishCO("닉네임은 3자 이상 16자 이하여야 합니다.");
                return;
            }

            DatabaseManager.Instance.GetOnMain(DBRoutes.Nicknames(newNickname), snap =>
            {
                if (snap.Exists)
                {
                    StartVanishCO($"닉네임: {newNickname}가 이미 존재합니다.");
                }
                else
                {
                    Dictionary<string, object> updates = new()
                    {
                        { DBRoutes.Nickname(FirebaseManager.Instance.Auth.CurrentUser.UserId), newNickname}, // users/{uid}/nickname에 자신의 닉네임 설정
                        { DBRoutes.Nicknames(newNickname), FirebaseManager.Instance.Auth.CurrentUser.UserId} // 빠른 조회를 위해 역인덱스로 nicknames/{newNickname}에 uid 설정
                    };

                    DatabaseManager.Instance.UpdateOnMain(updates,
                        onSuccess: () =>
                        {
                            Debug.Log($"{newNickname}로 닉네임 설정 완료. 다음으로 넘어갈 수 있다는 안내 문구 띄우기");
                            _tapToStartObj.SetActive(true);
                            _nicknameObj.SetActive(false);
                        },
                        onError: err => Debug.LogWarning($"닉네임 설정 오류: {err}")
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

            updates[DBRoutes.Gold(CurrentUid)] = _initGold;
            updates[DBRoutes.BlueHoneyGem(CurrentUid)] = _initBlueHoneyGem;
            //updates[DBRoutes.Money3(CurrentUid)] = _initMoney3;

            //updates[DBRoutes.Experience(CurrentUid)] = _initExp;


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

        private void StartVanishCO(string text)
        {
            if (_textVanishCO != null)
            {
                StopCoroutine(_textVanishCO);
                _textVanishCO = null;
            }
            _textVanishCO = StartCoroutine(TextVanishRoutine(text));
        }

        private void StopVanishCO()
        {
            if (_textVanishCO != null)
            {
                StopCoroutine(_textVanishCO);
                _textVanishCO = null;
            }
        }

        private IEnumerator TextVanishRoutine(string text)
        {
            _infoText.text = text;
            yield return new WaitForSeconds(_vanishSec);
            _infoText.text = string.Empty;
        }
    }
}
