using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class MissionCsvImporter : MonoBehaviour
    {
        [Header("Target SO")]
        [SerializeField] private MissionPatchSO _targetSO;

        [Header("CSV Settings")]
        [SerializeField] private string dailyCsvPath;
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


        }
    }
}
