using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MissionListLoader : MonoBehaviour
    {
        [SerializeField] private MissionUnitBehaviour _uiPrefab;
        [SerializeField] private Transform _dailyParent;        // 데일리 미션 프리팹 부모
        [SerializeField] private Transform _achievementParent;  // 도전과제 미션 프리팹 부모


        private void OnEnable()
        {
            RenewUI();
        }


        public void OnClickCloseButton()
        {
            UIManager.Instance.Hide("Mission Panel");
        }


        [Button("RenewUI")]
        private void RenewUI()
        {
            MissionService.Instance.LoadUserMissions(
                RenderList,
                err => Debug.LogWarning($"[MissionListLoader] 미션 조회 실패: {err}")
            );
        }

        // 일단 풀링 안함
        private void RenderList(List<MissionWrapper> list)
        {
            if (list == null)
            {
                list = new List<MissionWrapper>();
            }

            // UI 프리팹 파괴
            ClearChildren(_dailyParent);
            ClearChildren(_achievementParent);

            // 정렬: 클리어했으나 수령 안함 -> 진행 중 -> 수령 완료
            // 이후 인덱스 오름차순 정렬함
            list.Sort((a, b) =>
            {
                int g = a.MissionGroup.CompareTo(b.MissionGroup);
                if (g != 0)
                {
                    return g;
                }

                int r = Arrange(a).CompareTo(Arrange(b));
                if (r != 0)
                {
                    return r;
                }

                return a.MissionEntry.Index.CompareTo(b.MissionEntry.Index);
            });

            // 프리팹 생성
            foreach (var w in list)
            {
                Transform parent;
                if (w.MissionGroup == MissionGroup.Daily)
                {
                    parent = _dailyParent;
                }
                else
                {
                    parent = _achievementParent;
                }

                if (parent == null)
                {
                    Debug.LogWarning("[MissionListLoader] Parent가 null입니다.");
                    continue;
                }

                var item = Instantiate(_uiPrefab, parent);
                item.Init(w);
            }
        }

        private void ClearChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                Destroy(t.GetChild(i).gameObject);
            }
        }

        private int Arrange(MissionWrapper w)
        {
            if (w.Cleared && !w.Claimed) return 0; // 클리어했으나 수령 안함
            if (!w.Claimed) return 1;              // 진행 중
            return 2;                              // 수령 완료
        }
    }
}
