using Sambit.Common.Interfaces;
using Sambit.Player;
using Sandbox;
using Sandbox.Physics;
using Sandbox.VR;
using Sandbox.Weapons;
using System.Threading;
using Sambit.Player.Health;

public sealed class ScorchPlosion : Component, Component.ICollisionListener
{
	[Property] private SphereCollider collider { get; set; }
	[Property] private ParticleEffect particleEffect { get; set; }
	[Property] private ParticleSphereEmitter FireEmitter { get; set; }
	[Property] private GameObject ExplosionPrefab { get; set; }
	[Property] private float MaxDamage { get; set; }
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
	private CancellationTokenSource cancellationTokenSource;


	private int tier;
	[Property] private float baseForce { get; set; }

	public static float CalculateExplosionForce( int tier, float baseForce )
	{
		float multiplier = 1.0f;
		switch ( tier )
		{
			case 1:
				multiplier = 1.25f;
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

	public float CalculateDropoffDamage( Vector3 explosionPosition, Vector3 targetPositionm, float distance )
	{
		distance = Math.Min( distance, ExplosionRadius );

		var sigma = ExplosionRadius / 2;
		float damage = MaxDamage *
		               (float)Math.Exp(
			               -(distance * distance) / (2f * (ExplosionRadius / 2f) * (ExplosionRadius / 2f)) );

		//Log.Info( distance );

		damage = CalculateExplosionForce( tier, damage );
		return damage;
	}

	protected override void OnAwake()
	{
	}

	async protected override void OnStart()
	{
		Owner = Scene.Directory.FindByGuid( OwnerId );
		Weapon = Owner.Components.Get<WeaponComponent>();
		RocketBody = Components.Get<Rigidbody>();
		rocketCreated = 0;
		Weapon.RocketCreated = true;

		await GameTask.Delay( 200 );
		//Log.Info( "test" );

		if ( Weapon.RocketCharging )
		{
			hasCharge = true;
			chargeTime = 0;
		}
	}

	public void OnCollisionStart( Collision other )
	{
		if ( Owner == null )
		{
			Owner = Scene.Directory.FindByGuid( OwnerId );
			Weapon = Owner.Components.Get<WeaponComponent>();
			RocketBody = Components.Get<Rigidbody>();
		}

		RocketBody.Velocity = 0;
		Log.Info( Weapon );
		if ( Weapon.RocketCharging )
		{
			hasCharge = true;
		}


		// if ( hasExploded ) return;


		Log.Info( "collided" );
		//collider.Tags.Add( "projectile" );


		hasEmbeded = true;
		if ( !hasCharge )
		{
			Explosion();
		}
	}


	private void Explosion()
	{
		if ( IsProxy ) return;
		var explosionSphere = new Sphere( GameObject.Transform.Position, ExplosionRadius );

		var trExplosion = Scene.Trace
			.Sphere( ExplosionRadius, GameObject.Transform.Position, GameObject.Transform.Position )
			.Run();


		//Debug Purposes, breaks networking (somehow)
		//Gizmo.Draw.LineSphere( trExplosion.EndPosition, ExplosionRadius );

		Weapon.RocketCreated = false;

		var trDistance = Scene.Trace.Ray( GameObject.Transform.Position, Owner.Transform.Position )
			.Run();

		//Log.Info( trDistance.Distance );
		//Log.Info( rocketCreated );

		if ( trExplosion.Hit )
		{
			var explosionTargets = Scene.FindInPhysics( explosionSphere ).Where( x => x.Tags.Has( "player" ) );
			foreach ( var target in explosionTargets )
			{
				if ( target.Network.IsOwner )
				{
					var distance = Vector3.DistanceBetween( target.Transform.Position, trExplosion.EndPosition );

					float damage = CalculateDropoffDamage( trExplosion.EndPosition, target.Transform.Position,
						distance );
					target.Components.GetInAncestorsOrSelf<IDamagable>().Damage( damage / 3, null, OwnerId );
					//Log.Info( "Damage: " + damage / 3 );


					//Log.Info( distance );

					var explosionForce = CalculateExplosionForce( tier, baseForce );

					target.Components.TryGet( out CharacterController character, FindMode.InParent );
					{
						var explosionPunch =
							((target.Transform.Position) - trExplosion.EndPosition).Normal * explosionForce;

						// Log.Info( explosionPunch );
						character.Velocity += explosionPunch;
					}
				}
				else
				{
					var distance = Vector3.DistanceBetween( target.Transform.Position, trExplosion.EndPosition );
					if ( distance > 45 )
					{
						float damage = CalculateDropoffDamage( trExplosion.EndPosition, target.Transform.Position,
							distance );
						target.Components.GetInAncestorsOrSelf<IDamagable>().Damage( damage, null, OwnerId );

						//Log.Info( "Damage: " + damage );
					}

					if ( distance <= 45 )
					{
						float damage = 2000;
						target.Components.GetInAncestorsOrSelf<IDamagable>().Damage( damage, null, OwnerId );

						//Log.Info( "Damage: " + damage );
					}

					//Log.Info( distance );
					var explosionForce = CalculateExplosionForce( tier, baseForce );

					target.Components.TryGet( out CharacterController character, FindMode.InParent );
					{
						var pushDirection = (character.GameObject.Transform.Position - Transform.Position).Normal;
						character.Velocity += pushDirection * 1000;
					}
				}
			}
		}

		var ExplosionGO = ExplosionPrefab.Clone( Transform.Position );
		ExplosionGO.Enabled = true;
		ExplosionGO.Components.Get<ParticleEffect>().Tint = particleEffect.Tint;
		ExplosionGO.Components.Get<ParticleSphereEmitter>().Velocity = FireEmitter.Velocity * 5;
		ExplosionGO.BreakFromPrefab();
		ExplosionGO.NetworkSpawn();


		hasExploded = true;
		GameObject.Destroy();
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( hasExploded ) return;

		if ( !Weapon.RocketCharging && hasCharge )
		{
			Explosion();
		}

		if ( rocketCreated >= 0.1f && !Weapon.RocketCharging )
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
	void BlastKnockback( CharacterController character, GameObject target, SceneTraceResult trExplosion )
	{
		var explosionForce = CalculateExplosionForce( tier, baseForce );

		var explosionPunch =
			((target.Transform.Position) - trExplosion.EndPosition).Normal * explosionForce;

		// Log.Info( explosionPunch );
		character.Velocity += explosionPunch;
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
