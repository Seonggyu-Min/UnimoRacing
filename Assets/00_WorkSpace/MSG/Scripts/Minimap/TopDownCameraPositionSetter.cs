using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// 맵 스폰 위치가 일정하지 않아서 임의로 맞춰주기 위해 사용하는 컴포넌트입니다.
    /// </summary>
    public class TopDownCameraPositionSetter : MonoBehaviour
    {
        [SerializeField] private Vector3 _map1Pos;
        [SerializeField] private Vector3 _map2Pos;
        [SerializeField] private Vector3 _map3Pos;

        private void Start()
        {
            int index = PhotonNetworkCustomProperties.GetRoomProp<int>(RoomKey.WinnerMapIndex);
            Debug.Log($"index: {index}");

            switch (index)
            {
                case 1:
                    transform.position = _map1Pos;
                    break;

                case 2:
                    transform.position = _map2Pos;
                    break;

                case 3:
                    transform.position = _map3Pos;
                    break;

                default:
                    transform.position = _map1Pos;
                    break;
            }
        }
    }
}
