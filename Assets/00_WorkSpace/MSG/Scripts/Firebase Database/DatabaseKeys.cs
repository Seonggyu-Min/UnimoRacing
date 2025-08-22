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


        // -- Presence Keys --
        public const string presence = "presence"; // 전체 Presence 데이터 최상위 노드
        public const string online = "online"; // 온라인 상태
        public const string inRoom = "inRoom"; // 방에 있는 상태
        public const string inGame = "inGame"; // 게임 중인 상태
        public const string inParty = "inParty"; // 파티에 있는 상태
        public const string lastSeen = "lastSeen"; // 마지막 활동 시간
        public const string partyId = "partyId"; // 파티 ID
        public const string roomName = "roomName"; // 방 이름
        public const string heartbeat = "heartbeat"; // 예외 처리를 위한 하트비트


        // -- Party Keys --
        public const string parties = "parties"; // 파티 목록
        // public const string partyId = "partyId"; // 파티 ID, 위와 동일
        // public const string status = "status"; // 파티 상태, 위와 동일
        public const string idle = "idle";
        public const string matching = "matching"; // 파티 매칭 중
        // public const string inGame = "inGame"; // 파티 게임 중, 위와 동일
        public const string members = "members"; // 파티 멤버 목록
        public const string leaderUid = "leaderUid"; // 파티 리더의 UID
        public const string targetRoom = "targetRoom"; // 파티 들어가게 될 방 이름


        // -- PartyMembership Keys --
        public const string partyMemberships = "partyMemberships"; // 파티 멤버십 목록
        // public const string partyId = "partyId"; // 파티 ID, 위와 동일


        // -- Invitation Keys --
        public const string invitations = "invitations"; // 초대 목록
        // public const string partyId = "partyId"; // 파티 ID, 위와 동일
        // public const string from = "from"; // 초대를 보낸 사람의 ID, 위와 동일
        // public const string to = "to"; // 초대를 받은 사람의 ID, 위와 동일
        // public const string status = "status"; // 초대장의 상태, 위와 동일
        // public const string pending = "pending"; // 초대 대기 상태, 위와 동일
        // public const string accepted = "accepted"; // 초대 수락 상태, 위와 동일
        // public const string rejected = "rejected"; // 초대 거절 상태, 위와 동일
        public const string createdAt = "createdAt"; // 초대가 생성된 시간


        // -- userInvitations Keys --
        public const string userInvitations = "userInvitations"; // 유저별 초대장 목록
    }
}
