namespace Sambit.Common;

public class SelfDestruct : Component
{
    [Property] public float Lifetime { get; set; } = 5;
    public TimeUntil Timer;

    protected override void OnAwake()
    {
        Timer = Lifetime;
    }

    protected override void OnFixedUpdate()
    {
        if (Timer <= 0)
            GameObject.Destroy();
    }
}