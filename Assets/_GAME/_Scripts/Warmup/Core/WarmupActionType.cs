namespace GameYT.Warmup
{
    public enum WarmupActionType
    {
        Run = 0,
        [UnityEngine.InspectorName("Left")]
        MoveLeft = 1,
        [UnityEngine.InspectorName("Right")]
        MoveRight = 2,
        Jump = 3,
        Duck = 4,
        Punch = 5,
        Freeze = 6
    }
}
