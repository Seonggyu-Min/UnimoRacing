using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class PlayerBuffSystem : MonoBehaviourPun
{
    // 카테고리별 마지막 버프 빠르게 찾기 + 전체 조회
    private readonly Dictionary<BuffCategory, AppliedBuff> _byCategory = new();
    private readonly Dictionary<BuffId, AppliedBuff> _byId = new();

    public event Action<AppliedBuff> OnBuffAdded;
    public event Action<AppliedBuff> OnBuffRefreshed;
    public event Action<AppliedBuff> OnBuffRemoved;

    private void Update()
    {
        // 만료 처리
        // 복사 후 순회(딕셔너리 수정 충돌 방지)
        var expired = ListPool<AppliedBuff>.Get(); // 임시 리스트(없으면 그냥 new List<AppliedBuff>())
        foreach (var kv in _byId)
        {
            if (kv.Value.IsExpired) expired.Add(kv.Value);
        }
        foreach (var b in expired) RemoveInternal(b);
        ListPool<AppliedBuff>.Release(expired);
    }

    // 로컬(owner)에서 호출: 아이템/트리거로 버프 적용
    public void ApplyBuffLocal(BuffId id, float? overrideDuration = null)
    {
        if (!photonView.IsMine)
        {
            Debug.LogWarning("ApplyBuffLocal은 owner만 호출해야 해.");
            return;
        }

        if (!BuffCatalog.TryGet(id, out var cat, out var policy, out var baseDur))
            return;

        float dur = overrideDuration ?? baseDur;
        photonView.RPC(nameof(RPC_ApplyBuff), RpcTarget.AllBuffered, (int)id, (int)cat, (int)policy, dur);
    }

    [PunRPC]
    private void RPC_ApplyBuff(int idRaw, int catRaw, int policyRaw, float duration)
    {
        var id = (BuffId)idRaw;
        var cat = (BuffCategory)catRaw;
        var policy = (BuffStackPolicy)policyRaw;

        if (_byCategory.TryGetValue(cat, out var exist))
        {
            if (exist.Id == id)
            {
                exist.Refresh(duration);
                OnBuffRefreshed?.Invoke(exist);
                return;
            }
            else
            {
                if (policy == BuffStackPolicy.Replace)
                {
                    RemoveInternal(exist);
                }
                // Stack 정책이면 동시 존재 허용도 가능하지만,
                // 카테고리 기준은 보통 치환을 권장(중복 가속 방지).
            }
        }

        // 새로 추가
        var applied = new AppliedBuff(id, cat, policy, duration);
        _byId[id] = applied;
        _byCategory[cat] = applied;
        OnBuffAdded?.Invoke(applied);
    }

    private void RemoveInternal(AppliedBuff buff)
    {
        if (buff == null) return;
        _byId.Remove(buff.Id);
        if (_byCategory.TryGetValue(buff.Category, out var cur) && cur == buff)
            _byCategory.Remove(buff.Category);
        OnBuffRemoved?.Invoke(buff);
    }

    // --- 조회 API ---
    public bool HasBuff(BuffId id) => _byId.TryGetValue(id, out var b) && !b.IsExpired;
    public bool TryGetBuff(BuffId id, out AppliedBuff buff) => _byId.TryGetValue(id, out buff) && !buff.IsExpired;
    public float GetRemaining(BuffId id) => _byId.TryGetValue(id, out var b) ? b.RemainingSeconds : 0f;

    // 디버그용: 현재 버프 전부
    public IReadOnlyCollection<AppliedBuff> GetAll() => _byId.Values;
}

// 간단한 리스트 풀(원하면 제거해도 됨)
public static class ListPool<T>
{
    static readonly Stack<List<T>> _pool = new();
    public static List<T> Get() => _pool.Count > 0 ? _pool.Pop() : new List<T>(8);
    public static void Release(List<T> list) { list.Clear(); _pool.Push(list); }
}
