using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RelationItem : MonoBehaviour
{
    [Header("Images")]
    public RawImage leftRaw;
    public RawImage rightRaw;
    public Image heartImage;

    [Header("Text")]
    public TMP_Text leftNameText;   // 왼쪽 캐릭터 이름
    public TMP_Text rightNameText;  // 오른쪽 캐릭터 이름
}
