public interface IGameSetup
{
    /// <summary>Setup 실행 순서 (작을수록 먼저 실행)</summary>
    int Order { get; }

    /// <summary>셋업 로직. 성공 시 true</summary>
    bool Setup();
}
