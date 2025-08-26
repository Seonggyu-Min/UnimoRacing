using Firebase.Database;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    public class DatabaseTester : MonoBehaviour
    {
        private string uid => "testUser";

        // 1) wins = 0으로 쓰기
        public void Btn_SetWinsZero()
        {
            DatabaseManager.Instance.SetOnMain(DBRoutes.Wins(uid), 0);
        }

        // 2) wins 읽기
        public void Btn_ReadWins()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.Wins(uid), (DataSnapshot s) => Debug.Log($"승리 횟수: {s.Value}"));
        }

        // 3) wins +1 트랜잭션
        public void Btn_IncrementWins()
        {
            DatabaseManager.Instance.IncrementToLongOnMainWithTransaction(DBRoutes.Wins(uid), +1);
        }

        // 4) 멀티 업데이트
        public void Btn_UpdateStatsMulti()
        {
            var updates = new Dictionary<string, object>
            {
                { DBRoutes.Wins(uid), 1 },
                { DBRoutes.Losses(uid), 1 }
            };

            DatabaseManager.Instance.UpdateOnMain(updates);
        }

        // 5) stats 전체 읽기
        public void Btn_ReadStats()
        {
            DatabaseManager.Instance.GetOnMain(DBRoutes.Stats(uid), (DataSnapshot s) => Debug.Log(s.GetRawJsonValue()));
        }

        // 6) wins 삭제
        public void Btn_RemoveWins()
        {
            DatabaseManager.Instance.RemoveOnMain(DBRoutes.Wins(uid));
        }

        // 7) 사용자 정의 트랜잭션 (score가 없으면 1000으로 세팅)
        public void Btn_CustomTransaction()
        {
            DatabaseManager.Instance.RunTransactionOnMain(
                DBRoutes.Losses(uid),
                mutable =>
                {
                    // 현재 값 읽기
                    long current = 0;
                    try
                    {
                        if (mutable.Value != null) current = Convert.ToInt64(mutable.Value);
                    }
                    catch { }

                    if (current == 0) current = 1000;
                    mutable.Value = current;
                    return TransactionResult.Success(mutable);
                }
            );
        }
    }
}
