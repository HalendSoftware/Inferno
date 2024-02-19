namespace Sambit.Common.Interfaces;

public interface IDamagable {

    void Damage(float damage, IDamageSource source);
}

public interface IDamageSource {

    public string GetSourceName() => "The Architects";
}