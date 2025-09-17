using Photon.Pun;
using System;

[Serializable]
public class AppliedBuff
{
    public BuffId Id { get; private set; }
    public BuffCategory Category { get; private set; }
    public BuffStackPolicy StackPolicy { get; private set; }
    public int Stack { get; private set; }          // 필요 없으면 항상 1
    public double StartServerTime { get; private set; }
    public float Duration { get; private set; }

    public double EndServerTime => StartServerTime + Duration;
    public bool IsExpired => PhotonNetwork.Time >= EndServerTime;
    public float RemainingSeconds => (float)System.Math.Max(0.0, EndServerTime - PhotonNetwork.Time);

    public AppliedBuff(BuffId id, BuffCategory cat, BuffStackPolicy policy, float duration, int stack = 1)
    {
        Id = id;
        Category = cat;
        StackPolicy = policy;
        Duration = duration;
        Stack = stack;
        StartServerTime = PhotonNetwork.Time;
    }

    public void Refresh(float newDuration)
    {
        Duration = newDuration;
        StartServerTime = PhotonNetwork.Time;
    }

    public void AddStack(int add = 1, float extraDuration = 0f)
    {
        Stack += add;
        if (extraDuration > 0f) Duration += extraDuration;
    }
}
