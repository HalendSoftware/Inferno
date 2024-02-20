using Sambit.Player;
using Sandbox;

public sealed class ScorchPlosion : Component, Component.ICollisionListener
{
	public bool HasCollided = false;	
	[Property] public float BlastStrength { get; set; } = 1000;
	[Property] private SphereCollider collider { get; set; }
	public void OnCollisionStart(Collision other)
	{
		foreach (var c in collider.Touching)
		{
			Log.Info(c.GameObject.Name  );
			var velocity = (c.Transform.Position - Transform.Position).Normal * BlastStrength;
			if (c.Components.TryGet(out Rigidbody rb))
				rb.Velocity += velocity;

			if (c.Components.TryGet(out CharacterController character))
				character.Velocity += velocity;
		}
	}

	public void OnCollisionUpdate(Collision other)
	{
	}

	public void OnCollisionStop(CollisionStop other)
	{
	}


	// protected override void OnFixedUpdate()
	// {
	// 	foreach (var c in collider.Touching)
	// 	{
	// 		Log.Info(c.GameObject.Name  );
	// 		var velocity = (c.Transform.Position - Transform.Position).Normal * BlastStrength;
	// 		if (c.Components.TryGet(out Rigidbody rb))
	// 			rb.Velocity += velocity;
	//
	// 		if (c.Components.TryGet(out CharacterController character))
	// 			character.Velocity += velocity;
	// 	}
	// 	//GameObject.Destroy();
	// }
	
}
