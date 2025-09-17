using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCarData", menuName = "Car/CarData")]
public class CarData : ScriptableObject
{
    public string carID;
    public string carName;
    public string description;
    public Sprite carSprite;
    public bool isOwned; // 보유 여부 변수 추가
}