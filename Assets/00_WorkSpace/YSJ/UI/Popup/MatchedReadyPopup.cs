using Photon.Pun;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using YSJ.Util;

// 해당 클래스는 일단 룸에 있고 풀방일 때 나오는 UI 입니다.
public class MatchedReadyPopup : PopupBaseUI
{
    [SerializeField] private Image _fillGaugeImage;
    [SerializeField] private TextMeshProUGUI _readyPlayerCountTMP;
    [SerializeField] private Button _raceReadyButton;
    [SerializeField] private float _readyMaxTiem = 10.0f;

    private double _currentReadiableTime = 0.0f;
    private double _currentReadiableMaxTime = 0.0f;
    private Coroutine _readyCO;

    public override void Open()
    {
        base.Open();
        _currentReadiableTime = RoomManager.Instance.RoomMatchReadyStartTime;
        _currentReadiableMaxTime = _currentReadiableTime + _readyMaxTiem;
        ChangeFillGauge(_currentReadiableTime);
        _readyCO = StartCoroutine(ReadyGaugeCO());

        if (_raceReadyButton != null)
        {
            _raceReadyButton.onClick.AddListener(RaceMacthReady);
            _raceReadyButton.interactable = true; // 버튼 비활성화
        }

        RacePlayerCount();
        RoomManager.Instance.OnActionRoomPPTUpdate -= RacePlayerCount;
        RoomManager.Instance.OnActionRoomPPTUpdate += RacePlayerCount;
    }

    public override void Close()
    {
        base.Close();

        if (_readyCO != null)
        {
            StopCoroutine(_readyCO);
            Destroy(this.gameObject);
        }
    }

    private IEnumerator ReadyGaugeCO()
    {
        yield return new WaitForSeconds(ShowTime);

        bool normalOperation = false;
        while (_currentReadiableTime < _readyMaxTiem)
        {
            yield return null;

            _currentReadiableTime += Time.deltaTime;
            normalOperation = ChangeFillGauge(_currentReadiableTime);
        }

        if (normalOperation)
            yield return new WaitForSeconds(ShowTime);

        PhotonNetwork.LeaveRoom();
        Close();
    }

    private bool ChangeFillGauge(double time = 0.0f)
    {
        if (_fillGaugeImage == null)
        {
            this.PrintLog("게이지 이미지 컴포넌트가 존재하지 않습니다. ( => _fillGaugeImage)");
            return false;
        }

        _currentReadiableTime = time;
        _fillGaugeImage.fillAmount = (float)((_readyMaxTiem - (_currentReadiableMaxTime - _currentReadiableTime)) / _readyMaxTiem);
        return true;
    }

    private void RaceMacthReady()
    {
        if (_raceReadyButton != null)
        {
            _raceReadyButton.interactable = false; // 버튼 비활성화
        }
        PlayerManager.Instance.SetRaceReadySelection(true);
    }

    private void RacePlayerCount()
    {
        if (_readyPlayerCountTMP != null)
        {
            int currentPlayer = RoomManager.Instance.PlayersReadyCount;
            int currentMaxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
            _readyPlayerCountTMP.text = $"{currentPlayer} / {currentMaxPlayers}";
        }
    }
}
