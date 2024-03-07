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
	[Property] public Guid OwnerId;
	[Property] public GameObject Owner;
	private Rigidbody RocketBody { get; set; }
	[Property] public WeaponComponent Weapon { get; set; }
	private TimeSince rocketCreated { get; set; }
	private bool hasExploded = false;
	private bool hasEmbeded = false;
	private bool hasCharge = false;

	protected override void OnAwake()
	{
	}

	protected override void OnStart()
	{
		Owner = Scene.Directory.FindByGuid( OwnerId );
		Weapon = Owner.Components.Get<WeaponComponent>();
		RocketBody = Components.Get<Rigidbody>();
		rocketCreated = 0;
		Weapon.RocketCreated = true;
	}

	public void OnCollisionStart( Collision other )
	{
		if ( hasExploded ) return;
		if ( RocketBody is not null )
		{
			RocketBody.Velocity = 0;
			RocketBody.AngularVelocity = 0;
		}


		hasEmbeded = true;
		if ( !hasCharge )
		{
			Explosion();
		}
	}

	public void OnCollisionUpdate( Collision other )
	{
	}

	public void OnCollisionStop( CollisionStop other )
	{
	}

	[Broadcast]
	void Explosion()
	{
		Weapon.RocketCreated = false;
		var explosionSphere = new Sphere( GameObject.Transform.Position, ExplosionRadius );

		var trExplosion = Scene.Trace
			.Sphere( ExplosionRadius, GameObject.Transform.Position, GameObject.Transform.Position )
			.Run();

		Gizmo.Draw.LineSphere( trExplosion.EndPosition, ExplosionRadius );
		hasExploded = true;
		if ( trExplosion.Hit )
		{
			var explosionTargets = Scene.FindInPhysics( explosionSphere ).Where( x => x.Tags.Has( "player" ) );
			foreach ( var target in explosionTargets )
			{
				var distance = Vector3.DistanceBetween( target.Transform.Position, trExplosion.EndPosition );
				var damage = Damage - (distance / 2);
				target.Components.GetInAncestorsOrSelf<IDamagable>().Damage( damage, null );
				Log.Info( distance );
				var velocity = 1000f;

				target.Components.TryGet( out CharacterController character, FindMode.InParent );
				{
					character.Velocity += velocity;
					Log.Info( "test" );
				}
			}
		}


		GameObject.Destroy();
	}

	protected override void OnUpdate()
	{
		//( Weapon.RocketCharging );

		if ( IsProxy || hasExploded ) return;

		if ( rocketCreated > 0.25f && rocketCreated < 0.26f && Weapon.RocketCharging )
		{
			hasCharge = true;
		}

		if ( !Weapon.RocketCharging && hasCharge )
		{
			Explosion();
		}

		if ( rocketCreated > 2f && !Weapon.RocketCharging )
		{
			Weapon.RocketCreated = false;
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
