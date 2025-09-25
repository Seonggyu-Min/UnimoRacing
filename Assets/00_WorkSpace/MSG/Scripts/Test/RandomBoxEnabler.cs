using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    // 테스트용 코드이며 실제 배포 시 사용하면 안될 것 같은 구조입니다.
    public class RandomBoxEnabler : MonoBehaviour
    {
        [SerializeField] private GameObject _map1Obj;
        [SerializeField] private GameObject _map2Obj;
        [SerializeField] private GameObject _map3Obj;

        private void Start()
        {
            int index = PhotonNetworkCustomProperties.GetRoomProp<int>(RoomKey.WinnerMapIndex);

            if (index == 1)
            {
                _map1Obj?.SetActive(true);
                _map2Obj?.SetActive(false);
                _map3Obj?.SetActive(false);
            }
            else if (index == 2)
            {
                _map1Obj?.SetActive(false);
                _map2Obj?.SetActive(true);
                _map3Obj?.SetActive(false);
            }
            else if (index == 3)
            {
                _map1Obj?.SetActive(false);
                _map2Obj?.SetActive(false);
                _map3Obj?.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[RandomBoxEnabler] 투표된 맵 인덱스: {index}의 값이 비정상적입니다.");
            }
        }
    }
}
