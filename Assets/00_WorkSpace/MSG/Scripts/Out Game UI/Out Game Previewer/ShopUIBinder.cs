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
        public int UnimoId = -1;
        public int KartId = -1;

        public bool IsBound { get; private set; }

        public void TryBind()
        {
            if (IsBound) return;

            if (UnimoId >= 0) ItemPreviewManager.Instance.BindUnimoPreview(UnimoId, _rawImage);
            else if (KartId >= 0) ItemPreviewManager.Instance.BindKartPreview(KartId, _rawImage);
            IsBound = true;
        }

        public void TryUnbind()
        {
            if (!IsBound) return;

            if (UnimoId >= 0) ItemPreviewManager.Instance.UnbindPreview(UnimoId, _rawImage);
            else if (KartId >= 0) ItemPreviewManager.Instance.UnbindPreview(KartId, _rawImage);
            IsBound = false;
        }
    }
}