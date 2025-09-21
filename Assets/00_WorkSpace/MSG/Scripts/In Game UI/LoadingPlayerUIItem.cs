using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    public class LoadingPlayerUIItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nicknameText;
        [SerializeField] private Image _unimoImage;
        [SerializeField] private Color _selfNicknameColor = Color.yellow;
        [SerializeField] private Color _otherNicknameColor = Color.white;

        public void Init(string nickname, int unimoIndex, bool amISelf)
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
    }
}
