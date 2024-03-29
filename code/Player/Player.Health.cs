using System.Net.NetworkInformation;
using Microsoft.VisualBasic;
using Sambit.Common.Interfaces;
using Sambit.Player.Ragdoll;
using Sandbox.Diagnostics;
using Sandbox.UI;

namespace Sambit.Player.Health;

public class PlayerHealth : Component, IDamagable
{
	[Property] public bool AwaitingResurrection;
	[Property] public bool AwaitingRevive;

	private float health;
	[Property] public bool IsRespawning;
	[Property] public LifeState LifeState;
	private RagdollController ragdollController;
	private float shields;
	[Property] public HealthState HealthState;
	[Property] public ShieldState ShieldState;
	[Property] public GameObject Spark;
	public TimeSince TimeSinceDeath;
	[Property] public float respawnTime { get; set; }
	private PlayerController playerController => Components.Get<PlayerController>();
	private CharacterController characterController => Components.Get<CharacterController>();
	private PlayerTeam playerTeam => Components.Get<PlayerTeam>();
	public RoundManager roundManager => Scene.GetAllComponents<RoundManager>().FirstOrDefault();

	[Property, Sync] public GameObject lastAttacker { get; set; }
	[Property, Sync] public Guid lastAttackerId { get; set; }
	[Property] public Guid MyGuid { get; set; }
	[Property] private Vignette DeathVignette { get; set; }
	[Property] private DepthOfField DeathDOF { get; set; }
	[Property] private DeathColorFilter _deathColorFilter { get; set; }

	//Health Properties

	[Category( "Health" )] [Property] public float MaxHealth { get; } = 100f;
	[Category( "Health" )] [Property] public float MaxShields { get; } = 100f;
	[Property] public uint PlayerKills { get; set; }

	[Category( "Health" )]
	[Property]
	[Sync]
	public float Health
	{
		get => health;
		set => health = Math.Clamp( value, 0.0f, MaxHealth );
	}


	[Category( "Health" )]
	[Property]
	[Sync]
	public float Shields
	{
		get => shields;
		set => shields = Math.Clamp( value, 0.0f, MaxShields );
	}

	[Category( "Health" )]
	[Property]
	[Sync]
	public float DefaultHealthRegen { get; } = 1f;

	[Category( "Health" )]
	[Property]
	[Sync]
	public float DefaultShieldsRegen { get; } = 1f;

	[Category( "Health" )]
	[Property]
	[Sync]
	public float HealthRegen { get; set; }

	[Category( "Health" )]
	[Property]
	[Sync]
	public float ShieldsRegen { get; set; }

	[Category( "Health" )]
	[Property]
	[Sync]
	public float DefaultRegenDelay { get; set; }

	[Category( "Health" )]
	[Property]
	[Sync]
	public float RegenDelay { get; set; }

	//

	private TimeSince timeSinceDamaged { get; set; }

	// Idk if should be synced
	// so far so good, surely nothing breaks
	private TimeSince timeSinceSpawnCheck { get; set; } = 0;
	private Transform? overrideSpawnPoint { get; set; }

	[Broadcast]
	public void Damage( float damage, IDamageSource source, Guid AttackerId )
	{
		if ( LifeState != LifeState.Alive )
		{
			return;
		}


		//if ( (info.Attacker as Pawn) != null && (info.Attacker as Pawn).teamID == teamID && teamID != 0 && !(info.Attacker == this)) return;

		// float remainingHealth = Health - damage;
		//
		// if ( remainingHealth <= 0 )
		// {
		// 	Health = 0; // Ensure health doesn't go below zero
		// 	LifeState = LifeState.Dead;
		// 	Log.Info( "Killed Player via " + source.GetSourceName() );
		// 	return; // Exit the method since the player is dead
		// }

		//var isHeadshot = info.Hitbox.HasTag( "head" );
		if ( Shields <= 0 )
		{
			// if ( isHeadshot )
			// {
			// 	info.Damage *= 10.0f;
			// }
			Health -= damage;
		}
		else
		{
			var leftOverDamage = -(Shields - damage);
			Shields -= damage;
			if ( Shields <= 0 )
			{
				// OnShieldBreak();
				// if ( isHeadshot )
				// {
				// 	leftOverDamage *= 10.0f;
				// }
				damage = leftOverDamage;
				Health -= damage;
			}
		}


		if ( AttackerId != MyGuid )
		{
			lastAttackerId = AttackerId;
		}

		//AttackerGuid = AttackerId.ToString();
		timeSinceDamaged = 0;
	}


