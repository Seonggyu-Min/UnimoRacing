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

        public void Init(string nickname, int unimoIndex)
        {
            _nicknameText.text = nickname;

            if (UnimoKartDatabase.Instance == null)
            {
                Debug.LogWarning("UnimoKartDatabase의 인스턴스가 null입니다.");
            }
            else
            {
                if (UnimoKartDatabase.Instance.TryGetByUnimoIndex(unimoIndex, out UnimoCharacterSO so))
                {
                    _unimoImage.sprite = so.characterSprite;
                }
            }
        }
    }
}
