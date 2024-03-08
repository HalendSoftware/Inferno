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

	[Property, Category( "Rocket Properties" )]
	public float FirstChargeTime { get; set; }

	[Property, Category( "Rocket Properties" )]
	public float SecondChargeTime { get; set; }

	[Property, Category( "Rocket Properties" )]
	public float ThirdChargeTime { get; set; }

	private Rigidbody RocketBody { get; set; }
	[Property] public WeaponComponent Weapon { get; set; }
	private TimeSince rocketCreated { get; set; }
	private TimeSince chargeTime { get; set; }
	private bool hasExploded = false;
	private bool hasEmbeded = false;
	private bool hasCharge = false;


	private int tier;
	[Property] private float baseForce { get; set; }

	public static float CalculateExplosionForce( int tier, float baseForce )
	{
		float multiplier = 1.0f;
		switch ( tier )
		{
			case 1:
				multiplier = 1.0f;
				break;
			case 2:
				multiplier = 1.5f;
				break;
			case 3:
				multiplier = 2.0f;
				break;
			default:
				break;
		}

		return baseForce * multiplier;
	}

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

		if ( Weapon.RocketCharging )
		{
			hasCharge = true;
			chargeTime = 0;
		}
	}

	public void OnCollisionStart( Collision other )
	{
		if ( hasExploded ) return;
		if ( RocketBody is not null )
		{
			RocketBody.Velocity = 0;
			RocketBody.AngularVelocity = 0;
		}

		collider.Enabled = false;

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
	private void Explosion()
	{
		Log.Info( "test" );

		var explosionSphere = new Sphere( GameObject.Transform.Position, ExplosionRadius );

		var trExplosion = Scene.Trace
			.Sphere( ExplosionRadius, GameObject.Transform.Position, GameObject.Transform.Position )
			.Run();
		var explosionForce = CalculateExplosionForce( tier, baseForce );
		//Gizmo.Draw.LineSphere( trExplosion.EndPosition, ExplosionRadius );

		if ( trExplosion.Hit )
		{
			var explosionTargets = Scene.FindInPhysics( explosionSphere ).Where( x => x.Tags.Has( "player" ) );
			foreach ( var target in explosionTargets )
			{
				var distance = Vector3.DistanceBetween( target.Transform.Position, trExplosion.EndPosition );
				var damage = Damage - (distance / 2);
				target.Components.GetInAncestorsOrSelf<IDamagable>().Damage( damage, null );
				Log.Info( distance );

				target.Components.TryGet( out CharacterController character, FindMode.InParent );
				{
					var explosionPunch =
						((target.Transform.Position) - trExplosion.EndPosition).Normal * explosionForce;

					// Log.Info( explosionPunch );
					character.Velocity += explosionPunch;
					Log.Info( "test" );
				}
			}
		}
		

		Weapon.RocketCreated = false;
		hasExploded = true;
		Log.Info( explosionForce );
		GameObject.Destroy();
	}

	protected override void OnUpdate()
	{
		//( Weapon.RocketCharging );

		if ( IsProxy ) return;
		if ( hasExploded ) return;

		// if ( rocketCreated > 0.01f && rocketCreated < 0.02f && Weapon.RocketCharging )
		// {
		// 	hasCharge = true;
		//
		// 	chargeTime = 0;
		// }

		// if ( chargeTime > 0.2f &&  Weapon.RocketCharging )
		// {
		// 	hasCharge = true;
		// }

		if ( !Weapon.RocketCharging && hasCharge )
		{
			Explosion();
		}

		if ( rocketCreated > 2f && !Weapon.RocketCharging )
		{
			Weapon.RocketCreated = false;
		}

		if ( chargeTime > FirstChargeTime && chargeTime < SecondChargeTime && hasCharge )
		{
			tier = 1;
		}
		else if ( chargeTime > SecondChargeTime && chargeTime < ThirdChargeTime && hasCharge )
		{
			tier = 2;
		}
		else if ( chargeTime > ThirdChargeTime && hasCharge )
		{
			tier = 3;
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
