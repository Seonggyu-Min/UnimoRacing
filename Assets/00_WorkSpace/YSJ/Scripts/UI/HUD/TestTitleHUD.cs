using Photon.Pun;
using Runtime.UI;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YSJ.Util;

public sealed class TestTitleHUD : BaseUI
{
    private enum TestLobbyUI
    {
        RoomName_TMP,

        RaceCharacterId_InputField,
        RaceCharacterId_TMP,

        RaceCarId_InputField,
        RaceCarId_TMP,

        HopeRaceMapId_InputField,
        HopeRaceMapId_TMP,

        Match_Button,
        Match_TMP,
    }

    private enum MatchType
    {
        None = 0,

        Matching,

        Matched,
    }

    private UIBinder<TestLobbyUI> _uiBinder;
    private MatchType _matchType;

    protected override void InitBaseUI()
    {
        base.InitBaseUI();
        _uiBinder = new UIBinder<TestLobbyUI>(this);

        var matchButton = _uiBinder.Get<Button>(TestLobbyUI.Match_Button);
        var matchButtonEvent = _uiBinder.GetEvent(TestLobbyUI.Match_Button);

        _matchType = MatchType.None;
        matchButtonEvent.Click += OnMathButtonEvent;

        var raceCharacterId_InputField = _uiBinder.Get<TMP_InputField>(TestLobbyUI.RaceCharacterId_InputField);
        var raceCarId_InputField = _uiBinder.Get<TMP_InputField>(TestLobbyUI.RaceCarId_InputField);
        var hopeRaceMapId_InputField = _uiBinder.Get<TMP_InputField>(TestLobbyUI.HopeRaceMapId_InputField);

        raceCharacterId_InputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        raceCarId_InputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        hopeRaceMapId_InputField.contentType = TMP_InputField.ContentType.IntegerNumber;

        raceCharacterId_InputField.richText = false;
        raceCarId_InputField.richText = false;
        hopeRaceMapId_InputField.richText = false;

        PhotonNetworkManager.Instance.OnActionLeftRoom -= CleanupUI;
        PhotonNetworkManager.Instance.OnActionLeftRoom += CleanupUI;
    }

    private void OnDestroy()
    {
        PhotonNetworkManager.Instance.OnActionLeftRoom -= CleanupUI;
    }

    public void OnMathButtonEvent(PointerEventData eventData)
    {
        if (_matchType != MatchType.None) return;
        StartCoroutine(CO_DelayMatch());
    }

    public IEnumerator CO_DelayMatch()
    {
        var matchRoomName = _uiBinder.Get<TextMeshProUGUI>(TestLobbyUI.RoomName_TMP);
        var matchTmp = _uiBinder.Get<TextMeshProUGUI>(TestLobbyUI.Match_TMP);

        if (PhotonNetwork.InRoom)
            matchRoomName.text = "Not Find Room";

        // 매칭 시작
        _matchType = MatchType.Matching;
        matchTmp.text = _matchType.ToString();
        this.PrintLog($"{_matchType.ToString()}");
        yield return new WaitForSeconds(2.0f);

        var raceCharacterIdString = _uiBinder.Get<TMP_InputField>(TestLobbyUI.RaceCharacterId_InputField).text;
        var raceCarIdString = _uiBinder.Get<TMP_InputField>(TestLobbyUI.RaceCarId_InputField).text;
        var hopeRaceMapIdString = _uiBinder.Get<TMP_InputField>(TestLobbyUI.HopeRaceMapId_InputField).text;

        // 실제 데이터 문자열
        this.PrintLog(
            $"raceCharacterIdString len={raceCharacterIdString.Length} codes={DumpCodes(raceCharacterIdString)}\n" +
            $"raceCarIdString len={raceCarIdString.Length} codes={DumpCodes(raceCarIdString)}\n" +
            $"hopeRaceMapIdString len={hopeRaceMapIdString.Length} codes={DumpCodes(hopeRaceMapIdString)}\n");

        // 문자열에 안보이는 유니코드 등 제거
        CleanInt(raceCharacterIdString);
        CleanInt(raceCarIdString);
        CleanInt(hopeRaceMapIdString);

        // 파싱
        var isRaceCharactorId   = int.TryParse(raceCharacterIdString, out int raceCharacterIdInt);
        var isRaceCarId         = int.TryParse(raceCarIdString,       out int raceCarIdInt);
        var isHopeRaceMapId     = int.TryParse(hopeRaceMapIdString,   out int hopeRaceMapIdInt);

        // 정상 처리 확인
        var isMatchedStartable = isRaceCharactorId && isRaceCarId && isHopeRaceMapId;

        // 결과 로그
        this.PrintLog($"isRaceCharactorId > {raceCharacterIdString} / isRaceCarId > {raceCarIdString} / isHopeRaceMapId > {hopeRaceMapIdString}");
        this.PrintLog($"isRaceCharactorId > {isRaceCharactorId} / isRaceCarId > {isRaceCarId} / isHopeRaceMapId > {isHopeRaceMapId} => isMatchedStartable > {isMatchedStartable}");

        // 플레이어 데이터 수정
        PlayerManager.Instance.SetPlayerCPCharacterId(raceCharacterIdInt);
        PlayerManager.Instance.SetPlayerCPKartId(raceCarIdInt);
        PlayerManager.Instance.SetPlayerCPHopeRaceMapId(hopeRaceMapIdInt);

        // 매칭 준비 완료.
        _matchType = MatchType.Matched;
        matchTmp.text = _matchType.ToString();
        this.PrintLog($"{_matchType.ToString()}");
        yield return new WaitForSeconds(2.0f);

        // 매치 이동 후 처리
        if (!isMatchedStartable)
        {
            // 팝업
            matchTmp.text = "Fail Match";
            this.PrintLog(matchTmp.text);

            yield return new WaitForSeconds(3.0f);

            CleanupUI();
        }
        else
        {
            matchTmp.text = "Find Player...";
            // RoomManager.Instance.MatchAction();
            yield return new WaitForSeconds(3.0f);
            StartCoroutine(RoomNameUpdate());
        }
    }

    public IEnumerator RoomNameUpdate()
    {
        var matchRoomName = _uiBinder.Get<TextMeshProUGUI>(TestLobbyUI.RoomName_TMP);
        while (!PhotonNetwork.InRoom)
        {
            yield return null;
        }

        matchRoomName.text = PhotonNetwork.CurrentRoom.Name;
        yield break;
    }

    string DumpCodes(string s) => string.Join(" ", s.Select(ch => ((int)ch).ToString("X4")));

    string CleanInt(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = s.Trim();
        // 유니코드 제로위드/제어문자 제거
        s = Regex.Replace(s, @"[\u200B-\u200D\uFEFF\u2060]", "");
        s = Regex.Replace(s, @"\p{Cf}|\p{Cs}", "");  // 포맷/서러게이트 등
        return s;
    }

    private void CleanupUI()
    {
        var roomNameTmp = _uiBinder.Get<TextMeshProUGUI>(TestLobbyUI.RoomName_TMP);
        var matchTmp = _uiBinder.Get<TextMeshProUGUI>(TestLobbyUI.Match_TMP);

        roomNameTmp.text = "Not Finded Room";
        matchTmp.text = "Match";
        _matchType = MatchType.None;
    }
}
