using UnityEngine;
using YSJ;
using YSJ.Util;

public class UnimoPassiveSkillSystem : MonoBehaviour
{
    private bool _isSetup = false;

    private PlayerRaceData _data;
    private UnimoPassiveSkillSO _passiveSkill;
    //  private PlayerItemInventory _inventory;
    private ItemInventory _inventory;

    private int _triggerCurrentCount = 0;

    public bool IsSetup => _isSetup;

    public void Setup(PlayerRaceData data)
    {
        _data = data;

        int loadPassiveSkillID = _data.KartSO.passiveSkillId;
        if (loadPassiveSkillID < 0)
        {
            this.PrintLog("이상한 Passive Skill ID가 이상합니다. [load Passive Skill ID: {loadPassiveSkillID}]");
            return;
        }

        _passiveSkill = Resources.Load<UnimoPassiveSkillSO>($"{LoadPath.PLAYER_UNIMO_PASSIVE_SKILL_SO}_{loadPassiveSkillID}");
        if (_passiveSkill == null)
        {
            this.PrintLog($"Unimo Passive Skill SO가 로드 하지못했습니다. [Load Path: {LoadPath.PLAYER_UNIMO_PASSIVE_SKILL_SO}_{loadPassiveSkillID}]");
            return;
        }

        _inventory = GetComponentInChildren<ItemInventory>();
        if (_inventory == null)
        {
            this.PrintLog($"inventory가 존재 하지않습니다.");
            return;
        }

        var type = _passiveSkill.passiveSkillType;
        switch (type)
        {
            // 조건부 강화
            case PassiveSkillType.ConditionalEnhancement:
                _inventory.OnSaveItem -= OnItemConditionalEnhancement;
                _inventory.OnSaveItem += OnItemConditionalEnhancement;
                break;
            // 아이템 생성
            case PassiveSkillType.Creation:
                _inventory.OnSaveItem -= OnItemCreation;
                _inventory.OnSaveItem += OnItemCreation;
                break;
            // 아이템 방어
            case PassiveSkillType.Defense:
                _data.CollisionActionCmp.OnTriggerEnterAction -= OnItemDefense;
                _data.CollisionActionCmp.OnTriggerEnterAction += OnItemDefense;
                break;
            // 아이템 강화
            case PassiveSkillType.Enhancement:
                _inventory.OnSaveItem -= OnItemEnhancement;
                _inventory.OnSaveItem += OnItemEnhancement;
                break;
            default:
                this.PrintLog($"스킬 타입이 지정 되지않았습니다. [타입이 지정되지 않은 패시브 스킬 ID: {loadPassiveSkillID}]");
                break;
        }

        _isSetup = true;
    }

    #region Func About ConditionalEnhancement
    private void OnItemConditionalEnhancement(UnimoItemSO item)
    {

    }
    #endregion

    #region Func About Creation
    private void OnItemCreation(UnimoItemSO item)
    {
        int checkID = (int)item.itemID;
        int triggerID = (int)_passiveSkill.triggerItemID;

        if (_passiveSkill.triggerCount == -1)
            return;

        // 카운팅
        if (_triggerCurrentCount < _passiveSkill.triggerCount)
            _triggerCurrentCount++;


        // 조건에 맞는다면
        if (_triggerCurrentCount >= _passiveSkill.triggerCount)
        {
            // 카운팅 초기화
            _triggerCurrentCount = 0;
            // 조건이 맞을 때, 리워드 아이템 아이디 가지고 오기
            // UnimoItemSO reward = ItemManager.Instance.GetItemSOById((int)_passiveSkill.triggerRewardItemID);
            // 보상 아이템 추가
            // _inventory.SaveItem(reward, true);
        }
    }
    #endregion

    #region Func About Defense
    private void OnItemDefense(Collider item)
    {
        // 보류
    }
    #endregion

    #region Func About Enhancement
    private void OnItemEnhancement(UnimoItemSO item)
    {
        if (!_isSetup)
        {
            this.PrintLog("Setup이 되지 않았습니다.");
            return;
        }

        if (item == null)
        {
            this.PrintLog("item이 없어서 Enhancement를 진행 할 수 없습니다.");
            return;
        }

        // 저장된 후에 들어오는 함수임

        // 저장된 것 중에 트리거 아이템 ID 을 찾는다. 
        var so = _inventory.FindItemByID((int)_passiveSkill.triggerItemID);
        // 기존 인벤에 아이템을 제거
        _inventory.RemoveItemBySO(so);

        // 조건이 맞을 때, 리워드 아이템 아이디 가지고 오기
        // UnimoItemSO reward = ItemManager.Instance.GetItemSOById((int)_passiveSkill.triggerRewardItemID);

        // 보상 아이템을 넣어줄 때 
        // - 지연 저장 > 가능
        // - 무조건 저장 > 가능
        // - 아이템이 저장될 때 같이 실행 되는 액션 실행 > 불가능
        // _inventory.SaveItem(reward, true, true, false);
    }
    #endregion
}