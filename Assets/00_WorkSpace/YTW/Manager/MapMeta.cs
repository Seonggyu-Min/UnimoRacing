using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YTW
{
    public class MapMeta : MonoBehaviour
    {
        [SerializeField] string bgmAddress; // ¶Ç´Â AssetReferenceT<AudioClip> bgm;
        public string BgmAddress => bgmAddress;
    }
}