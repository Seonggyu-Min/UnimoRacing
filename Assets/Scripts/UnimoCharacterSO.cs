using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewUnimoCharacterSO", menuName = "Unimo/Character")]
public class UnimoCharacterSO : ScriptableObject
{
    [Header("ID & 기본 정보")]
    [Tooltip("테이블의 '캐릭터 ID'")]
    public int characterId;

    [Tooltip("테이블의 '캐릭터 이름'")]
    public string characterName;

    [Tooltip("테이블의 '캐릭터 이름'")]
    public GameObject characterPrefab;

    [Tooltip("테이블의 '캐릭터 스프라이트'")]
    public Sprite characterSprite;

    [Tooltip("재화 타입 (예: GameMoney, Cash)")]

    public Sprite currencyType;

    [TextArea]
    [Tooltip("테이블의 '캐릭터 설명'")]
    public string characterInfo;

    [Header("연관/시너지")]
    [Tooltip("테이블의 '시너지 차량 ID'")]
    public int SynergyKartID = -1;

    [Tooltip("테이블의 '관계 캐릭터 ID'")]
    public int relationCharacterId = -1;

    [Tooltip("테이블의 '대사 ID'")]
    public int dialogId = -1;

    [Header("Addressables 전환")]
    [Tooltip("true면 에디터에서도 직참조를 자동으로 비워 Addressables만 사용")]
    public bool useAddr = true;

    [Tooltip("캐릭터 프리팹(주소 참조)")]
    public AssetReferenceGameObject characterPrefabRef;

    [Tooltip("캐릭터 스프라이트(주소 참조)")]
    public AssetReferenceSprite characterSpriteRef;

    public async Task<GameObject> EnsureCharacterPrefabAsync()
    {
        if (characterPrefab) return characterPrefab;

        if (characterPrefabRef != null && characterPrefabRef.RuntimeKeyIsValid())
        {
            characterPrefab = await YTW.ResourceManager.Instance.LoadRefAsync<GameObject>(characterPrefabRef);
        }
        return characterPrefab;
    }

    public async Task<Sprite> EnsureCharacterSpriteAsync()
    {
        if (characterSprite) return characterSprite;

        if (characterSpriteRef != null && characterSpriteRef.RuntimeKeyIsValid())
        {
            characterSprite = await YTW.ResourceManager.Instance.LoadRefAsync<Sprite>(characterSpriteRef);
        }
        return characterSprite;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return; 
        // 에디터에서 useAddr 켜두면, 직참조를 즉시 비움
        if (useAddr && (characterPrefab != null || characterSprite != null))
        {
            characterPrefab = null;
            characterSprite = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
