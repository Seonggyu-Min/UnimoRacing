using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class FriendRequestPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nicknameText;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Image _unimoIcon;

        private string _pairId;
        private string _toUid;
        private string _fromUid;

        public void Init(string pairId, string toUid, string fromUid)
        {
            _pairId = pairId;
            _toUid = toUid;
            _fromUid = fromUid;

            LoadFriendInfo(_fromUid);
        }

        private void LoadFriendInfo(string uid)
        {
            DatabaseManager.Instance.GetOnMain(
                DBRoutes.Users(uid),
                snap =>
                {
                    // 닉네임
                    string nickname = "";
                    var nickSnap = snap.Child(DatabaseKeys.nickname);
                    if (nickSnap.Exists && nickSnap.Value != null)
                        nickname = nickSnap.Value.ToString();

                    _nicknameText.text = nickname;

                    // 레벨
                    int level = 1;
                    var expSnap = snap.Child(DatabaseKeys.gameData).Child(DatabaseKeys.experience);
                    if (expSnap.Exists && expSnap.Value != null)
                    {
                        long exp = 0;
                        long.TryParse(expSnap.Value.ToString(), out exp);
                        level = ExpToLevel.LevelFromTotalExp((int)exp);
                    }
                    _levelText.text = $"lv {level}";

                    // 유니모 아이콘
                    int equippedIndex = -1;
                    var unimoSnap = snap.Child(DatabaseKeys.equipped).Child(DatabaseKeys.unimos);
                    if (unimoSnap.Exists && unimoSnap.Value != null)
                        int.TryParse(unimoSnap.Value.ToString(), out equippedIndex);

                    if (UnimoKartDatabase.Instance.TryGetByUnimoIndex(equippedIndex, out UnimoCharacterSO unimo) &&
                        unimo != null && unimo.characterSprite != null)
                    {
                        _unimoIcon.sprite = unimo.characterSprite;
                    }
                },
                err => Debug.LogWarning($"[FriendRequestPanel] 유저 정보 로드 실패: {err}")
            );
        }

        public void OnClickAccept()
        {
            FriendsLogics.Instance.AcceptRequest(
                _pairId,
                _toUid,
                () => Debug.Log("친구 수락 성공"),
                err => Debug.LogWarning($"{err}")
            );
        }

        public void OnClickReject()
        {
            FriendsLogics.Instance.RejectRequest(
                _pairId,
                _toUid,
                () => Debug.Log("친구 거절 성공"),
                err => Debug.LogWarning($"{err}")
            );
        }
    }
}