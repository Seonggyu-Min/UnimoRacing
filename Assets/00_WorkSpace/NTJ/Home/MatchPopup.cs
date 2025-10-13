using MSG;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MatchPopup : MonoBehaviourPunCallbacks
{
    [SerializeField] private MatchFlowManager _matchFlowManager;

    [Header("매칭 관련 버튼")]
    [SerializeField] private GameObject playButtonGroup;     // Play! 버튼 그룹
    [SerializeField] private GameObject matchingButtonGroup; // 매칭중... 버튼 그룹

    [SerializeField] private Button startMatchButton;   // Play! 버튼
    [SerializeField] private Button cancelMatchButton;  // 매칭중일 때 취소 버튼

    //[Header("안내 텍스트")]
    //[SerializeField] private TMP_Text _infoText;
    //[SerializeField] private float _textShowSec;
    //private Coroutine _textCO;

    //[SerializeField] private TMP_Text _matchInfoText;   // 경과,인원 표시용
    //[SerializeField] private TMP_Text _adviceText;      // 권고 문구
    //[SerializeField] private float _warnAfterSec = 20f; // 해당 시간 이후 경고문구 켜기
    //private float _matchStartRt = -1f;


    public override void OnEnable()
    {
        base.OnEnable();

        startMatchButton.onClick.AddListener(OnStartMatch);
        cancelMatchButton.onClick.AddListener(OnCancelMatch);

        SetMatchingUI(false);
        SetInteractableUI();
        //SetStatusTextsActive(false);
    }

    public override void OnDisable()
    {
        base.OnDisable();

        startMatchButton.onClick.RemoveListener(OnStartMatch);
        cancelMatchButton.onClick.RemoveListener(OnCancelMatch);
    }

    private void Update()
    {
        if (matchingButtonGroup.activeSelf)
        {
            //UpdateMatchInfoTexts();
        }
    }

    private void OnStartMatch()
    {
        if (PartyService.Instance.IsInParty && !PartyService.Instance.IsLeader)
        {
            Debug.Log("파티의 시작은 파티장만 할 수 있습니다.");
            //StartTextCO("파티의 시작은 파티장만 할 수 있습니다.");
            return;
        }

        startMatchButton.interactable = false;
        SetMatchingUI(true);
        //_matchStartRt = Time.realtimeSinceStartup;
        //SetStatusTextsActive(true);

        _matchFlowManager.OnClickQuickMatch();
        SetInteractableUI();
    }

    private void OnCancelMatch()
    {
        cancelMatchButton.interactable = false;
        SetMatchingUI(false);
        //_matchStartRt = -1f;
        //SetStatusTextsActive(false);

        _matchFlowManager.OnClickCancelMatch();
        SetInteractableUI();
    }

    //private void RefreshUI(bool isMatching)
    //{
    //    playButtonGroup.SetActive(!isMatching);
    //    matchingButtonGroup.SetActive(isMatching);
    //}

    #region UI Logic

    public override void OnConnectedToMaster() => SetInteractableUI();
    public override void OnJoinedRoom() => SetInteractableUI();
    public override void OnLeftRoom() => SetInteractableUI();

    private void SetMatchingUI(bool isMatching)
    {
        playButtonGroup.SetActive(!isMatching);
        matchingButtonGroup.SetActive(isMatching);
    }

    private void SetInteractableUI()
    {
        bool isReady = PhotonNetwork.IsConnectedAndReady;
        bool inRoom = PhotonNetwork.InRoom;
        bool roomOkay = inRoom && IsAllowedRoom();

        // 현재 표시 중인 버튼 확인
        bool showingPlay = playButtonGroup.activeSelf;
        bool showingMatching = matchingButtonGroup.activeSelf;

        // 버튼 가능 조건
        bool canStart = isReady && roomOkay && showingPlay;
        bool canCancel = isReady && roomOkay && showingMatching;

        startMatchButton.interactable = canStart;
        cancelMatchButton.interactable = canCancel;

        //if (!canStart && !canCancel)
        //{
        //    //ShowInfoText("매칭 작업 중입니다. 잠시만 기다려주세요." +
        //    //    "인원이 적을 경우 동시에 매칭 버튼을 눌렀을 때" +
        //    //    "매칭이 안될 수 있으니 오랫동안 매칭되지 않으면" +
        //    //    "다시 시도해주세요");
        //    ShowInfoText("");
        //}
        //else
        //{
        //    HideInfoText();
        //}
    }

    private bool IsAllowedRoom()
    {
        var room = PhotonNetwork.CurrentRoom;
        if (room == null) return false;

        // 커스텀 프로퍼티로 방 타입 확인
        if (room.CustomProperties != null &&
            room.CustomProperties.TryGetValue(RoomMakeHelper.ROOM_TYPE, out object typeObj))
        {
            var type = (RoomType)typeObj;
            return type == RoomType.Match || type == RoomType.Party || type == RoomType.Home;
        }

        // 혹시 모르니까 방 타입 이름으로도 검증
        string name = room.Name ?? string.Empty;
        return name.StartsWith("_m") || name.StartsWith("_p") || name.StartsWith("_h");
    }

    //private void SetStatusTextsActive(bool active)
    //{
    //    if (_matchInfoText != null)
    //    {
    //        _matchInfoText.gameObject.SetActive(active);
    //    }
    //    if (_adviceText != null)
    //    {
    //        _adviceText.gameObject.SetActive(false);
    //    }
    //}

    //private void UpdateMatchInfoTexts()
    //{
    //    // 경과 시간 계산
    //    float elapsed = (_matchStartRt > 0f) ? (Time.realtimeSinceStartup - _matchStartRt) : 0f;

    //    // 현재 인원/최대 인원
    //    int cur = (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null) ? PhotonNetwork.CurrentRoom.PlayerCount : 0;
    //    int max = (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null) ? PhotonNetwork.CurrentRoom.MaxPlayers : 0;

    //    // 텍스트 표기
    //    if (_matchInfoText != null)
    //    {
    //        _matchInfoText.text = $"경과 {FormatMMSS(elapsed)}, 인원 {cur}/{max}";
    //    }

    //    // 일정 시간 이후 권고 문구 표기
    //    if (_adviceText != null)
    //    {
    //        _adviceText.gameObject.SetActive(elapsed >= _warnAfterSec);
    //    }
    //}

    private static string FormatMMSS(float sec)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt(sec));
        int mm = s / 60;
        int ss = s % 60;
        return $"{mm:00}:{ss:00}";
    }

    #endregion


    //#region Coroutine

    //private void StartTextCO(string text)
    //{
    //    if (_textCO != null)
    //    {
    //        StopCoroutine(_textCO);
    //        _textCO = null;
    //    }
    //    _textCO = StartCoroutine(InfoTextRoutine(text));
    //}

    //private void StopTextCO()
    //{
    //    if (_textCO != null)
    //    {
    //        StopCoroutine(_textCO);
    //        _textCO = null;
    //    }
    //}

    //private void ShowInfoText(string text)
    //{
    //    StopTextCO();
    //    _infoText.gameObject.SetActive(true);
    //    _infoText.text = text;
    //}

    //private void HideInfoText()
    //{
    //    StopTextCO();
    //    _infoText.gameObject.SetActive(false);
    //}

    //private IEnumerator InfoTextRoutine(string text)
    //{
    //    _infoText.gameObject.SetActive(true);
    //    _infoText.text = text;

    //    yield return new WaitForSeconds(_textShowSec);

    //    _infoText.gameObject.SetActive(false);
    //}

    //#endregion
}