	protected override void OnFixedUpdate()
	{
		if ( TimeSinceDeath >= respawnTime && LifeState == LifeState.Dead )
		{
			AwaitingResurrection = true;
			if ( Input.Pressed( "Interact" ) )
			{
				//Sound.Play( "sounds/player/ui/RespawnActivated.sound" );
				Respawn();
			}
		}

		if ( timeSinceDamaged >= 10 )
		{
			lastAttacker = GameObject;
			lastAttackerId = GameObject.Id;
		}

		if ( LifeState == LifeState.Dead )
		{
			return;
		}

		//Debug death until we can get the ability to actually die via death
		if ( Input.Pressed( "AirMove" ) )
		{
			health = 0;
			timeSinceDamaged = 0f;
		}

		CheckSpawnOverride();


		if ( Health < MaxHealth && timeSinceDamaged > RegenDelay )
		{
			HealthState = HealthState.Regenerating;
			Health += HealthRegen * Time.Delta;
		}

		if ( Shields < MaxShields && timeSinceDamaged > RegenDelay && HealthState != HealthState.Regenerating )
		{
			ShieldState = ShieldState.Charging;
			Shields += ShieldsRegen * Time.Delta;
		}

		if ( Shields == MaxShields )
			ShieldState = ShieldState.Charged;
		if ( Health == MaxHealth )
			HealthState = HealthState.Full;
	}

	protected override void OnUpdate()
	{
		if ( LifeState == LifeState.Dead )
		{
			return;
		}

		if ( Health <= 0 && LifeState == LifeState.Alive )
		{
			Death();
		}

		if ( Shields <= 0 && ShieldState == ShieldState.Charged )
		{
			ShieldBreak();
		}
	}

	public void Respawn()
	{
		if ( IsProxy )
		{
			return;
		}

		DeathVignette.Enabled = false;
		DeathDOF.Enabled = false;
		_deathColorFilter.Enabled = false;

		Health = MaxHealth;
		Shields = MaxShields;
		HealthRegen = DefaultHealthRegen;
		ShieldsRegen = DefaultShieldsRegen;
		AwaitingResurrection = false;
		IsRespawning = false;
		LifeState = LifeState.Alive;
		characterController.Velocity = Vector3.Zero;

		foreach ( var respawnListener in Components.GetAll<IRespawnListener>() )
		{
			respawnListener.OnRespawn();
		}

		MoveToSpawnPoint( overrideSpawnPoint );
		ragdollController.Unragdoll();
	}

	public void Death()
	{
		if ( IsProxy ) return;
		LifeState = LifeState.Dead;
		if ( lastAttackerId != Guid.Empty )
		{
			lastAttacker = Scene.Directory.FindByGuid( lastAttackerId );
			if ( lastAttacker.Network.OwnerId == GameObject.Network.OwnerId )
			{
				if ( lastAttacker.Components.Get<PlayerHealth>().PlayerKills != 0 )
					lastAttacker.Components.Get<PlayerHealth>().ReduceCredit();
			}
			else
				lastAttacker.Components.Get<PlayerHealth>().KillCredit();
		}

		//Log.Error( lastAttackerId );
		playerController.IsEscaping = false;
		Health = 0;
		Shields = 0;
		IsRespawning = true;

		TimeSinceDeath = 0f;

		//TODO: HOLY FUCK, do NOT do this, PLEASE draw it UNDERNEATH the UI, SAVE YOURSELF
		DeathVignette.Enabled = true;
		DeathDOF.Enabled = true;
		_deathColorFilter.Enabled = true;

		foreach ( var deathListener in Components.GetAll<IDeathListener>() )
		{
			deathListener.OnDeath();
		}

		//We can feed the force damage information eventually
		ragdollController.Ragdoll( Transform.Position, Vector3.Forward * 1f );

		//TODO: Add other shit

		//If our override is null, spawn the ghost at player position, otherwise this errors out
		//No, i don't know why the trace is slightly off.
		//this is a hyper niche scenario when could this ever possibly fuck things up?
		GhostDrop();
	}

