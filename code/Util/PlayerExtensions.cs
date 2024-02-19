using Sambit.Player;
using Sambit.Player.Health;

namespace Sandbox.Util;

public static class PlayerExtensions
{
    public static bool IsAlive(this GameObject _)
        => (_.Components.Get<PlayerHealth>()?.LifeState ?? LifeState.Alive) == LifeState.Alive;
}