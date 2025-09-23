using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using YSJ.Util;

/// <summary>
/// 돌리 트랙 관리
/// </summary>
public class TrackPathRegistry : SimpleSingleton<TrackPathRegistry>
{
    private bool _isInit = false;

    [SerializeField] private bool _isPrintLog = false;
    [SerializeField] private List<CinemachinePathBase> _paths = new();

    public bool IsInit => _isInit;

    // 돌리 트랙 레일 찾고
    // 다 찾았다면, 
    protected override void Init()
    {
        base.Init();

        var findTrack = FindObjectsOfType<CinemachinePathBase>(true);
        SetTrack(findTrack);

        if (_paths.Count == 0)
            return;

        _isInit = true;
    }

    public void RePathLoad()
    {
        _paths?.Clear();
        var findTrack = FindObjectsOfType<CinemachinePathBase>(true);
        SetTrack(findTrack);

        if (_paths.Count == 0)
            return;

        _isInit = true;
    }

    public void SetTrack(params CinemachinePathBase[] paths)
    {
        if (_paths == null)
            return;

        foreach (var path in paths)
        {
            if (!_paths.Contains(path))
                _paths.Add(path);
        }
    }

    public int GetPathIndex(CinemachinePathBase path)
    {
        if (!_isInit
            || path == null
            || _paths == null
            || _paths.Count <= 0)
            return -1;

        for (int i = 0; i < _paths.Count; i++)
        {
            if (_paths[i] == path)
                return i;
        }

        return -1;
    }

    public int GetPlayerSetupPath(int playerListIndex)
    {
        if (!_isInit
            || _paths == null
            || _paths.Count <= 0
            || _paths.Count <= playerListIndex
            || 0 > playerListIndex)
            return -1;

        int index = playerListIndex % _paths.Count;
        return index;
    }

    public int GetPathLength()
    {
        if (!_isInit || _paths == null) return -1;

        return _paths.Count;
    }

    public CinemachinePathBase GetPath(int index = -1)
    {
        if (!_isInit)
        {
            this.PrintLog("초기화가 진행되지 않았습니다.");
            return null;
        }
        if (_paths == null)
        {
            this.PrintLog("_paths 가 비어 있습니다.");
            return null;
        }
        if (_paths.Count <= 0)
        {
            this.PrintLog("_paths.Count 가 0 이하 입니다.");
            return null;
        }
        if (_paths.Count <= index)
        {
            this.PrintLog("요청한 index가 리스트 크기 이상입니다.");
            return null;
        }
        if (0 > index)
        {
            this.PrintLog("index가 음수 입니다.");
            return null;
        }

        this.PrintLog($"패스가 정상 할당이 되어 리턴됩니다.(=> {_paths[index]})");
        return _paths[index];
    }
    public CinemachinePathBase GetLeftPath(int currentIndex)
    {
        if (!_isInit
            || _paths == null
            || _paths.Count <= 0
            || _paths.Count <= currentIndex
            || 0 > currentIndex)
            return null;

        int leftIndex = currentIndex - 1;
        if (0 > leftIndex || _paths.Count <= leftIndex)
            return null;

        return _paths[leftIndex];
    }
    public CinemachinePathBase GetRightPath(int currentIndex)
    {
        if (!_isInit
            || _paths == null
            || _paths.Count <= 0
            || _paths.Count <= currentIndex
            || 0 > currentIndex)
            return null;

        int rightIndex = currentIndex + 1;
        if (0 > rightIndex || _paths.Count <= rightIndex)
            return null;

        return _paths[rightIndex];
    }

    private void PrintLog(string printLog, LogType type = LogType.Log)
    {
        if (!_isPrintLog) return;
        UnityUtilEx.PrintLog(this, printLog, type);
    }
}
