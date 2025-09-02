using System;
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
        // ----- User Data -----
        public static string Users(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid);
        public static string Nickname(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.nickname);


        // ----- Stats Settings -----
        public static string Stats(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.stats);
        public static string Wins(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.stats, DatabaseKeys.wins);
        public static string Losses(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.stats, DatabaseKeys.losses);


        // ----- Equipped Data -----
        public static string Equipped(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.equipped); // 현재 장착한 아이템들
        public static string EquippedKart(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.equipped, DatabaseKeys.karts); // 현재 장착한 카트
        public static string EquippedUnimo(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.equipped, DatabaseKeys.unimos); // 현재 장착한 유니모


        // ----- Inventory Data -----
        public static string Inventory(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.inventory); // 아이템 인벤토리
        public static string KartsInventory(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.inventory, DatabaseKeys.karts); // 카트 인벤토리, value는 kartId
        public static string UnimosInventory(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.inventory, DatabaseKeys.unimos); // 유니모 인벤토리, value는 unimoId
        public static string KartInventory(string uid, int kartId) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.inventory, DatabaseKeys.karts, kartId.ToString()); // 특정 카트 아이템, value는 int (강화 레벨, 0이면 미보유)
        public static string UnimoInventory(string uid, int unimoId) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.inventory, DatabaseKeys.unimos, unimoId.ToString()); // 특정 유니모 아이템, value는 int (강화 레벨, 0이면 미보유)


        // ----- Nickname Data -----
        public static string Nicknames(string nickname) => DBPathMaker.Join(DatabaseKeys.nicknames, nickname); // 닉네임 중복 확인용 역인덱스, value는 uid


        // ----- Friend Data -----
        public static string FriendListRoot(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.list);
        public static string Friend(string myUid, string friendUid) => DBPathMaker.Join(DatabaseKeys.users, myUid, DatabaseKeys.list, friendUid); // 친구 데이터


        // ----- Game Data -----
        public static string GameData(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.gameData);
        public static string Money1(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.gameData, DatabaseKeys.money1);
        public static string Money2(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.gameData, DatabaseKeys.money2);
        public static string Money3(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.gameData, DatabaseKeys.money3);
        //public static string Level(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.gameData, DatabaseKeys.level);
        public static string Experience(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.gameData, DatabaseKeys.experience);


        // ----- Daily Missions -----
        public static string DailyMissions(string uid) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.dailyMissions); // 전체 일일 미션, value는 missionId
        public static string DailyMission(string uid, int missionId) => DBPathMaker.Join(DatabaseKeys.users, uid, DatabaseKeys.dailyMissions, missionId.ToString()); // 특정 일일 미션, value는 bool (완료 여부)


        // ----- Friend Links -----
        public static string FriendLinks(string pairId) => DBPathMaker.Join(DatabaseKeys.friendLinks, pairId);
        public static string OutBoxRoot(string uid) => DBPathMaker.Join(DatabaseKeys.outbox, uid);
        public static string OutBox(string uid, string pairId) => DBPathMaker.Join(DatabaseKeys.outbox, uid, pairId);
        public static string InBoxRoot(string uid) => DBPathMaker.Join(DatabaseKeys.inbox, uid);
        public static string InBox(string uid, string pairId) => DBPathMaker.Join(DatabaseKeys.inbox, uid, pairId);


        // ----- Catalog -----
        public static string CatalogRoot() => DBPathMaker.Join(DatabaseKeys.catalog);
        public static string CatalogVersion() => DBPathMaker.Join(DatabaseKeys.catalog, DatabaseKeys.version);
        public static string CatalogGlobals() => DBPathMaker.Join(DatabaseKeys.catalog, DatabaseKeys.globals);
        public static string CatalogKarts() => DBPathMaker.Join(DatabaseKeys.catalog, DatabaseKeys.karts);
        public static string CatalogKart(int kartId) => DBPathMaker.Join(DatabaseKeys.catalog, DatabaseKeys.karts, kartId.ToString());


        // ----- Presence Data(WIP) -----
        public static string Presence(string uid) => DBPathMaker.Join(DatabaseKeys.presence, uid);
        public static string OnlineStatus(string uid) => DBPathMaker.Join(DatabaseKeys.presence, uid, DatabaseKeys.online);
        public static string InRoomStatus(string uid) => DBPathMaker.Join(DatabaseKeys.presence, uid, DatabaseKeys.inRoom);
        public static string InGameStatus(string uid) => DBPathMaker.Join(DatabaseKeys.presence, uid, DatabaseKeys.inGame);
        public static string InPartyStatus(string uid) => DBPathMaker.Join(DatabaseKeys.presence, uid, DatabaseKeys.inParty); // 파티에 있는지 여부
        public static string PartyIdForPresence(string uid) => DBPathMaker.Join(DatabaseKeys.presence, uid, DatabaseKeys.partyId); // 파티 ID
        public static string LastSeen(string uid) => DBPathMaker.Join(DatabaseKeys.presence, uid, DatabaseKeys.lastSeen); // 마지막 업데이트 타임스탬프
        public static string RoomName(string uid) => DBPathMaker.Join(DatabaseKeys.presence, uid, DatabaseKeys.roomName); // 현재 방 이름
        public static string Heartbeat(string uid) => DBPathMaker.Join(DatabaseKeys.presence, uid, DatabaseKeys.heartbeat); // Heartbeat 타임스탬프


        #region Deprecated

        // ------ Party Data(Deprecated) -----
        public static string PartyRoot(string partyId) => DBPathMaker.Join(DatabaseKeys.parties, partyId); // 파티 데이터
        public static string PartyMembers(string partyId) => DBPathMaker.Join(DatabaseKeys.parties, partyId, DatabaseKeys.members); // 파티 멤버 목록
        public static string PartyLeader(string partyId) => DBPathMaker.Join(DatabaseKeys.parties, partyId, DatabaseKeys.leaderUid); // 파티장 UID
        public static string PartyMember(string partyId, string uid) => DBPathMaker.Join(DatabaseKeys.parties, partyId, DatabaseKeys.members, uid); // 특정 파티 멤버 데이터
        public static string PartyStatus(string partyId) => DBPathMaker.Join(DatabaseKeys.parties, partyId, DatabaseKeys.status); // 파티 상태
        //public static string PartyTargetRoom(string partyId) => DBPathMaker.Join(DatabaseKeys.parties, partyId, DatabaseKeys.targetRoom); // 파티가 들어갈 방 이름


        //// ----- Party Membership Data(Deprecated) -----
        public static string PartyMembershipsRoot(string uid) => DBPathMaker.Join(DatabaseKeys.partyMemberships, uid); // 유저의 파티 멤버십 목록
        public static string PartyMembership(string uid) => DBPathMaker.Join(DatabaseKeys.partyMemberships, uid, DatabaseKeys.partyId); // 특정 파티 멤버십 데이터


        //// ----- Invitation Data(Deprecated) -----
        //public static string InvitationsRoot(string inviteId) => DBPathMaker.Join(DatabaseKeys.invitations, inviteId); // 초대 데이터
        //public static string InvitationPartyId(string inviteId) => DBPathMaker.Join(DatabaseKeys.invitations, inviteId, DatabaseKeys.partyId); // 초대된 파티 ID
        //public static string InvitationFrom(string inviteId) => DBPathMaker.Join(DatabaseKeys.invitations, inviteId, DatabaseKeys.from); // 초대 보낸 사람의 UID
        //public static string InvitationTo(string inviteId) => DBPathMaker.Join(DatabaseKeys.invitations, inviteId, DatabaseKeys.to); // 초대 받은 사람의 UID
        //public static string InvitationStatus(string inviteId) => DBPathMaker.Join(DatabaseKeys.invitations, inviteId, DatabaseKeys.status); // 초대 상태 (예: pending, accepted, rejected)
        //public static string InvitationCreatedAt(string inviteId) => DBPathMaker.Join(DatabaseKeys.invitations, inviteId, DatabaseKeys.createdAt); // 초대 생성 시간


        //// ----- UserInvitations Data(Deprecated) -----
        //public static string UserInvitations(string uid) => DBPathMaker.Join(DatabaseKeys.userInvitations, uid); // 유저의 초대 목록
        //public static string UserInvitation(string uid, string inviteId) => DBPathMaker.Join(DatabaseKeys.userInvitations, uid, inviteId); // 특정 초대 데이터

        #endregion
    }
}