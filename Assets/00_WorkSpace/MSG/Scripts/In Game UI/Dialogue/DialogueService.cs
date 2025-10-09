using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;


namespace MSG
{
    public class DialogueService : MonoBehaviour
    {
        [SerializeField] private DialogueSO _dialogueSO;
        [SerializeField] private GameObject _billBoardCanvas;
        [SerializeField] private GameObject _dialogueObj;
        [SerializeField] private TMP_Text _dialogueText;
        [SerializeField] private Vector3 _offset = Vector3.up;

        // 디버그용
        private int myRelation;
        private List<int> racerCharacterIds = new();


        private void Start()
        {
            InGameManager.Instance.OnStateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            if (InGameManager.GetInstance != null)
            {
                InGameManager.Instance.OnStateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(RaceState state)
        {
            // 기다릴 때 텍스트 표기
            if (state == RaceState.Countdown)
            {
                StartCoroutine(TryShowRelationText());
            }

            // 시작 시 캔버스 비활성화
            else if (state == RaceState.Racing)
            {
                _billBoardCanvas.gameObject.SetActive(false);
                _dialogueObj.gameObject.SetActive(false);

                //StartCoroutine(TestWait());
            }
        }


        private IEnumerator TryShowRelationText()
        {
            yield return null;

            List<PlayerRaceData> racerData = new();
            racerData = FindObjectsOfType<PlayerRaceData>().ToList();       // 잘 안찾아져서 잠깐 FindObjectsOfType 사용

            //var racerData = InGameManager.Instance.PlayerRaceDatas;       // 이게 잘 안받아와지는 경우가 있는 듯??
            var myData = racerData.FirstOrDefault(d => d.View.IsMine);

            if (racerData != null)
            {
                foreach (var data in racerData)
                {
                    racerCharacterIds.Add(data.CharacterSO.characterId); // 디버그용
                }
            }
            else
            {
                Debug.LogWarning("[DialogueService] racerData가 null입니다.");
            }

            bool hasRelation = false;
            if (myData != null && myData.CharacterSO != null)
            {
                myRelation = myData.CharacterSO.relationCharacterId;

                foreach (var data in racerData)
                {
                    if (data != null)
                    {
                        if (data.CharacterSO.characterId == myRelation)
                        {
                            hasRelation = true;
                            break;
                        }
                    }
                }

                if (hasRelation)
                {
                    if (_dialogueSO != null)
                    {
                        var data = _dialogueSO.DialogueBoxes;
                        var datum = data.FirstOrDefault(d => d.DialogueIndex == myData.CharacterSO.dialogId);

                        if (datum != null)
                        {
                            _billBoardCanvas.SetActive(true);
                            _dialogueObj.SetActive(true);

                            Transform t = myData.transform;
                            _billBoardCanvas.transform.position = t.position + t.rotation * _offset;

                            //_billBoardCanvas.gameObject.transform.position = myData.gameObject.transform.position + _offset;

                            _dialogueText.text = datum.Text;
                        }
                        else
                        {
                            Debug.LogWarning("[DialogueService] datum이 null입니다");
                        }
                    }
                }
                else
                {
                    Debug.Log("[DialogueService] 관련된 유니모가 없어 대사를 출력하지 않습니다");
                }
            }
            else
            {
                Debug.LogWarning("[DialogueService] myData 또는 myData.CharacterSO가 null입니다.");
            }
        }

        private IEnumerator TestWait()
        {
            yield return new WaitForSeconds(10f);

            _billBoardCanvas.gameObject.SetActive(false);
            _dialogueObj.gameObject.SetActive(false);
        }
    }
}
