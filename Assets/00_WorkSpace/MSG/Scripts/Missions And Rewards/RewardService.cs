using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace MSG
{
    public class RewardService : MonoBehaviour
    {
        // TODO: 기본 보상, 차등 보상도 DB에서 가져와서 갱신해줘야 좋을 듯
        // 일단은 그냥 필드에서 갖고 있게 함

        //[SerializeField] private int _baseReward = 1000;                    // 기본 보상량
        //[SerializeField] private MoneyType _moneyType = MoneyType.Gold;     // 어떤 재화로 받을지

        //private void Start()
        //{
        //    ReInGameManager.Instance.OnRaceState_Finish += Give; // 모든 플레이어가 들어오면 호출. 그리고 PostGame은 코루틴 종료 후 Invoke 돼서 늦을 수도?
        //}

        //private void Give()
        //{
        //    // 자신이 몇 등인지 확인

        //    Dictionary<Player, float> playerDict = new();
        //    foreach (var p in PhotonNetwork.CurrentRoom.Players)
        //    {
        //        // 기억이 안나는데
        //        float finishedTime = 0f;
        //        if (!float.TryParse(ReInGameManager.Instance.GetPlayerProps<string>(p.Value), out finishedTime))
        //        {
        //            Debug.LogWarning("[RewardService] finishedTime 파싱 실패. 0으로 처리합니다");
        //        }

        //        playerDict.Add(p.Value, finishedTime);
        //    }

        //    // 완주 시간 기준 정렬 index로 등수 부여
        //    var ordered = playerDict
        //        .OrderBy(kv => kv.Value)
        //        .Select((kv, index) => new { Player = kv.Key, Time = kv.Value, Rank = index + 1})
        //        .ToList();

        //    // 로컬 플레이어의 순위 찾기
        //    var myResult = ordered.FirstOrDefault(o => o.Player == PhotonNetwork.LocalPlayer);
        //    if (myResult == null)
        //    {
        //        Debug.LogError("[RewardService] 내 순위를 찾을 수 없음");
        //        return;
        //    }

        //    float multiplier = 1.0f;
        //    switch (myResult.Rank)
        //    {
        //        case 1: 
        //            multiplier = 1.4f; 
        //            break;
        //        case 2: 
        //            multiplier = 1.2f; 
        //            break;
        //        case 3: 
        //            multiplier = 1.1f; 
        //            break;
        //        default: 
        //            multiplier = 1.0f; 
        //            break;
        //    }

        //    int rewardQuantity = Mathf.FloorToInt(_baseReward * multiplier); // 반올림
        //    RewardManager.Instance.AddMoney(_moneyType, rewardQuantity);
        //}
    }
}
