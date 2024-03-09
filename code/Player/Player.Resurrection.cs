using Sambit.Player;
using Sambit.Player.Client;
using Sambit.Player.Health;
using Sandbox;

public sealed class GhostRevive : Interactable
{
	private ModelRenderer parentObject;
	[Property] public Guid OwnerId;
	[Property] public GameObject Owner;
	[Property] public PlayerHealth OwnerHealth;

	protected override void OnAwake()
	{
		parentObject = Components.Get<ModelRenderer>();
	}

	protected override void OnStart()
	{
		Owner = Scene.Directory.FindByGuid( OwnerId );
		OwnerHealth = Owner.Components.Get<PlayerHealth>();
	}

	protected override void OnUpdate()
	{
		if ( OwnerHealth.Health > 0 )
			DestroyGhost();
	}

	public override void Interact( GameObject player )
	{
		IsActive = true;
		Log.Info( $"{Owner.Name} Revived" );

		Resurrect();

		GameObject.Destroy();
	}

	[Broadcast]
	public void Resurrect()
	{
		OwnerHealth.Respawn();
		DestroyGhost();
	}

	[Broadcast]
	public void DestroyGhost()
	{
		GameObject.Destroy();
	}
}
