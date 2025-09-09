using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace YTW
{
    public class GamePreloader : MonoBehaviour
    {
        ResourceManager.PreloadTicket _ticket;
        string _charKey, _kartKey;

        // 로비씬에서 게임씬 진입 직전에 호출
        public async Task GoToGameWithPreloadAsync(UnimoCharacterSO charSO, UnimoKartSO kartSO, IEnumerable<string> labelsForGame)
        {
            // 공통 번들(맵/사운드/이펙트 등) 미리 다운로드(디스크 캐시까지)
            _ticket = await ResourceManager.Instance.PreloadLabelsAsync(labelsForGame, toMemory: false);

            // 곧바로 쓸 로드아웃만 메모리에 올려두기(스폰 시 hitch 방지)
            //_charKey = charSO.model.RuntimeKey.ToString();
            //_kartKey = kartSO.model.RuntimeKey.ToString();
            await ResourceManager.Instance.LoadAsync<GameObject>(_charKey);
            await ResourceManager.Instance.LoadAsync<GameObject>(_kartKey);

            // 준비 끝. 씬 전환(네트워크 로딩 로직대로)
            Manager.Scene.LoadGameScene(SceneType.Map1_TEST);
        }

        // 경기 종료(혹은 로비 복귀) 시 호출
        public void CleanupAfterMatch()
        {
            // 로드아웃 메모리 해제
            if (!string.IsNullOrEmpty(_charKey)) ResourceManager.Instance.Release(_charKey);
            if (!string.IsNullOrEmpty(_kartKey)) ResourceManager.Instance.Release(_kartKey);
            _charKey = _kartKey = null;

            // 프리로드 해제(디스크 캐시는 유지)
            ResourceManager.Instance.ReleasePreload(_ticket);
            _ticket = null;
        }
    }
}
