using Sambit.Common.Interfaces;

namespace Sandbox.Weapons;

[GameResource("Weapon Definition", "weapon", "Describes a weapon and its properties.")]
public class WeaponAsset : GameResource, IDamageSource {
    
    [Category("Meta")] public WeaponType Archetype { get; set; }
    [Category("Meta")] public DamageType DamageType { get; set; }
    [Category("Meta")] public AmmunitionType AmmunitionType { get; set; }
    
    [Category("Description")] public string Name { get; set; }
    
    [Category("Weapon Properties")] public float Attack { get; set; }
    [Category("Weapon Properties")] public float Power { get; set; }
    [Category("Weapon Properties")] public float Zoom { get; set; }
    [Category("Weapon Properties")] public float RPM { get; set; }
    [Category("Weapon Properties")] public float Impact { get; set; }
    [Category("Weapon Properties")] public float Range { get; set; }
    [Category("Weapon Properties")] public float Stability { get; set; }
    [Category("Weapon Properties")] public float BlastRadius { get; set; }
    [Category("Weapon Properties")] public float Velocity { get; set; }
    [Category("Weapon Properties")] public int AmmoReserve { get; set; }
    [Category("Weapon Properties")] public int Magazine { get; set; }
    [Category("Weapon Properties")] public float ReloadSpeed { get; set; }
    [Category("Weapon Properties")] public float Handling { get; set; }
    [Category("Weapon Properties")] public float AimAssistance { get; set; }
    [Category("Weapon Properties")] public float RecoilDirection { get; set; }
    [Category("Weapon Properties")] public float AirbornEffectiveness { get; set; }
    
    [Category("Appearance")] public Model Model { get; set; }
    [Category("Appearance")] public PrefabFile Projectile { get; set; }
    
    [Category("Audio")] public SoundEvent FireSound { get; set; }
    [Category("Audio")] public SoundEvent FireEmptySound { get; set; }

    public TimeUntil NextFireTime() {
        return 1 / (RPM / 60f);
    }

    public TimeUntil GetReloadTimer() {
        return 1.5F;
    }

    public string GetSourceName() {
        return Name;
    }
}

public enum AmmunitionType {
    Primary,
    Special,
    Heavy,
    Limited
}

public enum DamageType {
    Kinetic,
    Stasis,
    Strand,
    Void,
    Solar,
    Arc
}

public enum WeaponType {
    AutoRifle,
    Bow,
    FusionRifle,
    Glaive,
    GrenadeLauncher,
    HandCannon,
    LinearFusionRifle,
    MachineGun,
    PulseRifle,
    RocketLauncher,
    ScoutRifle,
    Shotgun,
    Sidearm,
    SniperRifle,
    SubMachineGun,
    Sword,
    TraceRifle,
    Relic
}
