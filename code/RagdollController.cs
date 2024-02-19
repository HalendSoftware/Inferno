using Sandbox.UI;

namespace Sambit.Player.Ragdoll;

public sealed class RagdollController : Component
{
    [Property] public ModelPhysics Physics { get; private set; }

    public bool IsRagdolled => Physics.Enabled;

    [Broadcast]
    public void Ragdoll(Vector3 position, Vector3 force)
    {
        Physics.Enabled = true;

        foreach (var body in Physics.PhysicsGroup.Bodies)
        {
            body.ApplyImpulseAt(position, force * 200f);
        }
    }

    [Broadcast]
    public void Unragdoll()
    {
        Transform.LocalPosition = Vector3.Zero;
        Transform.LocalRotation = Rotation.Identity;
        Physics.Enabled = false;
    }
}