	public void ShieldBreak()
	{
		if ( IsProxy )
		{
		}
		//Add other shit
	}

	public void CheckSpawnOverride()
	{
		if ( timeSinceSpawnCheck > 5 )
		{
			if ( characterController.IsOnGround && LifeState == LifeState.Alive ) // Add other checks
			{
				overrideSpawnPoint = Transform.World;
				//Log.Info( $"Override Spawnpoint set to {overrideSpawnPoint?.Position}" );
			}

			timeSinceSpawnCheck = 0;
		}
	}

	private void MoveToSpawnPoint( Transform? overrideSpawn )
	{
		if ( IsProxy )
		{
			return;
		}


		var spawnpoints = Scene.GetAllComponents<SambitSpawnpoint>()
			.Where( x => x.Team == Components.Get<PlayerTeam>().CurrentTeam );

		// If there are no team assigned spawn points, just pick a random one
		if ( !spawnpoints.Any() )
		{
			spawnpoints = Scene.GetAllComponents<SambitSpawnpoint>();
		}

		var randomSpawnpoint = Game.Random.FromList( spawnpoints.ToList() );

		Transform.Position = randomSpawnpoint.Transform.Position;

		// // Spawn at override spawn point if available
		// if ( overrideSpawn != null )
		// {
		// 	Transform.Position = overrideSpawn.Value.Position;
		// 	return;
		// }
	}

	protected override void OnAwake()
	{
		ragdollController = Components.GetInDescendantsOrSelf<RagdollController>();
		MyGuid = GameObject.Id;
		Tags.Add( "damagable" );
	}

	public SceneTraceResult GhostFloorTrace()
	{
		var tr = Scene.Trace.Ray( new Ray( GameObject.Transform.Position, Vector3.Down ), 1000 )
			.WithoutTags( "player", "model" )
			//this seems like a bad idea, but it works for now. 
			.IgnoreGameObject( GameObject.Children.Any() ? GameObject.Children.FirstOrDefault() : null )
			.Run();

		if ( tr.Hit )
		{
		}

		return tr;
	}

	public void GhostDrop()
	{
		if ( overrideSpawnPoint == null )
		{
			var G = Spark.Clone( GhostFloorTrace().EndPosition + Vector3.Up * 40 );
			G.Components.Get<Collider>().IsTrigger = true;
			G.Components.Get<GhostRevive>().OwnerId = GameObject.Id;
			G.Enabled = true;
			G.BreakFromPrefab();
			G.NetworkSpawn();
		}
		else
		{
			var G = Spark.Clone( overrideSpawnPoint.Value.Position + Vector3.Up * 40 );
			G.Components.Get<Collider>().IsTrigger = true;
			G.Components.Get<GhostRevive>().OwnerId = GameObject.Id;
			G.Enabled = true;
			G.BreakFromPrefab();
			G.NetworkSpawn();
		}
	}

	public interface IDeathListener
	{
		void OnDeath();
	}

	[Broadcast]
	void KillCredit()
	{
		PlayerKills++;
	}

	[Broadcast]
	void ReduceCredit()
	{
		PlayerKills--;
	}


	public interface IRespawnListener
	{
		void OnRespawn();
	}
}
