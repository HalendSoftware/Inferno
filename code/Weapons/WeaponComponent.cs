using System;
using Sambit.Common.Interfaces;
using Sambit.Player;
using Sambit.Player.Camera;
using Sambit.Player.Health;
using Sandbox.Physics;
using Sandbox.Util;
using Sandbox.Weapons.Types;

namespace Sandbox.Weapons;

public class WeaponComponent : Component
{
	private static readonly string TAG_DAMAGABLE = "damagable";
	[Property] public PlayerHealth playerHealth { get; set; }
	[Property, ResourceType( "weapon" )] public WeaponAsset Weapon { get; set; }
	[Property] public Material ImpactDecal { get; set; }

	private int _magazine = 0;

	[Sync, Property]
	public int CurrentMagazine
	{
		get => Math.Clamp( _magazine, 0, Weapon.Magazine );
		set => _magazine = value;
	}

	private int _reserve = 0;

	[Sync, Property]
	public int CurrentReserve
	{
		get => Math.Clamp( _reserve, 0, Weapon.AmmoReserve );
		set => _reserve = value;
	}

	[Sync, Property] public bool InfiniteReserve { get; set; }
	[Sync, Property] public bool InfiniteAmmo { get; set; }
	[Property] public GameObject EyePos { get; set; }
	[Sync, Property] public bool IsReloading { get; set; }
	public TimeUntil ReloadTimer { get; set; } = 0;

	private TimeUntil FireRateCooldown { get; set; } = 0;

	private AbstractWeaponType _weaponType;

	protected override void OnAwake()
	{
		CurrentMagazine = Weapon.Magazine;
		CurrentReserve = Weapon.AmmoReserve;
	}

	protected override void OnUpdate()
	{
		if ( IsProxy ) return;
		if ( playerHealth.LifeState == LifeState.Dead ) return;
		try
		{
			var hasFired = false;
			if ( Input.Down( "Fire" ) )
				hasFired = Fire();
			if ( Input.Pressed( "Reload" ) && !hasFired )
				Reload();

			if ( IsReloading && ReloadTimer )
			{
				var reloadAmount = Weapon.Magazine - CurrentMagazine;
				CurrentMagazine += Math.Min( reloadAmount, CurrentReserve );
				if ( !InfiniteReserve )
					CurrentReserve -= reloadAmount;
				IsReloading = false;
			}
		}
		catch ( Exception e )
		{
			Log.Error( e.Message );
			Log.Info( e.StackTrace );
		}
	}

	public bool Fire()
	{
		if ( !FireRateCooldown )
			return false;

		if ( CurrentMagazine <= 0 )
		{
			FireRateCooldown = Weapon.NextFireTime();
			NoAmmoMessage();
			return false;
		}

		if ( IsReloading )
		{
			IsReloading = false;
			//TODO: Override interrupted reload animation
		}

		if ( !FireRateCooldown )
			return false;

		var projectile = Weapon.Rocket.Clone( EyePos.Transform.Position );
		projectile.NetworkSpawn();
		projectile.Transform.Rotation = EyePos.Transform.Rotation;
		projectile.Components.Get<Rigidbody>().Velocity = EyePos.Transform.Rotation.Forward * 1000f;


		FireGunMessage();


		FireRateCooldown = Weapon.NextFireTime();
		if ( !InfiniteAmmo )
			CurrentMagazine--;
		return true;
	}
	//TODO:  Move into abstracted Weapon Type Interface for projectiles VS hitscan, rockets, arrows, trace rifles etc
	// This is hitscan for now
	//TODO: Do what the thing above says, im too stupid. 
	// public bool Fire() {
	//  
	//     if (!FireRateCooldown)
	//         return false;
	//     
	//     if (CurrentMagazine <= 0) {
	//         FireRateCooldown = Weapon.NextFireTime();
	//         NoAmmoMessage();
	//         return false;
	//     }
	//
	//     if (IsReloading) {
	//         IsReloading = false;
	//         //TODO: Override interrupted reload animation
	//     }
	//     
	//     if (!FireRateCooldown)
	//         return false;
	//     
	//     //TODO: Spread, Recoil, Falloff
	//     //TODO: Marker Bone for projectile origin
	//     var result = Scene.Trace.Ray(new Ray(Scene.Camera.Transform.Position, Scene.Camera.Transform.Rotation.Forward), 10000)
	//         .IgnoreGameObjectHierarchy(GameObject.Root)
	//         .UsePhysicsWorld()
	//         .UseHitboxes()
	//         .Run();
	//     FireGunMessage();
	//
	//     if (result.Hit) {
	//         if (result.GameObject.Tags.Has(TAG_DAMAGABLE) && result.GameObject.Components.GetInAncestorsOrSelf<IDamagable>() != null) {
	//             ImpactMessageEntity(result.GameObject.Id, result.HitPosition, result.Direction);
	//         } else {
	//             ImpactMessageEnvironment(result.HitPosition, result.Direction);
	//             ShootProjectile(result.HitPosition);
	//             
	//         }
	//     }
	//
	//     FireRateCooldown = Weapon.NextFireTime();
	//     if(!InfiniteAmmo)
	//         CurrentMagazine--;
	//     return true;
	// }

	private void Reload()
	{
		if ( CurrentMagazine >= Weapon.Magazine || IsReloading )
			return;

		IsReloading = true;
		ReloadTimer = Weapon.GetReloadTimer();
		//TODO: Start reload animation
	}

	[Broadcast]
	private void NoAmmoMessage()
	{
		Sound.Play( Weapon.FireEmptySound, Transform.Position );
	}

	[Broadcast]
	private void FireGunMessage()
	{
		Sound.Play( Weapon.FireSound, Transform.Position );
	}

	[Broadcast]
	private void ImpactMessageEntity( Guid gameObject, Vector3 impactPos, Vector3 impactNormal )
	{
		var obj = Scene.Directory.FindByGuid( gameObject );
		obj.Components.GetInAncestorsOrSelf<IDamagable>().Damage( Weapon.Impact, Weapon );
	}

	[Broadcast]
	private void ImpactMessageEnvironment( Vector3 impactPos, Vector3 impactNormal )
	{
		CreateBulletImpactDecal( impactPos, impactNormal );
		Sound.Play( "sounds/impacts/impact-bullet-concrete.sound", Transform.Position );
	}

	[Broadcast]
	private void ShootProjectile( Vector3 impactPos )
	{
		var projectile = Weapon.Rocket.Clone( impactPos );
		projectile.NetworkSpawn();
	}

	private void CreateBulletImpactDecal( Vector3 position, Vector3 normal )
	{
		var gameobject = Scene.CreateObject();
		gameobject.Transform.Position = position - normal * 4;
		gameobject.Transform.Rotation = Rotation.LookAt( normal );

		var decal = gameobject.Components.Create<DecalRenderer>();
		decal.Material = ImpactDecal;
		decal.Size = new Vector3( 8, 8, 8 );

		gameobject.DestroyAsync( 15F );
	}
}
