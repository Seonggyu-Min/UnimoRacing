using Photon.Pun;
using UnityEngine;
using YSJ.Util;

namespace Runtime.UI
{
    public class PopupOpener : MonoBehaviour
    {
        [Tooltip("Start 시 자동으로 팝업 열지 여부")]
        public bool m_isStartOpenPopup = false;

        [Tooltip("해당 팝업을 띄어줄 캔버스")]
        [SerializeField] public Canvas useCanvas;

        [Tooltip("열릴 팝업 프리팹")]
        public GameObject popupPrefab;

        protected virtual void Start()
        {
            if (m_isStartOpenPopup)
                OpenPopup();
        }

        public virtual PopupBaseUI OpenPopup()
        {
            if (useCanvas == null)
            {
                this.PrintLog("useCanvas가 설정되지 않았습니다.");
                return null;
            }

            if (popupPrefab == null)
            {
                this.PrintLog("popupPrefab이 설정되지 않았습니다.");
                return null;
            }

            var popupCanvas = useCanvas;
            if (popupCanvas == null)
            {
                this.PrintLog("Canvas가 없습니다.");
                return null;
            }

            var ui = Instantiate(popupPrefab, popupCanvas.transform);
            var popup = ui.GetComponent<PopupBaseUI>();
            return popup;
        }
    }
}
