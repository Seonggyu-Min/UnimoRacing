using CustomUtility.IO;
using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace MSG
{
    public class MissionCsvImporter : MonoBehaviour
    {
        [Header("Target SO")]
        [SerializeField] private MissionPatchSO _targetSO;

        [Header("CSV Settings")]
        [SerializeField] private string dailyCsvPath; // Application.dataPath -> Assets 하위 경로
        [SerializeField] private string achvCsvPath;
        [SerializeField] private char separator = ',';

        [Button("Import CSV to SO", 50f)]
        /*private*/ public void Import()
        {
            if (_targetSO == null)
            {
                Debug.LogError("[MissionCsvImporter] _targetSO가 null입니다.");
                return;
            }

            try
            {
                var dailies = LoadEntriesFromCsv(dailyCsvPath, separator);
                var achvs = LoadEntriesFromCsv(achvCsvPath, separator);

                _targetSO.Dailies.Clear();
                _targetSO.Dailies.AddRange(dailies);

                _targetSO.Achievements.Clear();
                _targetSO.Achievements.AddRange(achvs);

#if UNITY_EDITOR
                EditorUtility.SetDirty(_targetSO);
                AssetDatabase.SaveAssets();
#endif
                Debug.Log($"[MissionCsvImporter] Imported: daily={dailies.Count}, achv={achvs.Count}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[MissionCsvImporter] Import failed: {e}");
            }
        }


        private static List<MissionEntry> LoadEntriesFromCsv(string relativePath, char sep)
        {
            var tbl = new CsvTable(relativePath, sep);
            CsvReader.Read(tbl);

            if (tbl.Table == null)
            {
                throw new Exception($"CSV table is null: {relativePath}");
            }

            int rows = tbl.Table.GetLength(0);
            int cols = tbl.Table.GetLength(1);
            if (rows < 4)
            {
                throw new Exception($"Unexpected row count (<4): {relativePath}");
            }

            // 0: 한글 제목, 1: 영문 제목, 2: 타입, 3 ~: 데이터 라인
            int headerRowEN = 1;
            int dataStart = 3;

            // 컬럼 키 수집
            var keys = new string[cols];
            for (int c = 0; c < cols; c++)
            {
                keys[c] = (tbl.Table[headerRowEN, c] ?? "").Trim();
            }

            int idxMissionId = FindCol(keys, "Mission_ID");
            int idxTitle = FindCol(keys, "Mission_Label");
            int idxType = FindCol(keys, "Mission_Type");
            int idxDesc = FindCol(keys, "Mission_Info");
            int idxRewardType = FindCol(keys, "Reward_Type");
            int idxRewardAmount = FindCol(keys, "Reward_Amount");
            int idxCount = FindCol(keys, "Mission_Count");
            int idxVerb = FindCol(keys, "Mission_Verb");
            int idxObject = FindCol(keys, "Mission_Object");
            int idxParty = FindCol(keys, "Party_Condition");

            List<MissionEntry> list = new();

            for (int r = dataStart; r < rows; r++)
            {
                string idCell = tbl.Table[r, idxMissionId];
                if (string.IsNullOrWhiteSpace(idCell)) continue;
                if (!int.TryParse(idCell, out int id))
                {
                    Debug.LogWarning($"[MissionCsvImporter] Invalid Mission_ID at row {r}: '{idCell}'");
                    continue;
                }

                var entry = new MissionEntry
                {
                    Index = id,
                    Title = Get(tbl, r, idxTitle),
                    Description = Get(tbl, r, idxDesc),
                    RewardQuantity = ToInt(Get(tbl, r, idxRewardAmount)),
                    TargetCount = ToInt(Get(tbl, r, idxCount)),
                    MissionType = ParseMissionType(Get(tbl, r, idxType)),
                    MoneyType = ParseMoneyType(Get(tbl, r, idxRewardType)),
                    MissionVerb = ParseMissionVerb(Get(tbl, r, idxVerb)),
                    MissionObject = ParseMissionObject(Get(tbl, r, idxObject)),
                    PartyCondition = ParsePartyCondition(Get(tbl, r, idxParty)),
                };

                list.Add(entry);
            }

            return list;
        }

        #region Helper Methods

        private static int FindCol(string[] keys, string name)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (string.Equals(keys[i], name)) return i;
            }
            throw new Exception($"Column '{name}' not found in header.");
        }
        private static string Get(CsvTable t, int r, int c) => (c >= 0 ? t.Table[r, c] : "") ?? "";
        private static int ToInt(string s) => int.TryParse(s, out var v) ? v : 0;

        #endregion


        #region Enum Parse Methods

        private static MissionType ParseMissionType(string s)
        {
            s = (s ?? "").Replace(" ", "");
            if (s.Contains("완주")) return MissionType.RaceFinish;
            if (s.Contains("교체")) return MissionType.Change;
            if (s.Contains("아이템")) return MissionType.Item;
            if (s.Contains("수집")) return MissionType.Collect;
            if (s.Contains("획득")) return MissionType.Obtain;
            return MissionType.RaceFinish;
        }
        private static MoneyType ParseMoneyType(string s)
        {
            s = (s ?? "").Replace(" ", "");
            if (s.Contains("무료재화")) return MoneyType.Gold;
            if (s.Contains("유료재화")) return MoneyType.BlueHoneyGem;
            return MoneyType.Gold;
        }
        private static MissionVerb ParseMissionVerb(string s)
        {
            s = (s ?? "").Trim();
            if (Enum.TryParse<MissionVerb>(s, true, out var e)) return e;
            if (s.Contains("완주")) return MissionVerb.Finish;
            if (s.Contains("교체")) return MissionVerb.Change;
            if (s.Contains("수집")) return MissionVerb.Collect;
            if (s.Contains("사용")) return MissionVerb.Use;
            if (s.Contains("획득")) return MissionVerb.Obtain;
            return MissionVerb.Obtain;
        }
        private static MissionObject ParseMissionObject(string s)
        {
            s = (s ?? "").Trim();
            if (Enum.TryParse<MissionObject>(s, true, out var e)) return e;
            if (s.Contains("레이스")) return MissionObject.Race;
            if (s.Contains("엔진")) return MissionObject.Engine;
            if (s.Contains("유니모")) return MissionObject.Unimo;
            if (s.Contains("아이템")) return MissionObject.Item;
            if (s.Contains("골드")) return MissionObject.Gold;
            if (s.Replace(" ", "").Contains("블루허니잼")) return MissionObject.BluyHoneyGem;
            return MissionObject.Race;
        }
        private static PartyCondition ParsePartyCondition(string s)
        {
            s = (s ?? "").Trim();
            if (Enum.TryParse<PartyCondition>(s, true, out var e)) return e;
            if (s.Contains("파티")) return PartyCondition.True;
            if (s.Contains("솔로") || s.Contains("혼자")) return PartyCondition.False;
            return PartyCondition.Any;
        }

        #endregion
    }
}
