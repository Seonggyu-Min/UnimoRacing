using System;

[Serializable]
public struct StatusEffectInstance
{
    public StatusEffect effect;     // 어떤 효과인지
    public float value;             // 값  
    public float percent;           // 퍼센트 
    public double startTime;        // 네트워크 기준 시작 시각
    public float duration;          // 지속시간(초)
    public int sourceActor;         // 시전자(Photon ActorNumber), 로컬이면 -1

    public double EndTime => startTime + duration;
    public float Remaining(double now) => (float)Math.Max(0, EndTime - now);
}
