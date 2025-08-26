using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    [CreateAssetMenu(fileName = "UnimoSO", menuName = "ScriptableObjects/UnimoSO")]
    public class UnimoSO : ScriptableObject
    {
        [field: SerializeField] public string Name { get; private set; }
        [field: SerializeField] public int Index { get; private set; }
        [field: SerializeField] public string Description { get; private set; }
        [field: SerializeField] public UnimoSO SynergyKart { get; private set; } // 시너지 있는 캐릭터 1개 등록
        [field: SerializeField] public int Price { get; private set; } // 가격

        // ----- 상점 프리뷰용 -----

        // 1. 오브젝트를 스프라이트로 구운 뒤 사용하는 방법
        // 성능 좋음
        // 움직이지 못함

        // 2. RenderTexture를 사용하는 방법
        // 성능 안좋음
        // 움직이게 할 수 있음

        // 일단 임시로 Sprite로 사용
        [field: SerializeField] public Sprite Thumbnail { get; private set; }

    }
}
