using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace YTW
{
    public class GamePreloader : MonoBehaviour
    {
        ResourceManager.PreloadTicket _ticket;
        string _charKey, _kartKey;

        // �κ������ ���Ӿ� ���� ������ ȣ��
        public async Task GoToGameWithPreloadAsync(UnimoCharacterSO charSO, UnimoKartSO kartSO, IEnumerable<string> labelsForGame)
        {
            // ���� ����(��/����/����Ʈ ��) �̸� �ٿ�ε�(��ũ ĳ�ñ���)
            _ticket = await ResourceManager.Instance.PreloadLabelsAsync(labelsForGame, toMemory: false);

            // ��ٷ� �� �ε�ƿ��� �޸𸮿� �÷��α�(���� �� hitch ����)
            //_charKey = charSO.model.RuntimeKey.ToString();
            //_kartKey = kartSO.model.RuntimeKey.ToString();
            await ResourceManager.Instance.LoadAsync<GameObject>(_charKey);
            await ResourceManager.Instance.LoadAsync<GameObject>(_kartKey);

            // �غ� ��. �� ��ȯ(��Ʈ��ũ �ε� �������)
            Manager.Scene.LoadGameScene(SceneType.Map1_TEST);
        }

        // ��� ����(Ȥ�� �κ� ����) �� ȣ��
        public void CleanupAfterMatch()
        {
            // �ε�ƿ� �޸� ����
            if (!string.IsNullOrEmpty(_charKey)) ResourceManager.Instance.Release(_charKey);
            if (!string.IsNullOrEmpty(_kartKey)) ResourceManager.Instance.Release(_kartKey);
            _charKey = _kartKey = null;

            // �����ε� ����(��ũ ĳ�ô� ����)
            ResourceManager.Instance.ReleasePreload(_ticket);
            _ticket = null;
        }
    }
}
