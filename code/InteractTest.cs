using Sambit.Player.Client;
using Sandbox;

public sealed class InteractTest : Interactable
{
    private ModelRenderer parentObject;

    protected override void OnAwake()
    {
        parentObject = Components.Get<ModelRenderer>();
    }

    protected override void OnUpdate()
    {
        if (!IsActive)
            parentObject.Tint = Color.White;
        else
            parentObject.Tint = Color.Red;

        IsActive = false;
    }

    [Broadcast]
    public override void Interact(GameObject player)
    {
        IsActive = true;
        Log.Info($"{player.Network.OwnerConnection.DisplayName} has unleashed an unknown threat...");
        Sound.Play("sounds/ding_ding_ding.sound");
        var controller = player.Components.Get<CharacterController>();
        if (controller.IsValid())
        {
            var pushDirection = (player.Transform.Position - Transform.Position).Normal;
            controller.Velocity += pushDirection * 1000;
        }

        GameObject.Destroy();
    }
}