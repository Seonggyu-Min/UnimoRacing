using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


namespace MSG
{
    public class SkyBoxApplier : MonoBehaviour
    {
        [SerializeField] private Material[] _skyBoxMaterials;   // 3개 다 등록해야 됩니다.

        private void Start()
        {
            int index = PhotonNetworkCustomProperties.GetRoomProp<int>(RoomKey.WinnerMapIndex);

            if (index >= 0 && index < _skyBoxMaterials.Length)
            {
                Material mat = _skyBoxMaterials[index];
                if (mat != null)
                {
                    RenderSettings.skybox = mat;
                    DynamicGI.UpdateEnvironment();
                }
                else
                {
                    Debug.Log($"[SkyBoxApplier] Skybox {index}가 null이라서 기본 skybox를 사용합니다.");
                }
            }
            else
            {
                Debug.LogWarning($"[SkyBoxApplier] _skyBoxMaterials에 material이 전부 등록되지 않았습니다. _skyBoxMaterials.Length: {_skyBoxMaterials.Length}");
            }
        }
    }
}
