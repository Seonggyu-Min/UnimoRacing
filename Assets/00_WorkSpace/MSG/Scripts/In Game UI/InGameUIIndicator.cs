using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace MSG
{
    public class InGameUIIndicator : MonoBehaviourPunCallbacks
    {
        [SerializeField] private TMP_Text _lapText;
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _timtText;

        private readonly string fisrt = "1st";
        private readonly string second = "2nd";
        private readonly string third = "3rd";
        private readonly string fourth = "4th";

        private bool _isSetStartTime = false;
        private float _startTime = 0f;

        private bool _isSetEndTime = false;
        private float _endTime = 0f;

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // 여기서 플레이어 랭크 변동

            // 여기서 랩 수 변동 체크


            // 플레이어 완주 체크
            if (changedProps.ContainsKey(PhotonNetworkCustomProperties.KEY_PLAYER_RACE_IS_FINISHED))
            {
                if (changedProps.TryGetValue(PhotonNetworkCustomProperties.KEY_PLAYER_RACE_IS_FINISHED, out object finished))
                {
                    if (bool.TryParse(finished.ToString(), out bool isFinished))
                    {
                        if (isFinished)
                        {
                            _isSetEndTime = true;
                            if (changedProps.TryGetValue(PhotonNetworkCustomProperties.KEY_PLAYER_RACE_FINISHED_TIME, out object endTime))
                            {
                                float.TryParse(endTime.ToString(), out _endTime);
                            }
                        }
                    }
                }
            }
        }

        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (propertiesThatChanged.ContainsKey(PhotonNetworkCustomProperties.KEY_RACE_START_TIME))
            {
                if (propertiesThatChanged.TryGetValue(PhotonNetworkCustomProperties.KEY_RACE_START_TIME, out object time))
                {
                    if (float.TryParse(time.ToString(), out _startTime))
                    {
                        _isSetStartTime = true;
                    }
                }
            }
        }

        private void Update()
        {
            if (!_isSetStartTime) return; // 시작 시간이 세팅되지 않았으면 시간 표시 안함

            if (_isSetEndTime)  // 이미 경기가 종료되었으면 종료 시간 - 시작 시간 표기
            {
                _timtText.text = (_endTime - _startTime).ToString("F2");
                return;
            }

            // 경기 중이면 시간 표기 텍스트 업데이트
            _timtText.text = FormatTime(PhotonNetwork.Time - _startTime);
        }

        private static string FormatTime(double seconds)
        {
            var ts = System.TimeSpan.FromSeconds(Mathf.Max(0f, (float)seconds));
            int centi = ts.Milliseconds / 10;
            return string.Format("{0:00}:{1:00}.{2:00}", ts.Minutes, ts.Seconds, centi);
        }
    }
}
