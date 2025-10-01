using EditorAttributes;
using System;
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

        private Dictionary<GameObject, Coroutine> _runningEffects = new();  // 코루틴 관리용

        private Dictionary<string, GameObject> _activeItemByKey = new();        // 같은 유저의 같은 효과 1개만 유지하기 위해
        private Dictionary<string, GameObject> _activePassiveByKey = new();     // 같은 유저의 같은 효과 1개만 유지하기 위해


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

            string key = MakeItemKey(uid, id);

            // 이미 같은 유저_같은 아이템 효과가 활성이라면
            if (itemSO.WillExtendWhenRepeating && _activeItemByKey.TryGetValue(key, out GameObject activeGo) && activeGo != null)
            {
                // 코루틴 중단 후 새 코루틴 시작
                RestartEffectTimer(activeGo, itemSO.itemEffectDuration, uid, itemId: id, passiveId: null);
                return;
            }

            Stack<GameObject> stack = GetItemStack(id);
            GameObject go = PopInactive(stack);
            if (go == null)
            {
                go = UnityEngine.Object.Instantiate(itemSO.itemEffectPrefab, transform);
            }

            go.SetActive(true);
            go.transform.SetParent(parentT, false);
            go.transform.localPosition = itemSO.offset;
            go.transform.localRotation = Quaternion.Euler(itemSO.rotationOffset);

            // 활성 맵 갱신
            _activeItemByKey[key] = go;

            // 타이머 시작
            StartEffectTimer(go, itemSO.itemEffectDuration, uid, itemId: id, passiveId: null);
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

            string key = MakePassiveKey(uid, id);

            // 이미 같은 유저_같은 패시브 효과가 활성이라면
            if (skillSO.WillExtendWhenRepeating && _activePassiveByKey.TryGetValue(key, out GameObject activeGo) && activeGo != null)
            {
                // 코루틴 중단 후 새 코루틴 시작
                RestartEffectTimer(activeGo, skillSO.itemEffectDuration, uid, itemId: null, passiveId: id);
                return;
            }

            Stack<GameObject> stack = GetPassiveStack(id);
            GameObject go = PopInactive(stack);
            if (go == null)
            {
                go = UnityEngine.Object.Instantiate(skillSO.itemEffectPrefab, transform);
            }

            go.SetActive(true);
            go.transform.SetParent(parentT, false);
            go.transform.localPosition = skillSO.offset;
            go.transform.localRotation = Quaternion.Euler(skillSO.rotationOffset);

            // 활성 맵 갱신
            _activePassiveByKey[key] = go;

            // 타이머 시작
            StartEffectTimer(go, skillSO.itemEffectDuration, uid, itemId: null, passiveId: id);
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
                    if (!_itemDict.ContainsKey(item.itemID))
                    {
                        _itemDict.Add(item.itemID, item);
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerEffectManager] 중복 ItemId: {item.itemID}");
                    }
                }
            }

            if (_passiveSkillList != null && _passiveSkillList.Count > 0)
            {
                _passiveSkillDict.Clear();

                foreach (var item in _passiveSkillList)
                {
                    if (!_passiveSkillDict.ContainsKey(item.passiveSkillID))
                    {
                        _passiveSkillDict.Add(item.passiveSkillID, item);
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerEffectManager] 중복 passiveSkillID: {item.passiveSkillID}");
                    }
                }
            }
        }

        private GameObject PopInactive(Stack<GameObject> stack)
        {
            GameObject go = null;
            while (stack != null && stack.Count > 0 && (go == null || go.activeInHierarchy))
            {
                go = stack.Pop();
                if (go == null) { go = null; }
                else if (go.activeInHierarchy) { go = null; }
            }
            return go;
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

        private void StartEffectTimer(GameObject effectGo, float duration, string uid, ItemId? itemId, int? passiveId)
        {
            // 이전 타이머 남아있다면 중단
            Coroutine prev;
            if (_runningEffects.TryGetValue(effectGo, out prev) && prev != null)
            {
                StopCoroutineSafe(prev);
            }

            Coroutine c = StartCoroutine(WaitAndReturn(duration, effectGo, uid, itemId, passiveId));
            _runningEffects[effectGo] = c;
        }

        private void RestartEffectTimer(GameObject effectGo, float duration, string uid, ItemId? itemId, int? passiveId)
        {
            Coroutine prev;
            if (_runningEffects.TryGetValue(effectGo, out prev) && prev != null)
            {
                StopCoroutineSafe(prev);
            }
            Coroutine c = StartCoroutine(WaitAndReturn(duration, effectGo, uid, itemId, passiveId));
            _runningEffects[effectGo] = c;
        }

        private IEnumerator WaitAndReturn(float seconds, GameObject effectPrefab, string uid, ItemId? itemId, int? passiveId)
        {
            yield return new WaitForSeconds(seconds);

            if (effectPrefab == null)
            {
                yield break;
            }

            effectPrefab.SetActive(false);
            effectPrefab.transform.SetParent(transform, false);

            // 풀에 반환
            if (itemId.HasValue)
            {
                GetItemStack(itemId.Value).Push(effectPrefab);

                // 활성 맵에서 제거
                string key = MakeItemKey(uid, itemId.Value);
                GameObject current;
                if (_activeItemByKey.TryGetValue(key, out current) && current == effectPrefab)
                {
                    _activeItemByKey.Remove(key);
                }
            }
            else if (passiveId.HasValue)
            {
                GetPassiveStack(passiveId.Value).Push(effectPrefab);

                string key = MakePassiveKey(uid, passiveId.Value);
                GameObject current;
                if (_activePassiveByKey.TryGetValue(key, out current) && current == effectPrefab)
                {
                    _activePassiveByKey.Remove(key);
                }
            }

            _runningEffects.Remove(effectPrefab);
        }

        private void StopCoroutineSafe(Coroutine c)
        {
            if (c != null)
            {
                StopCoroutine(c);
            }
        }

        private string MakeItemKey(string uid, ItemId id)
        {
            return uid + "_" + id.ToString();
        }

        private string MakePassiveKey(string uid, int id)
        {
            return uid + "_" + id.ToString();
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
