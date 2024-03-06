using Sambit.Common.Interfaces;
using Sambit.Player;
using Sandbox;
using Sandbox.Physics;
using Sandbox.VR;
using Sandbox.Weapons;

public sealed class ScorchPlosion : Component, Component.ICollisionListener
{
	[Property] private SphereCollider collider { get; set; }
	[Property] private float Damage { get; set; }
	[Property] private float ExplosionRadius { get; set; }
	private bool hasExploded = false;

	public void OnCollisionStart( Collision other )
	{
		if ( hasExploded ) return;
		Impact();
	}

	public void OnCollisionUpdate( Collision other )
	{
	}

	public void OnCollisionStop( CollisionStop other )
	{
	}

	void Impact()
	{
		var explosionSphere = new Sphere( GameObject.Transform.Position, ExplosionRadius );

		var trExplosion = Scene.Trace
			.Sphere( ExplosionRadius, GameObject.Transform.Position, GameObject.Transform.Position )
			.Run();

		Gizmo.Draw.LineSphere( trExplosion.EndPosition, ExplosionRadius );
		hasExploded = true;
		if ( trExplosion.Hit )
		{
			var explosionTargets = Scene.FindInPhysics( explosionSphere ).Where( x => x.Tags.Has( "damagable" ) );
			foreach ( var target in explosionTargets )
			{
				var distance = Vector3.DistanceBetween( target.Transform.Position, trExplosion.EndPosition );
				var damagecalculation = Damage - (distance / 2);
				target.Components.GetInAncestorsOrSelf<IDamagable>().Damage( damagecalculation, null );
				Log.Info( distance );
				// var explosionPunch = (target.Transform.Position - trExplosion.EndPosition).Normal * 1000;
				// target.Components.Get<CharacterController>().Velocity += explosionPunch;
				// Log.Info( explosionPunch );
			}
		}
	}

	// void ITriggerListener.OnTriggerEnter(Collider other)
	// {
	// 	Log.Info("Trigger Start");
	// }
	//
	// void ITriggerListener.OnTriggerExit(Collider other)
	// {
	// 	Log.Info("Trigger Start");
	// }
}
