using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace MSG
{
    public class PlayerEffectManager : SceneSingleton<PlayerEffectManager>
    {
        #region Fields and Properties
        [Header("아이템 SO 등록")]
        [SerializeField] private List<UnimoItemSO> _itemList = new();
        [SerializeField] private List<UnimoPassiveSkillSO> _passiveSkillList = new();

        private Dictionary<string, PlayerAttatchWrapper[]> _wrappers = new();

        private Dictionary<ItemId, UnimoItemSO> _itemDict = new();                  // 빠른 조회용
        private Dictionary<int, UnimoPassiveSkillSO> _passiveSkillDict = new();     // 빠른 조회용

        private Dictionary<ItemId, Stack<GameObject>> _itemPool = new();                   // 프리팹 풀링용
        private Dictionary<int, Stack<GameObject>> _passiveSkillPool = new();              // 프리팹 풀링용


        [Header("디버깅 및 테스트용 필드")]
        [SerializeField] private bool _useItem = true;
        [SerializeField] private string _uid;

        [ShowField(nameof(_useItem))]
        [SerializeField] private ItemId _itemId;

        [HideField(nameof(_useItem))]
        [SerializeField] private int _passiveId;

        #endregion


        #region Unity Methods

        private void Awake()
        {
            MakeDictionary();
        }

        #endregion


        #region Public API Methods

        // 플레이어가 자동 등록하는 구조
        public void RegisterPoints(string uid, PlayerAttatchWrapper[] wrapper)
        {
            // 이미 카트 혹은 유니모 한 쪽에서 등록했으면 확장해서 다시 넣어주기
            if (_wrappers.ContainsKey(uid))
            {
                _wrappers.TryGetValue(uid, out PlayerAttatchWrapper[] existWrapper);

                PlayerAttatchWrapper[] newWrapper = existWrapper.Concat(wrapper).ToArray();
                _wrappers.Remove(uid);
                _wrappers.Add(uid, newWrapper);
            }
            // 처음이면 그냥 넣음
            else
            {
                _wrappers.Add(uid, wrapper);
            }
        }

        public void ShowItemEffect(string uid, ItemId id)
        {
            if (!_wrappers.TryGetValue(uid, out PlayerAttatchWrapper[] wrappers) || wrappers == null)
            {
                Debug.LogWarning($"[PlayerEffectManager] {uid}의 wrappers를 찾을 수 없습니다");
                return;
            }
            if (!_itemDict.TryGetValue(id, out UnimoItemSO itemSO) || itemSO.itemEffectPrefab == null)
            {
                Debug.LogWarning($"[PlayerEffectManager] {id}의 SO 혹은 프리팹 누락");
                return;
            }

            PlayerAttatchWrapper wrapper = wrappers.FirstOrDefault(i => i.AttatchmentType == itemSO.attatchmentType);
            Transform parentT = (wrapper != null && wrapper.AttachObj != null) ? wrapper.AttachObj.transform : transform;   // 없으면 일단 transform 쓰게 함

            Stack<GameObject> stack = GetItemStack(id);
            GameObject go = null;

            // 스택에서 비활성 오브젝트 찾기
            while (stack.Count > 0 && (go == null || go.activeInHierarchy))
            {
                go = stack.Pop();
                if (go == null)
                {
                    go = null; // pop이 null이면 나오기
                } 
                else if (go.activeInHierarchy)
                {
                    go = null; // 사용 중이면 새로 찾기
                }
            }

            // 쓸 수 있는게 없으면 새로 생성
            if (go == null)
            {
                go = Instantiate(itemSO.itemEffectPrefab, transform);
            }

            go.SetActive(true);
            go.transform.SetParent(parentT, false);
            go.transform.localPosition = itemSO.offset;
            go.transform.localRotation = Quaternion.Euler(itemSO.rotationOffset);

            StartCoroutine(WaitAndReturn(itemSO.itemEffectDuration, go, itemId: id));
        }

        public void ShowPassiveEffect(string uid, int id)
        {
            if (!_wrappers.TryGetValue(uid, out PlayerAttatchWrapper[] wrappers) || wrappers == null)
            {
                Debug.LogWarning($"[PlayerEffectManager] {uid}의 wrappers를 찾을 수 없습니다");
                return;
            }
            if (!_passiveSkillDict.TryGetValue(id, out UnimoPassiveSkillSO skillSO) || skillSO.itemEffectPrefab == null)
            {
                Debug.LogWarning($"[PlayerEffectManager] {id}의 SO 혹은 프리팹 누락");
                return;
            }

            PlayerAttatchWrapper wrapper = wrappers.FirstOrDefault(i => i.AttatchmentType == skillSO.attatchmentType);
            Transform parentT = (wrapper != null && wrapper.AttachObj != null) ? wrapper.AttachObj.transform : transform;

            Stack<GameObject> stack = GetPassiveStack(id);
            GameObject go = null;

            // 스택에서 비활성 오브젝트 찾기
            while (stack.Count > 0 && (go == null || go.activeInHierarchy))
            {
                go = stack.Pop();
                if (go == null)
                {
                    go = null; // pop이 null이면 나오기
                } 
                else if (go.activeInHierarchy)
                {
                    go = null; // 사용 중이면 새로 찾기
                }
            }

            // 쓸 수 있는게 없으면 새로 생성
            if (go == null)
            {
                go = Instantiate(skillSO.itemEffectPrefab, transform);
            }

            go.SetActive(true);
            go.transform.SetParent(parentT, false);
            go.transform.localPosition = skillSO.offset;
            go.transform.localRotation = Quaternion.Euler(skillSO.rotationOffset);

            StartCoroutine(WaitAndReturn(skillSO.itemEffectDuration, go, passiveId: id));
        }

        #endregion


        #region Private Methods

        private void MakeDictionary()
        {
            if (_itemList != null && _itemList.Count > 0)
            {
                _itemDict.Clear();

                foreach (var item in _itemList)
                {
                    _itemDict.Add(item.itemID, item);
                }
            }

            if (_passiveSkillList != null && _passiveSkillList.Count > 0)
            {
                _passiveSkillDict.Clear();

                foreach (var item in _passiveSkillList)
                {
                    _passiveSkillDict.Add(item.passiveSkillID, item);
                }
            }
        }

        private IEnumerator WaitAndReturn(float seconds, GameObject effectPrefab, ItemId? itemId = null, int? passiveId = null)
        {
            yield return new WaitForSeconds(seconds);

            if (effectPrefab == null)
            {
                Debug.LogWarning("[PlayerEffectManager] effectPrefab이 null입니다");
                yield break;
            }

            effectPrefab.SetActive(false);
            effectPrefab.transform.SetParent(transform, false);

            if (itemId.HasValue)
            {
                GetItemStack(itemId.Value).Push(effectPrefab);
            }
            else if (passiveId.HasValue)
            {
                GetPassiveStack(passiveId.Value).Push(effectPrefab);
            }
        }

        private Stack<GameObject> GetItemStack(ItemId id)
        {
            if (!_itemPool.TryGetValue(id, out Stack<GameObject> stack) || stack == null)
            {
                stack = new Stack<GameObject>();
                _itemPool[id] = stack;
            }
            return stack;
        }

        private Stack<GameObject> GetPassiveStack(int id)
        {
            if (!_passiveSkillPool.TryGetValue(id, out Stack<GameObject> stack) || stack == null)
            {
                stack = new Stack<GameObject>();
                _passiveSkillPool[id] = stack;
            }
            return stack;
        }

        #endregion


        #region Debug Methods

        [Button("Debug Dict")]
        private void DebugDictionary()
        {
            StringBuilder sb = new();

            sb.AppendLine("_itemList: ");
            foreach (var item in _itemDict)
            {
                sb.Append($"{item.Key.ToString()}번호의 이름: {item.Value.itemName}");
            }

            sb.AppendLine("_passiveSkillList:");
            foreach (var item in _passiveSkillDict)
            {
                sb.Append($"{item.Key.ToString()}번호의 이름: {item.Value.passiveSkillName}");
            }

            sb.AppendLine("_wrappers: ");
            foreach (var wrapper in _wrappers)
            {
                sb.Append($"{wrapper.Key}의 wrapper 수: {wrapper.Value.Length}개");
            }

            Debug.Log(sb.ToString());
        }

        [Button("Use Skill Effect")]
        private void UseSkillEffect()
        {
            if (_useItem)
            {
                ShowItemEffect(_uid, _itemId);
            }
            else
            {
                ShowPassiveEffect(_uid, _passiveId);
            }
        }

        #endregion
    }
}
