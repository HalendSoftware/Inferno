using Sambit.Common.Interfaces;
using Sambit.Player;
using Sandbox;
using Sandbox.Physics;
using Sandbox.VR;
using Sandbox.Weapons;

public sealed class ScorchPlosion : Component, Component.ICollisionListener
{
	[Property] private SphereCollider collider { get; set; }
	[Property] private ParticleEffect particleEffect { get; set; }
	[Property] private ParticleSphereEmitter FireEmitter { get; set; }
	[Property] private GameObject ExplosionPrefab { get; set; }
	[Property] private float Damage { get; set; }
	[Property] private float ExplosionRadius { get; set; }
	[Property] public Guid OwnerId;
	[Property] public GameObject Owner;

	[Property, Category( "Rocket Properties" ), Sync]
	public float FirstChargeTime { get; set; }

	[Property, Category( "Rocket Properties" ), Sync]
	public float SecondChargeTime { get; set; }

	[Property, Category( "Rocket Properties" ), Sync]
	public float ThirdChargeTime { get; set; }

	private Rigidbody RocketBody { get; set; }
	[Property] public WeaponComponent Weapon { get; set; }
	[Sync] private TimeSince rocketCreated { get; set; }
	[Sync] private TimeSince chargeTime { get; set; }
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

		//collider.Tags.Add( "projectile" );


		hasEmbeded = true;
		if ( !hasCharge )
		{
			Explosion();
		}
	}

	[Broadcast]
	private void Explosion()
	{
		var explosionSphere = new Sphere( GameObject.Transform.Position, ExplosionRadius );

		var trExplosion = Scene.Trace
			.Sphere( ExplosionRadius, GameObject.Transform.Position, GameObject.Transform.Position )
			.Run();
		var explosionForce = CalculateExplosionForce( tier, baseForce );

		//Debug Purposes, breaks networking (somehow)
		//Gizmo.Draw.LineSphere( trExplosion.EndPosition, ExplosionRadius );

		if ( trExplosion.Hit )
		{
			var explosionTargets = Scene.FindInPhysics( explosionSphere ).Where( x => x.Tags.Has( "player" ) );
			foreach ( var target in explosionTargets )
			{
				if ( target.Network.IsOwner )
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
				else if ( IsProxy )
				{
					var distance = Vector3.DistanceBetween( target.Transform.Position, trExplosion.EndPosition );
					var damage = Damage;
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
		}

		var ExplosionGO = ExplosionPrefab.Clone( Transform.Position );
		ExplosionGO.Enabled = true;
		ExplosionGO.Components.Get<ParticleEffect>().Tint = particleEffect.Tint;
		ExplosionGO.Components.Get<ParticleSphereEmitter>().Velocity = FireEmitter.Velocity * 5;
		ExplosionGO.BreakFromPrefab();

		Weapon.RocketCreated = false;
		hasExploded = true;
		Log.Info( explosionForce );
		GameObject.Destroy();
	}

	protected override void OnUpdate()
	{
		if ( hasExploded ) return;

		if ( !Weapon.RocketCharging && hasCharge )
		{
			Explosion();
		}

		if ( rocketCreated >= 2f && !Weapon.RocketCharging )
		{
			Weapon.RocketCreated = false;
		}

		if ( chargeTime >= FirstChargeTime && chargeTime <= SecondChargeTime && hasCharge )
		{
			Tier1Charge();
		}
		else if ( chargeTime >= SecondChargeTime && chargeTime <= ThirdChargeTime && hasCharge )
		{
			Tier2Charge();
		}
		else if ( chargeTime >= ThirdChargeTime && hasCharge )
		{
			Tier3Charge();
		}
	}

	[Broadcast]
	void Tier1Charge()
	{
		tier = 1;

		float t = (chargeTime - FirstChargeTime) / (SecondChargeTime - FirstChargeTime);
		particleEffect.Tint = Color.Lerp( Color.Orange, Color.White, (t - 0.2f) );

		FireEmitter.Velocity = 20;
	}

	[Broadcast]
	void Tier2Charge()
	{
		tier = 2;
		float t = (chargeTime - SecondChargeTime) / (ThirdChargeTime - SecondChargeTime);
		particleEffect.Tint = Color.Lerp( Color.White, Color.Cyan, (t - 0.2f) );

		FireEmitter.Velocity = 30;
	}

	[Broadcast]
	void Tier3Charge()
	{
		tier = 3;
		particleEffect.Tint = Color.Cyan;

		FireEmitter.Velocity = 40;
	}

	public void OnCollisionUpdate( Collision other )
	{
	}

	public void OnCollisionStop( CollisionStop other )
	{
	}
}
