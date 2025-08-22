using Photon.Pun;
using Photon.Realtime;
using System;
using System.Reflection;
using System.Text;
using YSJ.Util;

public enum RoomPropertyKey
{
    MinPlayers,
    RaceChoosableMapCount,
    RaceMapId,
}

public class RoomManager : SimpleSingleton<RoomManager>
{
    private const string CUSTOMPROPERTIES_KEY_ROOM_MIN_PLAYERS = "room_minPlayers";
    private const string CUSTOMPROPERTIES_KEY_ROOM_RACE_CHOOSABLE_MAP_COUNT = "room_raceChoosableMapCount";
    private const string CUSTOMPROPERTIES_KEY_RACE_MAP_ID = "room_raceMapId";

    private const int CUSTOMPROPERTIES_VALUE_DEFAULT_RACE_MAP_ID = 0;
    private const int CUSTOMPROPERTIES_VALUE_NOT_CHOOSE_RACE_MAP_ID = -1;

    private RaceGameConfig raceGameConfig;

    protected override void Init()
    {
        base.Init();
        raceGameConfig = RaceGameConfig.Load();
    }

    public void OnMatchAction()
    {
        if (!PhotonNetwork.InLobby) return;

        var opts = new RoomOptions
        {
            MaxPlayers = raceGameConfig.RaceMaxPlayer,                                                                      // 방 최대 인원 수
            IsVisible = true,                                                                                               // 방이 로비에 등록되는지 여부
            IsOpen = true,                                                                                                  // 방에 참여할 수 있는지 여부
            CustomRoomProperties = new ExitGames.Client.Photon.Hashtable
            {
                { ToKey(RoomPropertyKey.MinPlayers),                raceGameConfig.RaceMinPlayer },                     // 방에 최소 플레이 가능 수
                { ToKey(RoomPropertyKey.RaceChoosableMapCount),     raceGameConfig.RaceChoosableMapCount },             // 게임이 시작될 때 투표 표시될 수 있는 맵 수
                { ToKey(RoomPropertyKey.RaceMapId),                 CUSTOMPROPERTIES_VALUE_NOT_CHOOSE_RACE_MAP_ID },    // 레이싱이 시작될 맵 ID
            },
        };

        PhotonNetwork.JoinRandomOrCreateRoom(
            expectedCustomRoomProperties: null,
            expectedMaxPlayers: 0,
            matchingType: MatchmakingMode.FillRoom,
            typedLobby: TypedLobby.Default,
            sqlLobbyFilter: null,
            roomName: PhotonNetwork.NickName + "_Room",
            roomOptions: opts
        );
    }

    // Print
    public string PrintCustomProperties(string methodName = "\n")
    {
        var values = Enum.GetValues(typeof(RoomPropertyKey));

        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(methodName);
        stringBuilder.Append("\n");

        foreach (var e in values)
        {
            try
            {
                stringBuilder.Append($"{e} : {PhotonNetwork.CurrentRoom.CustomProperties[ToKey((RoomPropertyKey)e)]}\n");
            }
            catch { this.PrintLog($"Type Print: {e}, Value Error"); }
        }
        string result = stringBuilder.ToString();
        // this.PrintLog(result);;
        return result;
    }
    public string PrintCurrentPlayers()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("Player List\n");
        foreach (var e in PhotonNetwork.PlayerList)
        {
            try
            {
                stringBuilder.Append($"Player Name: {e.NickName}\n");
            }
            catch { this.PrintLog($"Type Print: {e}, Value Error"); }
        }

        string result = stringBuilder.ToString();
        // this.PrintLog(result);
        return result;
    }
    private string ToKey(RoomPropertyKey key)
        => key switch
        {
            RoomPropertyKey.MinPlayers => CUSTOMPROPERTIES_KEY_ROOM_MIN_PLAYERS,
            RoomPropertyKey.RaceChoosableMapCount => CUSTOMPROPERTIES_KEY_ROOM_RACE_CHOOSABLE_MAP_COUNT,
            RoomPropertyKey.RaceMapId => CUSTOMPROPERTIES_KEY_RACE_MAP_ID,
            _ => key.ToString()
        };

}