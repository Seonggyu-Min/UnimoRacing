using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace MSG
{
    /// <summary>
    /// 유니모와 카트 SO를 모두 참조하여 Database처럼 사용할 컴포넌트입니다.
    /// </summary>
    public class UnimoKartDatabase : Singleton<UnimoKartDatabase>
    {
        [SerializeField] private List<UnimoCharacterSO> _unimoCharacterSOs = new(); // 유니모 SO들
        [SerializeField] private List<UnimoKartSO> _unimoKartSOs = new(); // 카트 SO들

        private Dictionary<int, UnimoCharacterSO> _unimoIndexDict = new();
        private Dictionary<int, UnimoKartSO> _kartIndexDict = new();


        private void Awake()
        {
            SingletonInit();

            foreach (var unimo in _unimoCharacterSOs)
            {
                if (!_unimoIndexDict.ContainsKey(unimo.characterId))
                {
                    _unimoIndexDict.Add(unimo.characterId, unimo);
                }
            }

            foreach (var kart in _unimoKartSOs)
            {
                if (!_kartIndexDict.ContainsKey(kart.carId))
                {
                    _kartIndexDict.Add(kart.carId, kart);
                }
            }
        }


        public bool TryGetByUnimoIndex(int index, out UnimoCharacterSO unimo)
        {
            unimo = null;
            return _unimoIndexDict != null && _unimoIndexDict.TryGetValue(index, out unimo);
        }

        public bool TryGetByKartIndex(int index, out UnimoKartSO kart)
        {
            kart = null;
            return _kartIndexDict != null && _kartIndexDict.TryGetValue(index, out kart);
        }
    }
}
