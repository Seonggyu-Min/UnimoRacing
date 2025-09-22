using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class PlayerUIItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nicknameText;
        [SerializeField] private Image _unimoImage;
        [SerializeField] private Color _selfNicknameColor = Color.yellow;
        [SerializeField] private Color _otherNicknameColor = Color.white;

        [SerializeField] private TMP_Text _rankText;    // 결과용 등수
        [SerializeField] private TMP_Text _timeText;    // 결과 화면에서 사용할 시간 텍스트
        [SerializeField] private Color _notInColor = new Color(1f, 0.6f, 0.6f);

        private readonly string fisrt = "1st";
        private readonly string second = "2nd";
        private readonly string third = "3rd";
        private readonly string fourth = "4th";


        public void InitForLoading(string nickname, int unimoIndex, bool amISelf)
        {
            _nicknameText.text = nickname;

            if (amISelf)
            {
                _nicknameText.color = _selfNicknameColor;
            }
            else
            {
                _nicknameText.color = _otherNicknameColor;
            }

            if (UnimoKartDatabase.Instance == null)
            {
                Debug.LogWarning("UnimoKartDatabase의 인스턴스가 null입니다.");
            }
            else
            {
                Debug.Log("[LoadingPlayerUIItem] 유니모 스프라이트 등록 시도");

                if (UnimoKartDatabase.Instance.TryGetByUnimoIndex(unimoIndex, out UnimoCharacterSO so))
                {
                    _unimoImage.sprite = so.characterSprite;
                    Debug.Log($"[LoadingPlayerUIItem] {so.characterSprite.name} 이 스프라이트로 등록됩니다.");
                }
                else
                {
                    Debug.Log($"[LoadingPlayerUIItem] {unimoIndex}번 유니모를 찾을 수 없습니다.");
                }
            }
        }

        public void InitForResult(string nickname, int unimoIndex, bool amISelf, int rank, bool finished, float time)
        {
            // 닉네임/색
            _nicknameText.text = nickname;
            _nicknameText.color = amISelf ? _selfNicknameColor : _otherNicknameColor;

            // 등수
            if (_rankText)
            {
                switch(rank)
                {
                    case 1:
                        _rankText.text = fisrt;
                        break;
                    case 2:
                        _rankText.text = second;
                        break;
                    case 3:
                        _rankText.text = third;
                        break;
                    case 4:
                        _rankText.text = fourth;
                        break;
                    default:
                        _rankText.text = rank.ToString() + "th";
                        break;
                }
            }

            // 유니모 이미지
            TrySetUnimoSprite(unimoIndex);

            // 시간
            if (_timeText != null)
            {
                if (finished)
                {
                    _timeText.text = FormatTime(time);   // mm:ss.mmm
                    _timeText.color = Color.white;
                }
                else
                {
                    _timeText.text = "Not In Yet";
                    _timeText.color = _notInColor;
                }
            }
        }

        private void TrySetUnimoSprite(int unimoIndex)
        {
            if (UnimoKartDatabase.Instance == null)
            {
                Debug.LogWarning("UnimoKartDatabase.Instance is null");
                return;
            }

            if (UnimoKartDatabase.Instance.TryGetByUnimoIndex(unimoIndex, out UnimoCharacterSO so))
            {
                _unimoImage.sprite = so.characterSprite;
            }
            else
            {
                Debug.Log($"Unimo {unimoIndex} not found.");
            }
        }

        private string FormatTime(float seconds)
        {
            // mm:ss.mmm
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remain = seconds - minutes * 60f;
            return $"{minutes:00}:{remain:00.000}";
        }
    }
}
