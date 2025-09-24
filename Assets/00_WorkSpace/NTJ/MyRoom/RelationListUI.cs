using MSG;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RelationListUI : MonoBehaviour
{
    [SerializeField] private Transform contentParent;
    [SerializeField] private RelationItem relationItemPrefab;
    [SerializeField] private Sprite heartSprite;

    private void Start()
    {
        PopulateRelationList();
    }

    private void PopulateRelationList()
    {
        var characters = UnimoKartDatabase.Instance.GetAllUnimos();
        Debug.Log($"[RelationListUI] 전체 유니모 개수: {characters.Count}");

        foreach (var character in characters)
        {
            if (character.relationCharacterId <= 0) continue;

            if (!UnimoKartDatabase.Instance.TryGetByUnimoIndex(character.relationCharacterId, out var related))
            {
                Debug.LogWarning($"[RelationListUI] {character.characterId} → {character.relationCharacterId} 인연을 찾을 수 없음");
                continue;
            }

            Debug.Log($"[RelationListUI] 인연 생성: {character.characterId} → {related.characterId}");

            RelationItem item = Instantiate(relationItemPrefab, contentParent);

            // 이미지 설정
            item.heartImage.sprite = heartSprite;
            ItemPreviewManager.Instance.BindUnimoPreview(character.characterId, item.leftRaw);
            ItemPreviewManager.Instance.BindUnimoPreview(related.characterId, item.rightRaw);

            // 이름 설정
            item.leftNameText.text = character.characterName;
            item.rightNameText.text = related.characterName;
        }
    }
}
