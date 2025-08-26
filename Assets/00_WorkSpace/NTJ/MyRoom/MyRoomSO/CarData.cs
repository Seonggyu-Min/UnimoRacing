using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCarData", menuName = "Car/CarData")]
public class CarData : ScriptableObject
{
    public string carID; // ���� ID
    public string carName; // ���� �̸�
    public string description; // ����
    public Sprite carSprite; // ��������Ʈ �̹��� ��ü�� ������ �� �ֽ��ϴ�.
}
