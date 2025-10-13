using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class IAPButtonMaker : MonoBehaviour
    {
        [SerializeField] private IAPButtonBehaviour _buttonPrefab;
        [SerializeField] private IAPTable _iapTable;
        [SerializeField] private Transform _parent;

        private Dictionary<string, IAPButtonBehaviour> _spritesDict = new();


        private void Start()
        {
            IAPManager.Instance.OnProductLoaded += BuildUI;

            StartCoroutine(Wait());
        }

        private void OnDisable()
        {
            if (IAPManager.Instance != null)
            {
                IAPManager.Instance.OnProductLoaded -= BuildUI;
            }
        }

        [Button("BuildUI")]
        private void BuildUI()
        {
            Debug.Log("BuildUI 호출");

            if (_spritesDict != null && _spritesDict.Count > 0)
            {
                foreach (var button in _spritesDict)
                {
                    Destroy(button.Value.gameObject);
                }
            }
            _spritesDict.Clear();

            for (int i = 0; i < _iapTable.Entries.Count; i++)
            {
                var button = Instantiate(_buttonPrefab, _parent);
                button.Init(i, _iapTable, _iapTable.Entries[i].Sprite);
                _spritesDict.Add(_iapTable.Entries[i].ProductId, button);
            }
        }
        
        // 일단 이벤트를 못 받는 것 같아서 코루틴 썼는데 추후 수정 예정
        private IEnumerator Wait()
        {
            yield return new WaitForSeconds(1f);
            BuildUI();
        }
    }
}
