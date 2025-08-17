using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// 데이터베이스 노드 접근 시 사용하는 키들을 오타 방지를 위해 상수로 관리하는 클래스입니다.
    /// </summary>
    public static class DatabaseKeys
    {
        // 아래는 예시로 작성된 키들입니다.

        // -- User Data Keys --
        public const string users = "users";
        public const string name = "name";
        public const string email = "email";

        // -- Game Data Keys --
        public const string gold = "gold";
        public const string diamond = "diamond";
        public const string level = "level";
        public const string experience = "experience";

        // -- Inventory Keys --
        public const string inventory = "inventory";
        public const string itemId = "itemId";

        // -- Settings Keys --
        public const string settings = "settings";
        public const string soundEnabled = "soundEnabled";
        public const string musicEnabled = "musicEnabled";

        // -- Achievements Keys --
        public const string achievements = "achievements";
        public const string achievementId = "achievementId";
        public const string achievementStatus = "achievementStatus";

        // -- Leaderboard Keys --
        public const string stats = "stats";
        public const string score = "score";
        public const string rank = "rank";
        public const string wins = "wins";
        public const string losses = "losses";
    }
}
