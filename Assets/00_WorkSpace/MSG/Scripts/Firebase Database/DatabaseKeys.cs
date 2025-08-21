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
        public const string nickname = "nickname";
        public const string nicknames = "nicknames"; // 닉네임 중복 확인용 역인덱스
        public const string email = "email";

        // -- Friend Data Keys --
        public const string friends = "friends";
        public const string list = "list"; // 친구 목록

        // -- Equipped Keys --
        public const string equipped = "equipped";
        public const string karts = "karts";
        public const string unimos = "unimos";

        // -- Inventory Keys --
        public const string inventory = "inventory";

        // -- Achievements Keys --
        //public const string achievements = "achievements";
        //public const string achievementId = "achievementId";
        //public const string achievementStatus = "achievementStatus";

        // -- Stats Keys --
        public const string stats = "stats";
        public const string wins = "wins";
        public const string losses = "losses";

        // -- Game Data Keys --
        public const string gameData = "gameData";
        public const string money1 = "money1";
        public const string money2 = "money2";
        public const string money3 = "money3";
        public const string level = "level";
        public const string experience = "experience";

        // -- Daily Mission Keys --
        public const string dailyMissions = "dailyMissions";

        // -- Friends Keys --
        public const string friendLinks = "friendLinks";
        public const string from = "from"; // 친구 요청을 보낸 사람의 ID
        public const string to = "to"; // 친구 요청을 받은 사람의 ID
        public const string requestedAt = "requestedAt"; // 친구 요청을 보낸 시간
        public const string acceptedAt = "acceptedAt"; // 친구 요청이 수락된 시간

        public const string status = "status"; // 친구 요청 상태 (예: pending, accepted, rejected)
        public const string pending = "pending"; // 친구 요청 대기 상태
        public const string accepted = "accepted"; // 친구 요청 수락 상태
        public const string rejected = "rejected"; // 친구 요청 거절 상태
        public const string cancelled = "cancelled"; // 친구 요청 취소 상태
        public const string removed = "removed"; // 친구 삭제 상태

        public const string inbox = "inbox"; // 친구 요청 수신함
        public const string outbox = "outbox"; // 친구 요청 발신함

    }
}
