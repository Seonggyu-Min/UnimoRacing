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

        // 이미 생성한 인연쌍 기록용 (작은 ID, 큰 ID)
        HashSet<(int, int)> createdPairs = new HashSet<(int, int)>();

        foreach (var character in characters)
        {
            if (character.relationCharacterId <= 0)
                continue;

            if (!UnimoKartDatabase.Instance.TryGetByUnimoIndex(character.relationCharacterId, out var related))
            {
                Debug.LogWarning($"[RelationListUI] {character.characterId} → {character.relationCharacterId} 인연을 찾을 수 없음");
                continue;
            }

            // ID 순서 상관없이 동일한 인연쌍으로 인식
            int a = Mathf.Min(character.characterId, related.characterId);
            int b = Mathf.Max(character.characterId, related.characterId);

            // 이미 처리된 인연쌍이면 건너뛰기
            if (createdPairs.Contains((a, b)))
                continue;

            createdPairs.Add((a, b));

            Debug.Log($"[RelationListUI] 인연 생성: {a} ↔ {b}");

            RelationItem item = Instantiate(relationItemPrefab, contentParent);

            // ID 기준으로 왼쪽/오른쪽 배치 결정
            var leftChar = (character.characterId == a) ? character : related;
            var rightChar = (leftChar == character) ? related : character;

            // 이미지 설정
            item.heartImage.sprite = heartSprite;
            ItemPreviewManager.Instance.BindUnimoPreview(leftChar.characterId, item.leftRaw);
            ItemPreviewManager.Instance.BindUnimoPreview(rightChar.characterId, item.rightRaw);

            // 이름 설정
            item.leftNameText.text = leftChar.characterName;
            item.rightNameText.text = rightChar.characterName;
        }
    }
}