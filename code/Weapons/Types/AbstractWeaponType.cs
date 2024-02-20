namespace Sandbox.Weapons.Types;

public abstract class AbstractWeaponType {

    private WeaponComponent _component;
    
    public bool Fire() {
        return false;
    }

    public abstract bool IsProjectile();
}