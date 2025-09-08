using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

public class PlayerSpawnDataInjecter : MonoBehaviour
{
    [Header("Setup Config")]
    [SerializeField] private    bool                    _isRandomUse = false; // 랜덤 사용 여부
    [SerializeField] private    int                     _useIndex = 0;

    // data
    [Header("Setup Data")]
    [SerializeField] private    List<PlayerSpawnData>   _playerSpawnDatas;

    public PlayerSpawnData GetData()
    {
        if (_playerSpawnDatas == null || _playerSpawnDatas.Count == 0)
        {
            this.PrintLog("인젝터 쪽, 리스트에 문제 발생", LogType.Error);
        }

        if (_isRandomUse)
            return _playerSpawnDatas[Random.Range(0, _playerSpawnDatas.Count)];

        return _playerSpawnDatas[_useIndex];
    }
}
