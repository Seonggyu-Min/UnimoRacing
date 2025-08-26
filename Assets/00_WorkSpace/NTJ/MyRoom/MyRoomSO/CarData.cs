using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCarData", menuName = "Car/CarData")]
public class CarData : ScriptableObject
{
    public string carID; // 차량 ID
    public string carName; // 차량 이름
    public string description; // 설명
    public Sprite carSprite; // 스프라이트 이미지 자체를 저장할 수 있습니다.
}
