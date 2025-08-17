using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// DB 경로 접근 시 오타 방지 및 공통화를 위해 사용하는 클래스입니다.
    /// </summary>
    public static class DBRoutes
    {
        public static string Users(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid);
        public static string Stats(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.stats);
        public static string Wins(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.stats, DatabaseKeys.wins);
        public static string Score(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.stats, DatabaseKeys.score);
    }
}