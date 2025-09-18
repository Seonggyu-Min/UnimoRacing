using Core.UnityUtil.PoolTool;
using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;


namespace MSG
{
    public class ItemPreviewManager : Singleton<ItemPreviewManager>
    {
        #region Fields and Properties

        [Header("프리뷰 오브젝트 설정")]
        [SerializeField] private string _previewLayer;          // 하나만 선택해야 됨. 이것 보다 좋은 방법이 있을 것 같은데...
                                                                // LayerMask는 중복 가능성, string은 오타 가능, int는 뭐가 어떤 레이어인지 모르는 상태라서 셋 다 마음에는 안드는 듯
        private int _layer;
        [SerializeField] private Transform _parent;
        [SerializeField] private int _xPositionInterval = 5;    // x포지션의 간격. 안겹치게 하기 위해서 씀
        [SerializeField] private int _yPositionInterval = 5;    // y포지션의 간격. 카트랑 캐릭터랑 안겹치게 하기 위해서 씀

        [Header("Render Texture 설정")]
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] GraphicsFormat _colorFormat;
        [SerializeField] GraphicsFormat _depthStencilFormat;

        [Header("Preview Camera 설정")]
        [SerializeField] private PreviewCameraScheduler _scheduler;


        private Dictionary<int, GameObject> _unimoObjs = new();
        private Dictionary<int, GameObject> _kartObjs = new();

        private readonly Dictionary<int, RenderTexture> _rtMap = new();


        public bool Ready { get; private set; }     // 이거 실제로 쓸 때는 필요 없음

        #endregion


        #region Unity Methods

        private void Awake()
        {
            SingletonInit();
            LayerMaskToLayer();
        }

        private void Start()
        {
            SpawnUnimos();
            SpawnKarts();
            Ready = true;
        }

        #endregion


        #region Public API Methods

        public void BindUnimoPreview(int unimoId, RawImage targetRawImage)
        {
            if (!_unimoObjs.TryGetValue(unimoId, out GameObject targetObj) || targetObj == null)
            {
                Debug.LogWarning($"[ItemPreview] Unimo {unimoId} 없음");
                return;
            }

            RenderTexture rt = GetOrCreateRenderTexture(unimoId);
            targetRawImage.texture = rt;

            _scheduler?.Register(unimoId, targetObj.transform, targetRawImage, rt);
        }

        public void BindKartPreview(int kartId, RawImage targetRawImage)
        {
            if (!_kartObjs.TryGetValue(kartId, out GameObject targetObj) || targetObj == null)
            {
                Debug.LogWarning($"[ItemPreview] Kart {kartId} 없음");
                return;
            }

            RenderTexture rt = GetOrCreateRenderTexture(kartId);
            targetRawImage.texture = rt;

            _scheduler?.Register(kartId, targetObj.transform, targetRawImage, rt);
        }

        public void UnbindPreview(int id, RawImage rawImage = null)
        {
            _scheduler.Unregister(id);
            if (rawImage != null) rawImage.texture = null;
        }

        // Render Texture 비우기. 씬 떠날 때 호출해야될 듯
        public void DisposePreviewRenderTexture(int id)
        {
            if (_rtMap.TryGetValue(id, out RenderTexture rt) && rt != null)
            {
                if (rt.IsCreated()) rt.Release();
                Destroy(rt);
            }
            _rtMap.Remove(id);
        }

        #endregion


        #region Private Methods

        // 런타임 로딩을 줄이려면 이 스폰을 안하고 미리 배치할 수 있을 듯
        // 근데 별 차이 없고 프리팹 추가 시 자동 등록이 훨씬 유용한 방법인 듯
        private void SpawnUnimos()
        {
            List<UnimoCharacterSO> unimos = UnimoKartDatabase.Instance.GetAllUnimos();

            if (unimos == null || unimos.Count == 0)
            {
                Debug.LogWarning("[UnimoKartSpawner] 유니모 리스트가 비어있습니다. 스폰을 할 수 없습니다");
                return;
            }

            for (int i = 0; i < unimos.Count; i++)
            {
                GameObject preview = Instantiate(unimos[i].characterPrefab);
                SetLayerRecursively(preview);
                preview.transform.parent = _parent;
                preview.transform.position = new Vector3(_xPositionInterval * i, 0f, 0f);    // 일렬로 나열해서 카메라에서 다른 오브젝트가 겹쳐지지 않게 함

                _unimoObjs.Add(unimos[i].characterId, preview);
            }
        }

        private void SpawnKarts()
        {
            List<UnimoKartSO> karts = UnimoKartDatabase.Instance.GetAllKarts();

            if (karts == null || karts.Count == 0)
            {
                Debug.LogWarning("[UnimoKartSpawner] 카트 리스트가 비어있습니다. 스폰을 할 수 없습니다");
                return;
            }

            for (int i = 0; i < karts.Count; i++)
            {
                GameObject preview = Instantiate(karts[i].kartPrefab);
                SetLayerRecursively(preview);
                preview.layer = _layer;  // 레이어를 프리뷰용으로 등록
                preview.transform.parent = _parent;
                preview.transform.position = new Vector3(_xPositionInterval * i, _yPositionInterval, 0f);    // 일렬로 나열해서 카메라에서 다른 오브젝트가 겹쳐지지 않게 함

                _kartObjs.Add(karts[i].KartID, preview);
            }
        }

        private RenderTexture GetOrCreateRenderTexture(int id)
        {
            if (_rtMap.TryGetValue(id, out RenderTexture rt) && rt != null) return rt;

            RenderTextureDescriptor desc = new RenderTextureDescriptor(_width, _height);
            desc.graphicsFormat = _colorFormat;
            desc.depthStencilFormat = _depthStencilFormat;

            rt = new RenderTexture(desc);
            rt.Create();

            _rtMap[id] = rt;
            return rt;
        }

        private void LayerMaskToLayer()
        {
            _layer = LayerMask.NameToLayer(_previewLayer);
        }

        private void SetLayerRecursively(GameObject obj)
        {
            obj.layer = _layer;
            foreach (Transform child in obj.transform)
            {
                if (child == null) continue;
                SetLayerRecursively(child.gameObject);
            }
        }

        #endregion


        #region Debug Methods

        [Button("Debug Dict")]
        private void DebugDict()
        {
            StringBuilder sb = new();

            sb.AppendLine("유니모 시작");
            foreach (var unimo in _unimoObjs)
            {
                sb.Append(unimo.Key);
                sb.Append(" ");
                sb.Append(unimo.Value.name.ToString());
                sb.AppendLine();
            }

            sb.AppendLine("카트 시작");
            foreach (var kart in _kartObjs)
            {
                sb.Append(kart.Key);
                sb.Append(" ");
                sb.Append(kart.Value.name.ToString());
                sb.AppendLine();
            }

            Debug.Log(sb);
        }

        #endregion
    }
}
