using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;


namespace MSG
{
    // 상점 UI가 가져야 할 동작인데 일단 합치면 빌드할 때 이상해지니까 따로 뒀음
    public class ShopUIBinder : MonoBehaviour
    {
        [SerializeField] private RawImage _rawImage;

        // 이건 실제 UI에서 들고 있어서 실제로는 해당 값 넘겨주면 됨
        [SerializeField] private int _unimoId = -1;
        [SerializeField] private int _kartId = -1;

        public bool IsBound { get; private set; }


        private void OnEnable()
        {
            StartCoroutine(Wait());
        }

        private void OnDisable()
        {
            if (_unimoId >= 0)
                ItemPreviewManager.Instance.UnbindPreview(_unimoId, _rawImage);
            else if (_kartId >= 0)
                ItemPreviewManager.Instance.UnbindPreview(_kartId, _rawImage);
        }


        public void TryBind()
        {
            if (IsBound) return;

            if (_unimoId >= 0) ItemPreviewManager.Instance.BindUnimoPreview(_unimoId, _rawImage);
            else if (_kartId >= 0) ItemPreviewManager.Instance.BindKartPreview(_kartId, _rawImage);
            IsBound = true;
        }

        public void TryUnbind()
        {
            if (!IsBound) return;

            if (_unimoId >= 0) ItemPreviewManager.Instance.UnbindPreview(_unimoId, _rawImage);
            else if (_kartId >= 0) ItemPreviewManager.Instance.UnbindPreview(_kartId, _rawImage);
            IsBound = false;
        }


        // 실제로는 시작하자 마자 첫 프레임에 상점을 열 일이 없으니까 코루틴 필요 없을 듯
        private IEnumerator Wait()
        {
            yield return new WaitUntil(() => ItemPreviewManager.Instance != null && ItemPreviewManager.Instance.Ready);

            if (_unimoId >= 0) ItemPreviewManager.Instance.BindUnimoPreview(_unimoId, _rawImage);
            else if (_kartId >= 0) ItemPreviewManager.Instance.BindKartPreview(_kartId, _rawImage);
        }
    }
}