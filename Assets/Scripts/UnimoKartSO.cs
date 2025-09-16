using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewUnimoKartSO", menuName = "Unimo/Kart")]
public class UnimoKartSO : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("차량 ID")]
    public int KartID;

    [Tooltip("차량 이름")]
    public string carName;

    [Tooltip("차량 프리팹")]
    public GameObject kartPrefab;

    [Tooltip("차량 스프라이트")]
    public Sprite kartSprite;

    [TextArea]
    [Tooltip("차량 설명")]
    public string carDesc;

    [Header("스킬")]
    [Tooltip("패시브 스킬 ID")]
    public int passiveSkillId;

    [Header("Addressables")]
    public bool useAddr = true;
    public AssetReferenceGameObject kartPrefabRef;
    public AssetReferenceSprite kartSpriteRef;

    public async Task<GameObject> EnsureKartPrefabAsync()
    {
        if (kartPrefab) return kartPrefab;
        if (kartPrefabRef == null || !kartPrefabRef.RuntimeKeyIsValid()) return null;
        kartPrefab = await YTW.ResourceManager.Instance.LoadRefAsync<GameObject>(kartPrefabRef);
        return kartPrefab;
    }

    public async Task<Sprite> EnsureKartSpriteAsync()
    {
        if (kartSprite != null) return kartSprite;
        if (kartSpriteRef == null || !kartSpriteRef.RuntimeKeyIsValid()) return null;

        var s = await YTW.ResourceManager.Instance.LoadRefAsync<Sprite>(kartSpriteRef);
        kartSprite = s;   
        return s;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return; // 플레이 중/직전엔 건드리지 않음
        // 에디터에서 useAddr 켜두면, 실수로 채운 직참조를 즉시 비움
        if (useAddr && (kartPrefab != null || kartSprite != null))
        {
            kartPrefab = null;
            kartSprite = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
